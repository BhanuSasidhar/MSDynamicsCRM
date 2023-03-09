using Microsoft.Xrm.Sdk;
using System;
using proMX.Locobuzz.Plugins.WellKnown;
using Microsoft.Xrm.Sdk.Query;

namespace proMX.Locobuzz.Plugins
{
   public class lbz_locobuzzentitymappingconfiguration_Create : IPlugin
   {
      public void Execute(IServiceProvider serviceProvider)
      {
         IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
         ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
         try
         {
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);
            Entity entityMappingCofig = context.InputParameters["Target"] as Entity;
            
            Implementation(service,tracing, entityMappingCofig);
            tracing.Trace("Task Implemented successfully");
         }
         catch (Exception ex)
         {
            tracing.Trace(ex.Message);
            throw new InvalidPluginExecutionException(ex.Message);
         }
      }
      private void Implementation(IOrganizationService service, ITracingService tracing,Entity entityMappingCofig)
      {
         tracing.Trace("Deleting all the old exisiting records which having the same EntityType");
         var entityColl = GetEntityMapConfigRecords(service,tracing, entityMappingCofig);
         foreach (var listRecord in entityColl.Entities)
         {
            service.Delete(EntityMapppingConfiguration.LogicalName, listRecord.GetAttributeValue<Guid>(EntityMapppingConfiguration.Id));
         }
      }
      private EntityCollection GetEntityMapConfigRecords(IOrganizationService service,ITracingService tracing, Entity entityMappingCofig)
      {
         tracing.Trace("Retrieve all the exisiting record on the basis of attribute EntityType");
         var id = entityMappingCofig.Id;
         var entityTypeValue = entityMappingCofig.GetAttributeValue<OptionSetValue>(EntityMapppingConfiguration.EntityType).Value;
         var getListOfRecords = new QueryExpression(EntityMapppingConfiguration.LogicalName);
         getListOfRecords.Criteria.AddCondition(EntityMapppingConfiguration.EntityType, ConditionOperator.Equal, entityTypeValue);
         getListOfRecords.Criteria.AddCondition(EntityMapppingConfiguration.Id, ConditionOperator.NotEqual, id);
         getListOfRecords.ColumnSet.AllColumns = true;
         return service.RetrieveMultiple(getListOfRecords);
      }
   }
}
