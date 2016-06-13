
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAMMatchRuleListResultSet : ResultSet
    {
        public List<PAMMatchRule> MatchRules { get; set; }
    }
}
