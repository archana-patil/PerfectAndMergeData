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
    
    public partial class BestFieldDetectionRuleType
    {
        public BestFieldDetectionRuleType()
        {
            this.BestFieldDetectionRules = new HashSet<BestFieldDetectionRule>();
        }
    
        public System.Guid Id { get; set; }
        public string RuleType { get; set; }
        public string RuleTypeEnum { get; set; }
        public Nullable<bool> Account { get; set; }
        public Nullable<bool> Contact { get; set; }
        public Nullable<bool> Lead { get; set; }
    
        public virtual ICollection<BestFieldDetectionRule> BestFieldDetectionRules { get; set; }
    }
}
