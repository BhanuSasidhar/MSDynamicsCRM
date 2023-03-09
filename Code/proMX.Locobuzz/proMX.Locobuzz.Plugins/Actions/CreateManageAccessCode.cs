using Microsoft.Xrm.Sdk;
using System;
using proMX.Locobuzz.Plugins.WellKnown;

namespace proMX.Locobuzz.Plugins.Actions
{
   public class CreateManageAccessCode : IPlugin
   {
      /// <summary>
      /// Create New ManageAccessCodes with given UserId and return the created id to ManageAccessCodeId
      /// Create Org service without context user, Require admin scope only.
      /// </summary>
      /// <history>
      /// 02.01.2023 : Created : Vijay Kumar
      /// 23.01.2023 : Modified : Vijay Kumar
      /// 27.02.2023 : Modified : Vijay kumar 
      /// </history>
      /// <remarks>
      /// Registration:
      /// lbz_CreateManageAccessCodes Action, PostOperation,Sync
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
            context.OutputParameters[ProcessAction.ManageAccessCodeId] = Implementation(service, userId).ToString();
            tracing.Trace("calling action: create manage access record");
         }
         catch (Exception ex)
         {
            tracing.Trace(ex.Message);
            throw new InvalidPluginExecutionException(ex.Message);
         }
      }

      private static Guid Implementation(IOrganizationService service, Guid userID)
      {
         Entity manageAccessCode = new Entity(ManageAccessCode.LogicalName);
         manageAccessCode.Attributes[ManageAccessCode.UserId] = new EntityReference("systemuser", userID);
         manageAccessCode.Attributes[ManageAccessCode.Status] = new OptionSetValue(0);
         manageAccessCode.Attributes[ManageAccessCode.StatusReason] = new OptionSetValue(1);
         var manageAccesscodeID = service.Create(manageAccessCode);
         return manageAccesscodeID;
      }
   }
}
