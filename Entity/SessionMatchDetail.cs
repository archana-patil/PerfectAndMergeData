using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData
{
   public partial class SessionMatchDetail
    {
       public SessionMatchDetail()
       {
           this.SessionMatchId = Guid.NewGuid();
       }

    }
}
