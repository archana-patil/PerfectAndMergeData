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
    
    public partial class PriorityMaster
    {
        public PriorityMaster()
        {
            this.MatchGroups = new HashSet<MatchGroup>();
            this.SessionGroups = new HashSet<SessionGroup>();
        }
    
        public System.Guid PriorityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Nullable<decimal> Priority { get; set; }
    
        public virtual ICollection<MatchGroup> MatchGroups { get; set; }
        public virtual ICollection<SessionGroup> SessionGroups { get; set; }
    }
}
