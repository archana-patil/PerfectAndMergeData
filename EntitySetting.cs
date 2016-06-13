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
    
    public partial class EntitySetting
    {
        public EntitySetting()
        {
            this.AutoMergeLogDetails = new HashSet<AutoMergeLogDetail>();
            this.EntityAutoMergeRules = new HashSet<EntityAutoMergeRule>();
            this.MatchAttributeSettings = new HashSet<MatchAttributeSetting>();
            this.MatchGroups = new HashSet<MatchGroup>();
            this.Sections = new HashSet<Section>();
            this.SuppressionHstories = new HashSet<SuppressionHstory>();
            this.AutoPromoteAndFillLogDetails = new HashSet<AutoPromoteAndFillLogDetail>();
            this.AttributeSettings = new HashSet<AttributeSetting>();
            this.Sessions = new HashSet<Session>();
        }
    
        public System.Guid EntitySettingId { get; set; }
        public string EntityLogicalName { get; set; }
        public string EntityDisplayName { get; set; }
    
        public virtual ICollection<AutoMergeLogDetail> AutoMergeLogDetails { get; set; }
        public virtual ICollection<EntityAutoMergeRule> EntityAutoMergeRules { get; set; }
        public virtual ICollection<MatchAttributeSetting> MatchAttributeSettings { get; set; }
        public virtual ICollection<MatchGroup> MatchGroups { get; set; }
        public virtual ICollection<Section> Sections { get; set; }
        public virtual ICollection<SuppressionHstory> SuppressionHstories { get; set; }
        public virtual ICollection<AutoPromoteAndFillLogDetail> AutoPromoteAndFillLogDetails { get; set; }
        public virtual ICollection<AttributeSetting> AttributeSettings { get; set; }
        public virtual ICollection<Session> Sessions { get; set; }
    }
}