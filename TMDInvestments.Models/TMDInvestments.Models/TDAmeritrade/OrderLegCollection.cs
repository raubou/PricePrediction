using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class OrderLegCollection
    {
        public string orderLegType { get; set; }
        public int legId { get; set; }
        public Instrument instrument { get; set; }
        public string instruction { get; set; }
        public string positionEffect { get; set; }
        public float quantity { get; set; }
    }
}
