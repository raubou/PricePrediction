using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class ExecutionLegs
    {
        public int legId { get; set; }
        public float quantity { get; set; }
        public float mismarkedQuantity { get; set; }
        public float price { get; set; }
        public string time { get; set; }
        public int instrumentId { get; set; }
    }
}
