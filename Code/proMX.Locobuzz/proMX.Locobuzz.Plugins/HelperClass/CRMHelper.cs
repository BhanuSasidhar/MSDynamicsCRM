using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using proMX.Locobuzz.Plugins.JsonClass;
using proMX.Locobuzz.Plugins.WellKnown;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;

namespace proMX.Locobuzz.Plugins.HelperClass
{
   public class CRMHelper
   {
      public static void CreateNotification(IOrganizationService service, ITracingService tracing, Entity entity, string title)
      {
         Guid userId = entity.GetAttributeValue<EntityReference>(Case.Owner).Id;

         var priority = string.Empty;
         if (entity.LogicalName == Case.LogicalName)
         {
            priority = entity.GetAttributeValue<string>(Case.Priority);
         }
         tracing.Trace($"Priority retrieved with value - {priority}");

         var notification = new Entity(Notification.LogicalName);
         notification[Notification.Title] = title;
         if (entity.LogicalName == LocobuzzMentions.LogicalName)
         {
            notification[Notification.Body] = $"{entity.GetAttributeValue<string>(LocobuzzMentions.Description)}";
            tracing.Trace($"Notification From {entity.LogicalName}");
            notification[Notification.IconType] = new OptionSetValue(100000004);
         }
         else if (entity.LogicalName == Case.LogicalName)
         {
            notification[Notification.Body] = $"{entity.GetAttributeValue<string>(Case.Title)}";
            tracing.Trace($"Notification From {entity.LogicalName}");
            if (priority == "High" || priority == "Urgent")
               notification[Notification.IconType] = new OptionSetValue(100000003);
            else
               notification[Notification.IconType] = new OptionSetValue(100000000);
         }
         notification[Notification.Owner] = new EntityReference("systemuser", userId);
         notification[Notification.Data] = "{\"actions\":[{\"title\":\"Navigate to Case\",\"data\":{\"url\":\"?pagetype=entityrecord&etn=" + entity.LogicalName + "&id=" + entity.Id + "\"}}]}";
         var notificatioID = service.Create(notification);
         tracing.Trace("Notification is created with an ID : " + notificatioID + "");

      }
      public static EntityCollection RetrieveMention(IOrganizationService service, ITracingService tracing, Entity incident)
      {
         var mentionQuery = new QueryExpression(LocobuzzMentions.LogicalName);
         mentionQuery.ColumnSet.AddColumn(LocobuzzMentions.ChannelGroupName);
         mentionQuery.AddOrder(LocobuzzMentions.ModofiededOn, OrderType.Descending);
         mentionQuery.Criteria.AddCondition(LocobuzzMentions.Case, ConditionOperator.Equal, incident.Id);
         var mentionEntity = service.RetrieveMultiple(mentionQuery);
         tracing.Trace($"No of Mention Retrieved : {mentionEntity.Entities.Count}");
         return mentionEntity;
      }
      public static void ProcessResponce(IOrganizationService service, ITracingService tracingService, HttpResponseMessage responseMessage, string method, string tableName)
      {
         if (responseMessage != null)
         {
            var responceText = responseMessage.Content.ReadAsStringAsync().Result;
            var responceObj = GetJsonObject<APIResponce>(responceText);
            tracingService.Trace("responceText:" + responceText);
            if (!responseMessage.IsSuccessStatusCode || !responceObj.Success)
            {
               Entity errorLog = new Entity(ErrorLog.LogicalName);
               errorLog[ErrorLog.ErrorDetails] = $"Request Url:{responseMessage?.RequestMessage?.RequestUri}\r\n " +
                  $"Data:{responseMessage?.RequestMessage?.Content?.ReadAsStringAsync()?.Result}\r\n " +
                  $"Response:{responceText}\r\n " +
                  $"StatusCode:{responseMessage.StatusCode}";
               errorLog[ErrorLog.AdditionalInformation] = responceObj.Message;
               errorLog[ErrorLog.TableName] = tableName;
               errorLog[ErrorLog.Method] = method;
               service.Create(errorLog);
            }
         }
      }
      public static HttpResponseMessage CallAPI<T>(string url, Entity apiConfigurationEntity, T jsonObject, ITracingService tracingService)
      {
         tracingService.Trace("Calling API");
         HttpResponseMessage response = null;
         using (var httpClient = new HttpClient())
         {

            string json = GetJsonFromObject(jsonObject);
            tracingService.Trace("Json:" + json);
            var authToken = Encoding.ASCII.GetBytes($"{apiConfigurationEntity.GetAttributeValue<string>(LocobuzzAPIConfiguration.AuthKey)}:{apiConfigurationEntity.GetAttributeValue<string>(LocobuzzAPIConfiguration.AuthSecret)}");
            var apiUrl = apiConfigurationEntity.GetAttributeValue<string>(LocobuzzAPIConfiguration.LocobuzzAPIURL) + url;
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            tracingService.Trace("apiUrl:" + apiUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
            response = httpClient.PostAsync(apiUrl, data).Result;
         }
         return response;
      }
      public static string GetJsonFromObject<T>(T ticketStatusChange)
      {
         DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
         MemoryStream memoryStream = new MemoryStream();
         serializer.WriteObject(memoryStream, ticketStatusChange);
         memoryStream.Position = 0;
         StreamReader sr = new StreamReader(memoryStream);
         return sr.ReadToEnd();
      }
      public static T GetJsonObject<T>(string json)
      {
         T uiJsonObject = default(T);
         using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(json)))
         {
            DataContractJsonSerializer deSerializer = new DataContractJsonSerializer(typeof(T));
            uiJsonObject = (T)deSerializer.ReadObject(stream);
         }
         return uiJsonObject;
      }
      public static UiJson GetUiJsonObject(Entity apiConfigurationEntity)
      {
         var uiJson = apiConfigurationEntity.GetAttributeValue<string>(LocobuzzAPIConfiguration.UiJson);
         var uiJsonObject = CRMHelper.GetJsonObject<UiJson>(uiJson);
         return uiJsonObject;
      }

      public static Entity GetAPIConfiguration(IOrganizationService service, ITracingService tracingService)
      {
         var query = new QueryExpression(Entities.APIConfiguration)
         {
            ColumnSet = new ColumnSet(LocobuzzAPIConfiguration.UiJson,
            LocobuzzAPIConfiguration.LocobuzzAPIURL, LocobuzzAPIConfiguration.AuthKey, LocobuzzAPIConfiguration.AuthSecret, LocobuzzAPIConfiguration.BrandID)
         };
         query.Criteria.AddCondition(LocobuzzAPIConfiguration.Status, ConditionOperator.Equal, 0);
         query.AddOrder(LocobuzzAPIConfiguration.CreatedOn, OrderType.Descending);
         return service.RetrieveMultiple(query).Entities.FirstOrDefault();
      }
   }
}
