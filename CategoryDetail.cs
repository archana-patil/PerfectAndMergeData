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
    
    public partial class CategoryDetail
    {
        public System.Guid CategoryDetaild { get; set; }
        public Nullable<System.Guid> CategoryId { get; set; }
        public string FromText { get; set; }
        public string ToText { get; set; }
        public Nullable<bool> IsMaster { get; set; }
        public Nullable<System.Guid> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.Guid> UpdateBy { get; set; }
        public Nullable<System.DateTime> UpdateDate { get; set; }
    
        public virtual CategoryMaster CategoryMaster { get; set; }
    }
}
