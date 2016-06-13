using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAMCRMUser : IEquatable<PAMCRMUser>
    {
        public System.Guid CRMUserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public bool Equals(PAMCRMUser other)
        {
            //Check whether the compared object is null.
            if (Object.ReferenceEquals(other, null)) return false;

            //Check whether the compared object references the same data.
            if (Object.ReferenceEquals(this, other)) return true;

            //Check whether the PAMCRMUser's properties are equal.
            return CRMUserId.Equals(other.CRMUserId);
        }
        public override int GetHashCode()
        {
            //Get hash code for the CRMUserId field if it is not null.
            int hashUserId = CRMUserId == null ? 0 : CRMUserId.GetHashCode();
            return hashUserId;
        }

    }
}
