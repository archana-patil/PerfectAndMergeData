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
    
    public partial class SessionBestRecordSetting
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> SessionId { get; set; }
        public Nullable<System.Guid> BestRecordDetectionSettingsId { get; set; }
        public Nullable<System.Guid> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.Guid> UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
    
        public virtual BestRecordDetectionSetting BestRecordDetectionSetting { get; set; }
        public virtual Session Session { get; set; }
    }
}
