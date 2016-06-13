using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAMSectionAttributeSetting
    {
        public string MatchGroupId { get; set; }
        public string GroupName { get; set; }
        public string MatchAttributeSettingId { get; set; }
        public string EntitySettingId { get; set; }
        public string SchemaName { get; set; }
        public string DataType { get; set; }
        public string DisplayName { get; set; }
        public string text { get; set; }
        public string id { get; set; }
        public bool leaf { get; set; }
        public string qtip { get; set; }
        public string cls { get; set; }
        public string CustomName { get; set; }
        public string DisplayOrder { get; set; }
        public Nullable<bool> IsVisible { get; set; }
        public string SessionId { get; set; }
        public Nullable<bool> ExcludeUpdate { get; set; }
        public string SectionId { get; set; }
        public Nullable<bool> UseForAutoMerge { get; set; }
        public string PriorityId { get; set; }
        public bool ExcludeFromMasterKey { get; set; }
        public string MatchKeyID { get; set; }
        public List<PAMBestFieldDetectionSettings> BestFieldDetectionSettings { get; set; }
        public List<PAMSectionAttributeSetting> children;
    }
}
