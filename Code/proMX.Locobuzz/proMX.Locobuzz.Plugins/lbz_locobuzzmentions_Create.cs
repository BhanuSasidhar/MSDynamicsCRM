using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using proMX.Locobuzz.Plugins.HelperClass;
using proMX.Locobuzz.Plugins.WellKnown;
using System;

namespace proMX.Locobuzz.Plugins
{
   public class lbz_locobuzzmentions_Create : IPlugin
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

            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity)) return;

            if (context.MessageName != "Create") return;

            Entity mention = context.InputParameters["Target"] as Entity;
            Implementation(service, tracing, mention);

            var locobuzzID = mention.GetAttributeValue<String>(Case.LocobuzzID);
            if (locobuzzID != null)
            {
               var incident = mention.GetAttributeValue<EntityReference>(LocobuzzMentions.Case);
               var channelGroupName = mention.GetAttributeValue<string>(LocobuzzMentions.ChannelGroupName);

               var incidentEntity = service.Retrieve(Case.LogicalName, incident.Id, new ColumnSet(Case.Title, Case.Priority, Case.Owner));
               var mentionEntity = CRMHelper.RetrieveMention(service, tracing, incidentEntity);

               if (mentionEntity.Entities.Count == 1)
               {
                  tracing.Trace($"Notification From {incidentEntity.LogicalName}");
                  CRMHelper.CreateNotification(service, tracing, incidentEntity, channelGroupName + " - New Case is Assigned to You.");
               }
               else if (mentionEntity.Entities.Count > 1)
               {
                  tracing.Trace($"Notification From {mentionEntity.Entities[0].LogicalName}");
                  CRMHelper.CreateNotification(service, tracing, mention, "New Mention is Added to the Case.");
               }
            }
            tracing.Trace("Plugin Execution Stopped");
         }
         catch (Exception ex)
         {
            tracing.Trace(ex.ToString());
            throw new InvalidPluginExecutionException(ex.Message);
         }
      }
      
      private void Implementation(IOrganizationService service, ITracingService tracing, Entity mention)
      {
         if ((mention.Contains(LocobuzzMentions.Title) || mention.Contains(LocobuzzMentions.Description)) && mention.Contains(LocobuzzMentions.Case))
         {
            Entity caseEntity = new Entity(Case.LogicalName);
            caseEntity.Id = mention.GetAttributeValue<EntityReference>(LocobuzzMentions.Case).Id;
            bool needSaparator = !string.IsNullOrEmpty(mention.GetAttributeValue<string>(LocobuzzMentions.Title)) && !string.IsNullOrEmpty(mention.GetAttributeValue<string>(LocobuzzMentions.Description));
            string title = $"{(mention.GetAttributeValue<string>(LocobuzzMentions.Title) + (needSaparator ? " - " : "") + mention.GetAttributeValue<string>(LocobuzzMentions.Description))}";
            if (title.Length > 100)
               title = title.Substring(0, 100);
            caseEntity[Case.Title] = title;
            service.Update(caseEntity);
            tracing.Trace("Case Updated with Title:" + caseEntity[Case.Title]);
         }
      }
   }
}
