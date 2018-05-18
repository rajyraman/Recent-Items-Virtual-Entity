using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RYR.VirtualEntity.RecentItems.Definitions;

namespace RYR.VirtualEntity.RecentItems
{
    public class Helper
    {
        public static Entity CreateRecentItem(RecentlyViewedItem item)
        {
            var id = Guid.NewGuid();
            var result = new Entity(RecentItem.SchemaName)
            {
                Id = id,
                [RecentItem.RecentItemId] = id,
                [RecentItem.UserId] = item.User,
                [RecentItem.Name] = item.DisplayName,
                [RecentItem.Action] = item.Action,
                [RecentItem.EntityTypeCode] = item.EntityTypeCode,
                [RecentItem.ObjectId] = item.ObjectId?.ToString("B"),
                [RecentItem.PinStatus] = item.PinStatus,
                [RecentItem.Type] = new OptionSetValue((int)item.Type),
                [RecentItem.Title] = item.Title,
                [RecentItem.LastAccessed] = item.LastAccessed
            };
            return result;
        }

        public static List<RecentlyViewedItem> RetrieveRecentItemsForUsers(List<Guid> users, IOrganizationServiceFactory factory, string conditionAttribute, object conditionValue, ConditionOperator conditionType)
        {
            var result = new List<RecentlyViewedItem>();
            var recentlyViewedRecords = new List<Entity>();

            foreach (var userId in users)
            {
                try
                {
                    var service = factory.CreateOrganizationService(userId);
                    recentlyViewedRecords.AddRange(service.RetrieveMultiple(
                        new FetchExpression($@"
                            <fetch distinct='false' no-lock='false' mapping='logical'>
                              <entity name='userentityuisettings'>
                                <attribute name='recentlyviewedxml' />
                                <attribute name='ownerid' />
                                <filter type='and'>
                                  <condition attribute='ownerid' operator='eq' value='{userId}' />
                                </filter>
                              </entity>
                            </fetch>")).Entities.ToList());
                    foreach (var recentlyViewedRecord in recentlyViewedRecords)
                    {
                        if (string.IsNullOrEmpty(recentlyViewedRecord.GetAttributeValue<string>(RecentlyViewedXml.RootNode))) continue;

                        var recentlyViewedItemXml = XElement.Parse(recentlyViewedRecord.GetAttributeValue<string>(RecentlyViewedXml.RootNode));
                        var recentlyViewedItems = recentlyViewedItemXml.Descendants(RecentlyViewedXml.ItemNode);
                        foreach (var r in recentlyViewedItems)
                        {
                            var recentlyViewItem = new RecentlyViewedItem
                            {
                                User = recentlyViewedRecord.GetAttributeValue<EntityReference>(
                                    RecentlyViewedXml.OwnerId),
                                Type = (RecentlyViewedType) int.Parse(r.Element(RecentlyViewedXml.ItemType).Value),
                                ObjectId = Guid.TryParse(r.Element(RecentlyViewedXml.ObjectId)?.Value, out Guid o)
                                    ? o
                                    : (Guid?) null,
                                EntityTypeCode = int.Parse(r.Element(RecentlyViewedXml.EntityTypeCode)?.Value),
                                DisplayName =
                                    r.Element(RecentlyViewedXml.DisplayName)?.Value == "System Form"
                                        ? "Dashboard"
                                        : r.Element(RecentlyViewedXml.DisplayName)?.Value,
                                Title = r.Element(RecentlyViewedXml.Title)?.Value,
                                Action = r.Element(RecentlyViewedXml.Action)?.Value,
                                IconPath = r.Element(RecentlyViewedXml.IconPath)?.Value,
                                PinStatus = bool.Parse(r.Element(RecentlyViewedXml.PinStatus)?.Value),
                                ProcessInstanceId =
                                    Guid.TryParse(r.Element(RecentlyViewedXml.ProcessInstanceId)?.Value, out var pi)
                                        ? pi
                                        : (Guid?) null,
                                ProcessId = Guid.TryParse(r.Element(RecentlyViewedXml.ProcessId)?.Value, out var p)
                                    ? p
                                    : (Guid?) null,
                                LastAccessed = DateTime.Parse(r.Element(RecentlyViewedXml.LastAccessed)?.Value,
                                    new CultureInfo("en-US", false)).ToLocalTime()
                            };
                            if (conditionAttribute == null || (conditionAttribute == RecentItem.Type
                                && conditionType == ConditionOperator.Equal
                               && recentlyViewItem.Type == (RecentlyViewedType)conditionValue
                                && !result.Exists(x => x.ObjectId == recentlyViewItem.ObjectId
                                    && x.User.Id == recentlyViewItem.User.Id
                                    && x.LastAccessed.Equals(recentlyViewItem.LastAccessed))))
                            {
                                result.Add(recentlyViewItem);
                            };
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }

            var sorted = (from r in result
                         orderby r.LastAccessed  descending,r.User.Name
                         select r).ToList();
            return sorted;
        }
    }
}
