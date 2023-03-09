using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using proMX.Locobuzz.Plugins.WellKnown;
using System;

namespace proMX.Locobuzz.Plugins
{
   public class lbz_locobuzzapiconfiguration_Create : IPlugin
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
            Implementation(service, tracing, apiConfig);
            tracing.Trace("Task Implemented successfully");
         }
         catch (Exception ex)
         {
            tracing.Trace(ex.Message);
            throw new InvalidPluginExecutionException(ex.Message);
         }
      }
      private void Implementation(IOrganizationService service, ITracingService tracing, Entity apiConfig)
      {
         tracing.Trace("Deleting all the old exisiting records which having the same EntityType");
         var entityColl = GetEntityMapConfigRecords(service,tracing,apiConfig);
         foreach (var listRecord in entityColl.Entities)
         {
            service.Delete(APIConfiguration.LogicalName, listRecord.GetAttributeValue<Guid>(APIConfiguration.Id));
         }
      }
      private EntityCollection GetEntityMapConfigRecords(IOrganizationService service, ITracingService tracing,Entity apiConfig)
      {
         tracing.Trace("Retrieve all the exisiting record on the basis of attribute CreatedOn in descending order");
         var id = apiConfig.Id;
         var getListOfRecords = new QueryExpression(APIConfiguration.LogicalName);
         getListOfRecords.Criteria.AddCondition(APIConfiguration.Id, ConditionOperator.NotEqual, id);
         getListOfRecords.Criteria.AddCondition(APIConfiguration.StatusCode, ConditionOperator.Equal, 1);
         return service.RetrieveMultiple(getListOfRecords);
      }
   }
}
