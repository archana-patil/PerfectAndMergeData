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
    
    public partial class LanguageMaster
    {
        public LanguageMaster()
        {
            this.SessionThresholdSettings = new HashSet<SessionThresholdSetting>();
        }
    
        public System.Guid LanguageId { get; set; }
        public string LanguageName { get; set; }
        public string CultureCode { get; set; }
    
        public virtual ICollection<SessionThresholdSetting> SessionThresholdSettings { get; set; }
    }
}