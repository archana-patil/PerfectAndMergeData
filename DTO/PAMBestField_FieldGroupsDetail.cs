using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAMBestField_FieldGroupsDetail
    {
        public string EntitySettingId { get; set; }
        public string BestFieldDetGroupMasterId { get; set; }
        public string BestFieldDetGroupEntitywiseDetailId { get; set; }
        public string FieldGroupName { get; set; }
        public string FieldSchemaName { get; set; }
        public string FieldDisplayName { get; set; }
        public string text { get; set; }
        public string id { get; set; }
        public bool leaf { get; set; }
        public string qtip { get; set; }
        public string cls { get; set; }
        public List<PAMBestField_FieldGroupsDetail> children;
    }
}
