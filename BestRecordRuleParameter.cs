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
    
    public partial class BestRecordRuleParameter
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RuleId { get; set; }
        public Nullable<System.Guid> ParameterId { get; set; }
    
        public virtual BestRecordRuleParametersMaster BestRecordRuleParametersMaster { get; set; }
        public virtual BestRecordDetectionRule BestRecordDetectionRule { get; set; }
    }
}
