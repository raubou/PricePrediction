using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class WatchListItems
    {
        public int sequenceId { get; set; }
        public float quantity { get; set; }
        public float averagePrice { get; set; }
        public float commission { get; set; }
        public Instrument instrument { get; set; }
    }
}
