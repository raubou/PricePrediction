using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class MACDClosesDateTime
    {
        public decimal Price { get; set; }
        public DateTime Datetime { get; set; }
    }
}
