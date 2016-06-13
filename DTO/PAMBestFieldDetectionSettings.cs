using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAMBestFieldDetectionSettings
    {
        public string Id { get; set; }
        public System.Guid RuleId { get; set; }
        public string RuleParam { get; set; }
        public System.Guid? RuleParamId { get; set; }
        public string RuleName { get; set; }
        public string RuleEnum { get; set; }
   //     public System.Guid? BestFieldDetGroupMasterId { get; set; }
        public System.Guid? BestFieldDetectionRuleTypesId { get; set; }
        public string RuleTypeEnum { get; set; }
        public System.Guid? BestFieldDetPicklistFieldsId { get; set; }
     //   public System.Guid? BestFieldDetGroupEntitywiseId { get; set; }
     //   public string GroupName { get; set; }
        public string PickListFieldSchemaName { get; set; }
        public System.Guid? AttributeSettingId { get; set; }
        public System.Guid? SectionId { get; set; }
       // public bool Account { get; set; }
      //  public bool Contact { get; set; }
      //  public bool Lead { get; set; }
        public List<HierarchyOfPickListFields> HierarchyRuleRecords { get; set; }
        public List<PickListScore> PickListScoreRecords { get; set; }
        public string text { get; set; }
        public string id { get; set; }
        public bool leaf { get; set; }
        public string qtip { get; set; }
        public string cls { get; set; }
        public List<PAMBestFieldDetectionSettings> children;
    }
}
