//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PerfectAndMergeData
{
    using System;
    using System.Collections.Generic;
    
    public partial class BestRecordRuleParametersMaster
    {
        public BestRecordRuleParametersMaster()
        {
            this.BestRecordRuleParameters = new HashSet<BestRecordRuleParameter>();
            this.BestRecordDetectionSettings = new HashSet<BestRecordDetectionSetting>();
        }
    
        public System.Guid Id { get; set; }
        public string Parameter { get; set; }
        public string ParameterEnum { get; set; }
    
        public virtual ICollection<BestRecordRuleParameter> BestRecordRuleParameters { get; set; }
        public virtual ICollection<BestRecordDetectionSetting> BestRecordDetectionSettings { get; set; }
    }
}
