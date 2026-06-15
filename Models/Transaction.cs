using System;

namespace SveshofReff.Models
{
    public class Transaction
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public int PointsAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
    }
}
