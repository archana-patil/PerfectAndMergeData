using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
  public class EntityRecord
    {
      public string EntityID;
      public List<PAM1Attribute> Attributes;
      public bool IsPrimary;
      public string Entity;
    }
}
