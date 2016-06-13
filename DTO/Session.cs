using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAM2Session
    {
        public Guid SessionId { get; set; }
        public string SessionName  { get; set; }
        public string EntitySettingId { get; set; }
        public string StatusId { get; set; }
        public string Status { get; set; }
        public string CreatedDate { get; set; }
        public string GroupCount { get; set; }
        public string Entity { get; set; }
        public string RecordsFed { get; set; }
        public string DuplicatesFound { get; set; }
        public string ConfirmedResultCount { get; set; }
        public string UnsureResultCount { get; set; }
        public bool Selected { get; set; }
        public bool IsAutoMergeInProgress { get; set; }
        public bool IsDeactivated { get; set; }
        public string CreatedBy { get; set; }
        public string ExecutedOn { get; set; }
        public string ExecutionEnd { get; set; }
        public string AutoMergedCount { get; set; }
        public string MergedCount { get; set; }
        public bool IsAutoPromotedAll { get; set; }
        public bool IsAutoFillAll { get; set; }
        public bool IsAutoPromoteFillAll { get; set; }

        //Added By: Sameer Ahire
        //Date: 06-Jun-2016
        public string MatchStatus { get; set; }
    }
}
