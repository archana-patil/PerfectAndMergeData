using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAMBestRecordDetectionSettings
    {
        public System.Guid Id { get; set; }
        public System.Guid EntitySettingId { get; set; }
        public System.Guid RuleId { get; set; }
        public string RuleParam { get; set; }
        public System.Guid? RuleParamId { get; set; }
        public string RuleName { get; set; }
        public string RuleEnum { get; set; }
        public bool Account { get; set; }
        public bool Contact { get; set; }
        public bool Lead { get; set; }
        public string text { get; set; }
        public string id { get; set; }
        public bool leaf { get; set; }
        public string qtip { get; set; }
        public string cls { get; set; }
        public List<PAMBestRecordDetectionSettings> children;
    }
}
