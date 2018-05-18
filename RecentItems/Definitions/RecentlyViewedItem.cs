using System;
using Microsoft.Xrm.Sdk;

namespace RYR.VirtualEntity.RecentItems.Definitions
{
    public class RecentlyViewedItem
    {
        public RecentlyViewedType Type { get; set; }
        public Guid? ObjectId { get; set; }
        public int EntityTypeCode { get; set; }
        public string DisplayName { get; set; }
        public string Title { get; set; }
        public string Action { get; set; }
        public string IconPath { get; set; }
        public bool PinStatus { get; set; }
        public Guid? ProcessInstanceId { get; set; }
        public Guid? ProcessId { get; set; }
        public DateTime LastAccessed { get; set; }
        public EntityReference User { get; set; }
    }
}