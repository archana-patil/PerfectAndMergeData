using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
   public class ResultSet
    {
        public bool Result { get; set; }
        public string Message { get; set; }
        public int total { get; set; }
        public bool success { get; set; }
    }
}
