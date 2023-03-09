using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using proMX.Locobuzz.Plugins.WellKnown;
using System;
using proMX.Locobuzz.Plugins.JsonClass;
using proMX.Locobuzz.Plugins.HelperClass;
using System.Net.Http;

namespace proMX.Locobuzz.Plugins
{
   public class annotation_Create : IPlugin
   {
      public void Execute(IServiceProvider serviceProvider)
      {
         IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
         ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
         try
         {
            tracing.Trace("Plugin Execution Started");
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);
            Entity apiConfig = context.InputParameters["Target"] as Entity;
            Implementation(service, tracing, apiConfig,context.UserId);
            tracing.Trace("Plugin Execution Stopped");
         }
         catch (Exception ex)
         {
            tracing.Trace(ex.Message);
            throw new InvalidPluginExecutionException(ex.Message);
         }
      }
      private void Implementation(IOrganizationService service, ITracingService tracingService, Entity noteEntity,Guid contextUserId)
      {
         if (noteEntity.Contains(Annotation.Regarding))
         {

            var regardingObject = noteEntity.GetAttributeValue<EntityReference>(Annotation.Regarding);
            if (regardingObject.LogicalName == Case.LogicalName)
            {
               var caseEntity = service.Retrieve(Case.LogicalName, regardingObject.Id, new ColumnSet(Case.LocobuzzID));
               if (caseEntity.Contains(Case.LocobuzzID) && caseEntity.GetAttributeValue<string>(Case.LocobuzzID) != string.Empty)
               {
                  var apiConfigurationEntity = CRMHelper.GetAPIConfiguration(service, tracingService);
                  if (apiConfigurationEntity == null)
                  {
                     tracingService.Trace("No API configuration");
                     return;
                  }
                  var uiJsonObject = CRMHelper.GetUiJsonObject(apiConfigurationEntity);
                  if (uiJsonObject.NoteSync)
                  {
                     AddTicketNoteCrm ticketStatusChange = new AddTicketNoteCrm()
                     {
                        TicketID = Convert.ToInt32(caseEntity.GetAttributeValue<string>(Case.LocobuzzID)),
                        Note = noteEntity.GetAttributeValue<string>(Annotation.Notes),
                        UserId = contextUserId.ToString(),
                        BrandGUID = apiConfigurationEntity.GetAttributeValue<string>(LocobuzzAPIConfiguration.BrandID)
                     };

                     HttpResponseMessage response = CRMHelper.CallAPI("/api/IntegratedCrm/AddTicketNoteCrm", apiConfigurationEntity, ticketStatusChange, tracingService);

                     CRMHelper.ProcessResponce(service, tracingService, response, "AddTicketNoteCrm", "Notes");
                  }
               }
            }
         }
      }
   }
}
