using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class TimeZone
    {
        public string TimeZoneId { get; set; }
        public string DisplayName { get; set; }
        public string DaylightName { get; set; }
        public string Offset { get; set; }
    }
}
