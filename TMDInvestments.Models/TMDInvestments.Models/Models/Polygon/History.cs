using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models.Models.Polygon
{
    public sealed class History
    {
        public decimal c { get; set; }
        public decimal h { get; set; }
        public decimal l { get; set; }
        public int n { get; set; }
        public decimal o { get; set; }
        public long t { get; set; }
        public dynamic v { get; set; }
        public decimal vw { get; set; }
    }
}
