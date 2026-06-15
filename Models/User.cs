using System;

namespace SveshofReff.Models
{
    public class User
    {
        public int ID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ReferralCode { get; set; } = string.Empty;
        public int? InviterID { get; set; }
        public int PointsBalance { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
