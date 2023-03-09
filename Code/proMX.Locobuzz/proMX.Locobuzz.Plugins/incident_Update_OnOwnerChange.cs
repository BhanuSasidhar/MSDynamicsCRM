using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using proMX.Locobuzz.Plugins.HelperClass;
using proMX.Locobuzz.Plugins.WellKnown;
using System;

namespace proMX.Locobuzz.Plugins
{
   public class incident_Update_OnOwnerChange : IPlugin
   {
      public void Execute(IServiceProvider serviceProvider)
      {
         IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
         ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
         try
         {
            tracing.Trace("Plugin executing initiated...");
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);
            Entity incident = context.InputParameters["Target"] as Entity;
            Entity incidentPreImg = context.PreEntityImages[Case.LogicalName];

            var caseRecord = service.Retrieve(Case.LogicalName, incident.Id, new ColumnSet(Case.Title, Case.Priority, Case.LocobuzzID, Case.Owner));
            var mentionEntity = CRMHelper.RetrieveMention(service, tracing, caseRecord);

            var channelGroupName = mentionEntity[0].GetAttributeValue<string>(LocobuzzMentions.ChannelGroupName);

            var l = caseRecord.GetAttributeValue<String>(Case.LocobuzzID);
            var locobuzzID = incidentPreImg.GetAttributeValue<String>(Case.LocobuzzID);

            if (locobuzzID != null)
            {
               Implementation(service, tracing, incidentPreImg, incident);
               tracing.Trace($"Notification From {caseRecord.LogicalName}");
               CRMHelper.CreateNotification(service, tracing, caseRecord, channelGroupName + " - New Case is Assigned to You.");
            }
            tracing.Trace("Implemented successfully");
         }
         catch (Exception ex)
         {
            tracing.Trace(ex.Message);
            throw new InvalidPluginExecutionException(ex.Message);
         }
      }
      private void Implementation(IOrganizationService service, ITracingService tracing, Entity incidentPreImg, Entity incident)
      {
         tracing.Trace("Implementing the logic...");
         var previousOwner = incidentPreImg.GetAttributeValue<EntityReference>(Case.Owner);
         var incidentReference = incident.ToEntityReference();
         var grantAccessRequest = new GrantAccessRequest
         {
            PrincipalAccess = new PrincipalAccess
            {
               AccessMask = AccessRights.ReadAccess,
               Principal = previousOwner
            },
            Target = incidentReference
         };
         service.Execute(grantAccessRequest);
      }
      private void CreateNotification(IOrganizationService service, ITracingService tracing, Entity incident, Entity incidentPreImg)
      {
         var user = service.Retrieve(Case.LogicalName, incident.Id, new ColumnSet(Case.Owner));
         Guid userId = user.GetAttributeValue<EntityReference>(Case.Owner).Id;

         var notification = new Entity(Notification.LogicalName);

         notification[Notification.Title] = "New Case is assigned to you";
         notification[Notification.Body] = $"You have new case";
         notification[Notification.IconType] = new OptionSetValue(100000000);
         notification[Notification.Owner] = new EntityReference("systemuser", userId);
         notification[Notification.Data] = "{\"actions\":[{\"title\":\"Navigate to Case\",\"data\":{\"url\":\"?pagetype=entityrecord&etn=incident&id=" + incident.Id + "\"}}]}";
         var notificatioID = service.Create(notification);
         tracing.Trace("Notification is created with an ID : " + notificatioID + "");
      }
   }
}
