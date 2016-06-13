using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData
{
    public partial class SuppressionSetting
    {
        public SuppressionSetting()
        {
            this.SuppressionSettingsId = Guid.NewGuid();
        }
    }
}
