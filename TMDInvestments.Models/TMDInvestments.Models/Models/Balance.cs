using System;
using System.ComponentModel.DataAnnotations;


namespace TMDInvestments.Models
{
    public class Balance
    {
        [Key]
        public long? Id { get; set; }
        public int? TradePlatform { get; set; }
        public decimal? CurrentBalance { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
