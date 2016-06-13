using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class SessionMatchGroupAttributeSetting
    {
        public string MatchGroupId { get; set; }
        public string GroupName { get; set; }
        public string MatchAttributeSettingId { get; set; }
        public string EntitySettingId { get; set; }
        public string SchemaName { get; set; }
        public string DisplayName { get; set; }
        public string text { get; set; }
        public string id { get; set; }
        public bool leaf { get; set; }
        public string qtip { get; set; }
        public string cls { get; set; }
        public string SessionId { get; set; }
        public string PriorityId { get; set; }
        public bool ExcludeFromMasterKey { get; set; }
        public string MatchKeyID { get; set; }
        public string DisplayOrder { get; set; }
        public List<SessionMatchGroupAttributeSetting> children;
    }
}
