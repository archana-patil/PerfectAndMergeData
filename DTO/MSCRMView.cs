using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class MSCRMView
    {
        public  Nullable<Guid> ViewId { get; set; }
        public string EntityName { get; set; }
        public string ViewName { get; set; }
        public string FetchXML { get; set; }
    }
}
