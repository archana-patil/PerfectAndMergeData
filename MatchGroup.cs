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
    
    public partial class MatchGroup
    {
        public MatchGroup()
        {
            this.MatchGroupId = Guid.NewGuid();
            this.MatchAttributeSettings = new HashSet<MatchAttributeSetting>();
            this.SessionGroups = new HashSet<SessionGroup>();
        }
    
        public System.Guid MatchGroupId { get; set; }
        public string DisplayName { get; set; }
        public System.Guid EntitySettingId { get; set; }
        public Nullable<bool> IsMaster { get; set; }
        public Nullable<System.Guid> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.Guid> UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdateDate { get; set; }
        public Nullable<int> DisplayOrder { get; set; }
        public Nullable<System.Guid> PriorityId { get; set; }
        public Nullable<bool> ExcludeFromMasterKey { get; set; }
        public Nullable<System.Guid> MatchKeyID { get; set; }
    
        public virtual EntitySetting EntitySetting { get; set; }
        public virtual ICollection<MatchAttributeSetting> MatchAttributeSettings { get; set; }
        public virtual MatchKeyMaster MatchKeyMaster { get; set; }
        public virtual PriorityMaster PriorityMaster { get; set; }
        public virtual User User { get; set; }
        public virtual User User1 { get; set; }
        public virtual ICollection<SessionGroup> SessionGroups { get; set; }
    }
}