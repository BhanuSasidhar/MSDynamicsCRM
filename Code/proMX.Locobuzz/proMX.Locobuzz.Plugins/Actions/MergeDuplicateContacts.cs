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
   /// Merge the Duplicate Contacts to the Main Contact record which are specified by Locobuzz.
   /// Copying the specified field values to the main contact, if the data exist.
   /// ///Also it will not overwrite the values if the Main record contains value for any specific field.
   /// </summary>
   /// <history>
   /// 19.12.2022 : Created : Angarika Mane
   /// 18.01.2023 : Modified : Angarika Mane
   /// 23.01.2023 : Modified : Angarika Mane
   /// </history>
   /// <remarks>
   /// Registration:
   /// lbz_flowContactsMergeDuplicateContacts Action, PostOperation,Sync
   /// Attributes: n/a
   /// </remarks>
   public class MergeDuplicateContacts : IPlugin
   {
      public void Execute(IServiceProvider serviceProvider)
      {
         var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
         var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
         var service = serviceFactory.CreateOrganizationService(context.UserId);
         ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

         try
         {
            if (!context.InputParameters.Contains(Process.DuplicateContactInput) && !context.InputParameters.Contains(Process.MainContactInput)) return;

            var mainContactId = (string)context.InputParameters[Process.MainContactInput];
            var duplicateContactId = (string)context.InputParameters[Process.DuplicateContactInput];

            if (duplicateContactId == null || mainContactId == null) return;
            string[] duplicateContactIds = duplicateContactId.Split(',');

            Implementation(duplicateContactIds, mainContactId, service, tracingService);
         }
         catch (FaultException<OrganizationServiceFault> ex)
         {
            throw new InvalidPluginExecutionException(ex.Message);
         }
      }
      public void Implementation(string[] duplicateContactIds, string mainContactId, IOrganizationService service, ITracingService tracingService)
      {
         tracingService.Trace("Called Implementation method");
         var mainContactGuid = Guid.Parse(mainContactId);
         tracingService.Trace("Converting the string of main case id to GUID");

         foreach (var contactId in duplicateContactIds)
         {
            var duplicateContactGuid = Guid.Parse(contactId);
            tracingService.Trace("Converting the string of duplicate case id to GUID");

            Entity mainContactRecordValues = service.Retrieve(Entities.Contacts, mainContactGuid, new ColumnSet(true));
            Entity duplicateContactRecordValues = service.Retrieve(Entities.Contacts, duplicateContactGuid, new ColumnSet(true));
            var duplicateContactStatus = duplicateContactRecordValues.GetAttributeValue<OptionSetValue>(Contact.Status);

            //Contact recored being merged into(main)
            var target = new EntityReference();
            target.Id = mainContactGuid;
            target.LogicalName = Entities.Contacts;
            tracingService.Trace("Assigned the Target Id");

            //Contact record merging(duplicate)
            var merge = new MergeRequest();
            merge.SubordinateId = duplicateContactGuid;
            merge.Target = target;
            merge.PerformParentingChecks = false;
            tracingService.Trace("Assigned the Subordinate Id");

            ContactFieldsDetails contactFieldsToMap = null;
            var contactMappingConfigurationRecord = RetrieveMultipleContactMappingConfiguration(service, tracingService);

            var mappingRecord = contactMappingConfigurationRecord.Entities[0];
            var fieldMapping = mappingRecord.GetAttributeValue<string>(EntityMapppingConfiguration.FieldMapping);

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(fieldMapping)))
            {
               DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(ContactFieldsDetails));
               contactFieldsToMap = (ContactFieldsDetails)deserializer.ReadObject(ms);
            }
            tracingService.Trace("converting the class to object");

            if (contactFieldsToMap == null) return;
            List<string> contactAttributeList = new List<string>(contactFieldsToMap.Fields.Select(c => c.CRMLogicalName));
            tracingService.Trace("Getting the attribute List");

            //Create another contact to hold new data to merge into the entity.
            var updateContent = new Entity(Entities.Contacts);

            //storing a list of fields to be updated in the maincontact.
            foreach (var attribute in contactAttributeList)
            {
               if (attribute == null)
               {
                  tracingService.Trace($"Attribute value is {attribute}");
                  continue;
               }

               if (mainContactRecordValues.Contains(attribute))
               {
                  tracingService.Trace($"Main Contact Record value Exist: {mainContactRecordValues.Contains(attribute)}");
                  continue;
               }
               if (!duplicateContactRecordValues.Contains(attribute))
               {
                  tracingService.Trace($"Duplicate Contact Record value Exist: {!duplicateContactRecordValues.Contains(attribute)}");
                  continue;
               }
               updateContent[attribute] = duplicateContactRecordValues[attribute];
            }
            UpdateSocialProfile(service, mainContactGuid, mainContactRecordValues, tracingService);

            merge.UpdateContent = updateContent;

            var merged = (MergeResponse)service.Execute(merge);
            tracingService.Trace("Records Merged");
         }
      }
      /// <summary>
      /// Update the Social Profiles Contact field with the value of the Main Contact record.
      /// </summary>
      /// <param name="service"></param>
      /// <param name="mainContactGuid"></param>
      /// <param name="mainContactRecordValues"></param>
      public void UpdateSocialProfile(IOrganizationService service, Guid mainContactGuid, Entity mainContactRecordValues, ITracingService tracingService)
      {
         tracingService.Trace("Called UpdateSocialProfile method");
         if (mainContactRecordValues.GetAttributeValue<string>(Contact.LocobuzzId) != null)
         {

            var contactLocobuzzId = mainContactRecordValues.GetAttributeValue<string>(Contact.LocobuzzId);

            // Define Condition Values
            EntityCollection socialProfileRecord = RetrieveSocialProfiles(service, contactLocobuzzId, tracingService);

            foreach (var profile in socialProfileRecord.Entities)
            {
               profile[LocobuzzSocialProfiles.Contact] = new EntityReference(Entities.Contacts, mainContactGuid);//mainContactRecordValues.GetAttributeValue<EntityReference>(mainContactGuid.ToString());
               service.Update(profile);
            }

         }
      }
      /// <summary>
      /// Retrieve Locobuzz Social Profiles that are Active and have Locobuzz Id same as the main Contact Record.
      /// </summary>
      /// <param name="service"></param>
      /// <param name="contactLocobuzzId"></param>
      public EntityCollection RetrieveSocialProfiles(IOrganizationService service, string contactLocobuzzId, ITracingService tracingService)
      {
         tracingService.Trace("Called RetrieveSocialProfiles method");
         var locobuzzId = contactLocobuzzId;
         var statecode = 0;
         var socialProfiles = new QueryExpression(Entities.LocobuzzSocialProfiles);
         socialProfiles.ColumnSet.AllColumns = true;
         socialProfiles.Criteria.AddCondition("lbz_locobuzzid", ConditionOperator.Equal, locobuzzId);
         socialProfiles.Criteria.AddCondition("statecode", ConditionOperator.Equal, statecode);

         return service.RetrieveMultiple(socialProfiles);
      }

      /// <summary>
      /// Retrieve Mapping records having Entity type as Contact and are in Active state.
      /// </summary>
      /// <param name="service"></param>

      public EntityCollection RetrieveMultipleContactMappingConfiguration(IOrganizationService service, ITracingService tracingService)
      {
         tracingService.Trace("Called RetrieveMultipleContactMappingConfiguration method");
         var mappingConfigurationRecord = new QueryExpression(Entities.EntiytMappingConfiguration);
         mappingConfigurationRecord.ColumnSet.AllColumns = true;
         mappingConfigurationRecord.Criteria.AddCondition(EntityMapppingConfiguration.Status, ConditionOperator.Equal, 0);
         mappingConfigurationRecord.Criteria.AddCondition(EntityMapppingConfiguration.EntityType, ConditionOperator.Equal, 851720001);
         return service.RetrieveMultiple(mappingConfigurationRecord);
      }
   }
}
