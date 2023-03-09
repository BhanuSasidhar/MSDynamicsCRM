using Microsoft.Xrm.Sdk;
using System;
using proMX.Locobuzz.Plugins.WellKnown;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace proMX.Locobuzz.Plugins.Actions
{
   public class ValidateManageAccessCode : IPlugin
   {
      /// <summary>
      /// Validate if the given AccessCode with given Userid ManageAccessCode is active 
      /// If it is active set IsValid  to "true" and make it inactive, else set IsValid to "false".
      /// </summary>
      /// <history>
      /// 02.01.2023 : Created : Vijay Kumar
      /// 23.01.2023 : Modified : Vijay Kumar
      /// 27.02.2023 : Modified : Vijay kumar 
      /// </history>
      /// <remarks>
      /// Registration:
      /// lbz_ValidateManageAccessCodes Action, PostOperation,Sync
      /// Attributes: n/a
      /// </remarks>
      public void Execute(IServiceProvider serviceProvider)
      {
         IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
         ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
         try
         {

            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);


            Guid userId = Guid.Parse((string)context.InputParameters[ProcessAction.UserID]);

            Guid accessCodeId = Guid.Parse((string)context.InputParameters[ProcessAction.AccessCodeID]);

            context.OutputParameters[ProcessAction.IsValid] = Implementation(service, userId, accessCodeId);
            tracing.Trace("calling action: Validate manage access record");
         }
         catch (Exception ex)
         {
            tracing.Trace(ex.Message);
            throw new InvalidPluginExecutionException(ex.Message);
         }
      }

      private bool Implementation(IOrganizationService service, Guid userId, Guid accessCodeId)
      {
         Entity collManagedAccessCode = RetrieveRecord(service, userId, accessCodeId);

         if (collManagedAccessCode == null)
         {
            return false;
         }

         var manageAccessCodeID = collManagedAccessCode.Id;
         Entity manageAccessCode = new Entity(ManageAccessCode.LogicalName);
         manageAccessCode.Id = manageAccessCodeID;
         manageAccessCode.Attributes[ManageAccessCode.Status] = new OptionSetValue(1);
         manageAccessCode.Attributes[ManageAccessCode.StatusReason] = new OptionSetValue(2);
         service.Update(manageAccessCode);
         return true;
      }

      private Entity RetrieveRecord(IOrganizationService service, Guid userId, Guid accessCodeId)
      {
         var getListOfRecord = new QueryExpression(ManageAccessCode.LogicalName);
         getListOfRecord.Criteria.AddCondition(ManageAccessCode.UserId, ConditionOperator.Equal, userId);
         getListOfRecord.Criteria.AddCondition(ManageAccessCode.ManageAccessCodeId, ConditionOperator.Equal, accessCodeId);
         getListOfRecord.Criteria.AddCondition(ManageAccessCode.Status, ConditionOperator.Equal, 0);
         return service.RetrieveMultiple(getListOfRecord).Entities.FirstOrDefault();

      }
   }
}
