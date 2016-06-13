using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAMTheme
    {
        public System.Guid ThemeId { get; set; }
        public string Name { get; set; }
        public string ThemeFileName { get; set; }
        public Nullable<bool> IsApplied { get; set; }
    }


}
