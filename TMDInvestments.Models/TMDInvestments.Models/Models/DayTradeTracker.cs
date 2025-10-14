using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class DayTradeTracker
    {
        [Key]
        public long? Id { get; set; }
        public long? StockTrackerId { get; set; }
        public Platform? TradePlatform { get; set; }
        public DateTime? DateCreated { get; set; }
    }
}