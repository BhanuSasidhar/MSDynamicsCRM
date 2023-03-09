using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using proMX.Locobuzz.Plugins.HelperClass;
using proMX.Locobuzz.Plugins.JsonClass;
using proMX.Locobuzz.Plugins.WellKnown;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;

namespace proMX.Locobuzz.Plugins
{
   public class incident_Update : IPlugin
   {
      public void Execute(IServiceProvider serviceProvider)
      {
         var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
         var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
         var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
         var service = serviceFactory.CreateOrganizationService(context.UserId);

         try
         {
            tracingService.Trace("Plugin-Start");
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity)) return;

            if (context.MessageName != "Update") return;

            var targetEntity = context.InputParameters["Target"] as Entity;
            
            tracingService.Trace("Id:"+ targetEntity.Id);
            tracingService.Trace("User Id:" + context.InitiatingUserId);
            Implementation(service, tracingService, targetEntity, context.InitiatingUserId);
            
            tracingService.Trace("Plugin-End");
         }
         catch (Exception ex)
         {
            throw new NotImplementedException(ex.ToString());
         }
      }
      private void Implementation(IOrganizationService service, ITracingService tracingService, Entity targetEntity,Guid contextUserId)
      {
         if (targetEntity == null || targetEntity.LogicalName != Case.LogicalName) return;

         var entity = service.Retrieve(Case.LogicalName, targetEntity.Id, new ColumnSet(Case.StateCode, Case.StatusCode, Case.LocobuzzID));

         if (string.IsNullOrEmpty(entity.GetAttributeValue<string>(Case.LocobuzzID))) return;

         var locobuzzID = entity.GetAttributeValue<string>(Case.LocobuzzID);

         if (targetEntity.Attributes.Contains(Case.StateCode) || targetEntity.Attributes.Contains(Case.StatusCode) || targetEntity.Attributes.Contains(Case.Owner))
         {
            var apiConfigurationEntity = CRMHelper.GetAPIConfiguration(service, tracingService);
            if (apiConfigurationEntity == null)
            {
               tracingService.Trace("No API configuration");
               return;
            }
            var uiJsonObject = CRMHelper.GetUiJsonObject(apiConfigurationEntity);

            if (targetEntity.Attributes.Contains(Case.Owner) && uiJsonObject.AssignmentSync)
            {
               if (targetEntity.Attributes.Contains(Case.SkipAssignCallback) && targetEntity.GetAttributeValue<Boolean>(Case.SkipAssignCallback))
               {
                  tracingService.Trace("Skip Owner Change Callback");
                  var caseEntity = new Entity(Case.LogicalName) { Id = targetEntity.Id };
                  caseEntity[Case.SkipAssignCallback] = false;
                  service.Update(caseEntity);
                  return;
               }
               tracingService.Trace("Owner Changed");
               var ownerId = targetEntity.GetAttributeValue<EntityReference>(Case.Owner).Id;
               tracingService.Trace("Owner Id" + ownerId);
               var owner = service.Retrieve(SystemUser.LogicalName, ownerId, new ColumnSet(SystemUser.LocobuzzID));
               var locobuzzUserId = 0;
               if (owner.Contains(SystemUser.LocobuzzID) && int.TryParse(owner.GetAttributeValue<string>(SystemUser.LocobuzzID), out int userId))
               {
                  locobuzzUserId = userId;
               }
               tracingService.Trace("Locobuzz Owner Id" + locobuzzUserId);
               TicketAssignmentChange ticketStatusChange = new TicketAssignmentChange()
               {
                  TicketID = Convert.ToInt32(locobuzzID),
                  AssignedToUserID = locobuzzUserId,
                  UserId = contextUserId.ToString(),
                  BrandGUID = apiConfigurationEntity.GetAttributeValue<string>(LocobuzzAPIConfiguration.BrandID)
               };

               HttpResponseMessage response = CRMHelper.CallAPI("/api/IntegratedCrm/TicketAssignmentChange", apiConfigurationEntity, ticketStatusChange, tracingService);

               CRMHelper.ProcessResponce(service, tracingService, response, "TicketAssignmentChange", "Case");
            }
            else if (uiJsonObject.StatusSync)
            {
               tracingService.Trace("Status Changed");
               var status = entity.GetAttributeValue<OptionSetValue>(Case.StateCode).Value;
               var statusReason = entity.GetAttributeValue<OptionSetValue>(Case.StatusCode).Value;
               var entityMappingConfigEntity = GetEntityMappingConfig(service);
               var fieldMappingObject = GetFieldMappingObject(entityMappingConfigEntity);
               var configObject = fieldMappingObject.Option[0].OptionValue.Where(op => statusReason == op.CRMOptionSetValue && status == op.CRMOptionSetParent).FirstOrDefault();
               if (configObject != null)
               {
                  var statusReasonText = entity.FormattedValues[Case.StatusCode];
                  var resolution = GetCaseResolution(service, entity.Id);

                  if (!string.IsNullOrEmpty( resolution))
                  {
                     AddTicketNoteCrm ticketNote = new AddTicketNoteCrm()
                     {
                        TicketID = Convert.ToInt32(locobuzzID),
                        Note = resolution,
                        UserId = contextUserId.ToString(),
                        BrandGUID = apiConfigurationEntity.GetAttributeValue<string>(LocobuzzAPIConfiguration.BrandID)
                     };
                     HttpResponseMessage noteResponse = CRMHelper.CallAPI("/api/IntegratedCrm/AddTicketNoteCrm", apiConfigurationEntity, ticketNote, tracingService);

                     CRMHelper.ProcessResponce(service, tracingService, noteResponse, "AddTicketNoteCrm", "Case");

                  }



                  TicketStatusChange ticketStatusChange = new TicketStatusChange()
                  {
                     TicketID = Convert.ToInt32(locobuzzID),
                     Status = configObject.LocobuzzOptionSetValue,
                     UserId = contextUserId.ToString(),
                     BrandGUID = apiConfigurationEntity.GetAttributeValue<string>(LocobuzzAPIConfiguration.BrandID)
                  };

                  HttpResponseMessage response = CRMHelper.CallAPI("/api/IntegratedCrm/TicketStatusChange", apiConfigurationEntity, ticketStatusChange, tracingService);

                  CRMHelper.ProcessResponce(service, tracingService, response, "TicketStatusChange", "Case");
               }
               else
               {
                  tracingService.Trace("No status mapping for locobuzz");
               }
            }
         }
         else
         {
            tracingService.Trace("No fields change");
         }
      }
      
      private FieldMapping GetFieldMappingObject(Entity entityMappingConfigEntity)
      {
         var fieldMapping = entityMappingConfigEntity.GetAttributeValue<string>(LocobuzzEntiytMappingConfiguration.FieldMapping);

         var fieldMappingObject = new FieldMapping();
         using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(fieldMapping)))
         {
            DataContractJsonSerializer deSerializer = new DataContractJsonSerializer(typeof(FieldMapping));
            fieldMappingObject = (FieldMapping)deSerializer.ReadObject(stream);
         }
         return fieldMappingObject;
      }

      private Entity GetEntityMappingConfig(IOrganizationService service)
      {
         var query = new QueryExpression(Entities.EntiytMappingConfiguration);
         query.ColumnSet.AddColumn(LocobuzzEntiytMappingConfiguration.FieldMapping);
         query.Criteria.AddCondition(LocobuzzEntiytMappingConfiguration.EntityType, ConditionOperator.Equal, 851720000);
         return service.RetrieveMultiple(query).Entities.First();
      }

      private string GetCaseResolution(IOrganizationService service,Guid caseId)
      {
         var query = new QueryExpression(CaseResolution.LogicalName);
         query.ColumnSet.AddColumn(CaseResolution.Resolution);
         query.Criteria.AddCondition(CaseResolution.Case, ConditionOperator.Equal, caseId);
         var caseResolution= service.RetrieveMultiple(query).Entities.FirstOrDefault();

         return caseResolution?.GetAttributeValue<string>(CaseResolution.Resolution);

      }
   }
}
