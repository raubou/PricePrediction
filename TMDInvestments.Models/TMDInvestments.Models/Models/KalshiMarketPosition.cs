using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class KalshiMarketPosition
    {
        [Key]
        public long Id { get; set; }
        public string Ticker { get; set; }
        public decimal fees_paid_dollars { get; set; }
        public decimal market_exposure_dollars { get; set; }
        public decimal position_fp { get; set; }
        public decimal total_traded_dollars { get; set; }
        public bool IsClosed { get; set; }
        public bool IsReversed { get; set; }
        public DateTime? lastUpdated { get; set; }
        public DateTime Created { get; set; }
    }
}
