using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestment.Models
{
    public sealed class HistoricData
    {
        public string symbol { get; set; }
        public long epoch { get; set; }
        public DateTime time { get; set; }
        public decimal low { get; set; }
        public decimal high { get; set; }
        public decimal open { get; set; }
        public decimal close { get; set; }
        public decimal volume { get; set;}
        public decimal change { get; set; }
        public decimal percent { get; set; }
    }
}
