using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class CTLCategoryDetail
    {
        public string CategoryDetaild { get; set; }
        public string CategoryId { get; set; }
        public string FromText { get; set; }
        public string ToText { get; set; }
        public DateTime? CreatedBy { get; set; }
        public string CreatedDate { get; set; }
        public DateTime? UpdateBy { get; set; }
        public string UpdateDate { get; set; }      
    }
}
