using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class FinnHub
    {
        public string URL { get; set; }
        public string finnhubKey { get; set; }
        public string Quote { get; set; }
        public string Metrics { get; set; }
        public string IPOCalendar { get; set; }
        public string SymbolLookup { get; set; }
        public string History { get; set; }
        public string Profile { get; set; }
        public string News { get; set; }
        public string Earnings { get; set; }
    }
}
