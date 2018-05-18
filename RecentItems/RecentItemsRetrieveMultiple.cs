using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Microsoft.Xrm.Sdk.Query;
using RYR.VirtualEntity.RecentItems.Definitions;

namespace RYR.VirtualEntity.RecentItems
{
    public class RecentItemsRetrieveMultiple : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(context.UserId);

            try
            {
                var query = context.InputParameterOrDefault<QueryExpression>("Query");
                var visitor = new RecentItemsQueryVisitor();
                query.Accept(visitor);
                var results = new EntityCollection();
                using (var c = new CrmContext(service))
                {
                    List<Guid> users;
                    if (visitor.ConditionAttribute == RecentItem.UserId)
                    {
                        users = (from u in c.CreateQuery<SystemUser>()
                            where u.Id == (visitor.ConditionValue != null ? new Guid(visitor.ConditionValue.ToString()) : context.UserId)
                            where u.IsDisabled == false
                            select u.Id).ToList();
                    }
                    else
                    {
                        users = (from u in c.CreateQuery<SystemUser>()
                            where u.IsDisabled == false
                            select u.Id).ToList();
                    }

                    var recentItems = Helper
                        .RetrieveRecentItemsForUsers(users, factory, visitor.ConditionAttribute, visitor.ConditionValue, visitor.ConditionType)
                        .Select(Helper.CreateRecentItem).ToList();
                    results.Entities.AddRange(recentItems);
                }
                context.OutputParameters["BusinessEntityCollection"] = results;
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}