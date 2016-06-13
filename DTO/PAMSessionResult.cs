using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAMSessionResult
    {
        public int SNo { get; set; }
        public string Name { get; set; }
        public string RecordName { get; set; }
        public decimal GroupNo { get; set; }
        public int? GroupRank { get; set; }
        public bool Primary { get; set; }
        public string ValidGroup { get; set; }
        public int? MasterRank { get; set; }
        public string CreatedOn { get; set; }
        public string ReviewStatus { get; set; }
        public string RecordID { get; set; }
        public string Session { get; set; }
        public string PunchIn { get; set; }
        public string PunchOut { get; set; }
        public string Reviewer { get; set; }
        public string SessionResultId { get; set; }
        public string CurrentStatusDateTime { get; set; }
        public string UserId { get; set; }
        public bool AutoPromote { get; set; }
        public bool AutoFill { get; set; }
        public bool AutoPromoteFill { get; set; }
    }
}
