using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAMStatusReasonSettings
    {
        public System.Guid StatusReasonSettingsId { get; set; }
        public System.Guid EntitySettingId { get; set; }
        public string EntityName { get; set; }
        public string StatusReasonFieldSchema { get; set; }
        public string StatusReasonFieldDisplay { get; set; }
    }
}
