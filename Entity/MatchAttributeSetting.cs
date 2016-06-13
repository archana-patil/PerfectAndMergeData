using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData
{
    public partial class MatchAttributeSetting
    {
        public MatchAttributeSetting()
        {
            this.MatchAttributeSettingId = Guid.NewGuid();
        }

        public string GroupDisplayName { get; set; }
        public string PriorityName { get; set; }
        public bool ExcludeFromMasterKey { get; set; }
        public string MatchKey { get; set; }
        public string Priority { get; set; }
    }
}
