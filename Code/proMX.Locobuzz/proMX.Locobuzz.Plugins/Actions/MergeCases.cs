using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using proMX.Locobuzz.Plugins.JsonClass;
using proMX.Locobuzz.Plugins.WellKnown;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.Text;

namespace proMX.Locobuzz.Plugins.Actions
{
   /// <summary>
   /// Merge the Duplicate Case to the Main Case record which are specified by Locobuzz.
   /// Copying the specified field values to the main Case, if the data exist.
   /// Also it will not overwrite the values if the Main record contains value for any specific field.
   /// </summary>
   /// <history>
   /// 01.12.2022 : Created : Angarika Mane
   /// 19.12.2022 : Modified : Angarika Mane
   /// 18.01.2023 : Modified : Angarika Mane
   /// 23.01.2023 : Modified : Angarika Mane
   /// </history>
   /// <remarks>
   /// Registration:
   /// lbz_flowContactsMergeDuplicateContacts Action, PostOperation,Sync
   /// Attributes: n/a
   /// </remarks>
   public class MergeCases : IPlugin
   {
      public void Execute(IServiceProvider serviceProvider)
      {
         var context = (IPluginExecutionContext)
            serviceProvider.GetService(typeof(IPluginExecutionContext));
         var serviceFactory = (IOrganizationServiceFactory)
            serviceProvider.GetService(typeof(IOrganizationServiceFactory));
         var service = serviceFactory.CreateOrganizationService(context.UserId);
         ITracingService tracingService = (ITracingService)
            serviceProvider.GetService(typeof(ITracingService));

         try
         {
            if (!context.InputParameters.Contains(Process.DuplicateCaseInput) &&
               !context.InputParameters.Contains(Process.MainCaseInput)) return;

            var mainCaseId = (string)context.InputParameters[Process.MainCaseInput];
            var duplicateCaseId = (string)context.InputParameters[Process.DuplicateCaseInput];

            if (duplicateCaseId == null || mainCaseId == null) return;
            string[] duplicateCaseIds = duplicateCaseId.Split(',');
            tracingService.Trace("Storing the multiple Duplicate Ids in the form of array.");

            Implementation(duplicateCaseIds, mainCaseId, service, tracingService);
         }
         catch (FaultException<OrganizationServiceFault> ex)
         {
            throw new InvalidPluginExecutionException(ex.Message);
         }
      }
      /// <summary>
      /// Creating Merge Request and executing it to merge the duplicate and main case id.
      /// </summary>
      /// <param name="mainCaseId"></param>
      /// <param name="duplicateCaseIds"></param>
      /// <param name="service"></param>
      public void Implementation(string[] duplicateCaseIds, string mainCaseId, IOrganizationService service, ITracingService tracingService)
      {
         tracingService.Trace("Called Implementation method");
         var mainCaseGuid = Guid.Parse(mainCaseId);
         tracingService.Trace("Converting the string of main case id to GUID");

         foreach (var caseId in duplicateCaseIds)
         {
            var duplicateCaseGuid = Guid.Parse(caseId);
            tracingService.Trace("Converting the string of duplicate case id to GUID");

            Entity mainCaseRecordValues = service.Retrieve(Entities.Case, mainCaseGuid, new ColumnSet(true));
            Entity duplicateCaseRecordValues = service.Retrieve(Entities.Case, duplicateCaseGuid, new ColumnSet(true));
            var duplicateCaseStatus = duplicateCaseRecordValues.GetAttributeValue<OptionSetValue>(Case.StateCode);

            var target = new EntityReference();
            target.Id = mainCaseGuid;
            target.LogicalName = Entities.Case;
            tracingService.Trace("Assigned the Target Id");

            //Case record merging(duplicate)
            var merge = new MergeRequest();
            merge.SubordinateId = duplicateCaseGuid;
            merge.Target = target;
            merge.PerformParentingChecks = false;
            tracingService.Trace("Assigned the Subordinate Id");

            var caseMappingConfigurationRecord = RetrieveMultipleCaseMappingConfiguration(service, tracingService);

            var mappingRecord = caseMappingConfigurationRecord.Entities[0];
            var fieldMapping = mappingRecord.GetAttributeValue<string>(EntityMapppingConfiguration.FieldMapping);

            CaseFieldsToMap caseFieldsToMap = null;

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(fieldMapping)))
            {
               DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(CaseFieldsToMap));
               caseFieldsToMap = (CaseFieldsToMap)deserializer.ReadObject(ms);
            }
            tracingService.Trace("converting the class to object");
            if (caseFieldsToMap == null) return;

            List<string> caseAttributeList = new List<string>(caseFieldsToMap.Fields.Select(c => c.CRMLogicalName));
            tracingService.Trace("Getting the attribute List");
            //Create another case to hold new data to merge into the entity.
            var updateContent = new Entity(Entities.Case);

            //storing a list of fields to be updated in the maincase.
            foreach (var attribute in caseAttributeList)
            {
               if (attribute == null)
               {
                  tracingService.Trace($"Attribute value is {attribute}");
                  continue;
               }

               if (mainCaseRecordValues.Contains(attribute))
               {
                  tracingService.Trace($"Main Contact Record value Exist: {mainCaseRecordValues.Contains(attribute)}");
                  continue;
               }
               if (!duplicateCaseRecordValues.Contains(attribute))
               {
                  tracingService.Trace($"Duplicate Contact Record value Exist: {!duplicateCaseRecordValues.Contains(attribute)}");
                  continue;
               }
            }
            merge.UpdateContent = updateContent;

            var merged = (MergeResponse)service.Execute(merge);
            tracingService.Trace("Records Merged");
         }
      }
      public EntityCollection RetrieveMultipleCaseMappingConfiguration(IOrganizationService service, ITracingService tracingService)
      {
         tracingService.Trace("Called RetrieveMultipleCaseMappingConfiguration method");
         var mappingConfigurationRecord = new QueryExpression(Entities.EntiytMappingConfiguration);
         mappingConfigurationRecord.ColumnSet.AllColumns = true;
         mappingConfigurationRecord.Criteria.AddCondition(EntityMapppingConfiguration.Status, ConditionOperator.Equal, 0);
         mappingConfigurationRecord.Criteria.AddCondition(EntityMapppingConfiguration.EntityType, ConditionOperator.Equal, 851720000);
         return service.RetrieveMultiple(mappingConfigurationRecord);
      }
   }
}