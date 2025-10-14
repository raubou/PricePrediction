using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models.Models.Polygon
{
    public sealed class Result
    {
        public Result() {
            results = new Tickers();
        }
        public Tickers results { get; set; }
    }
}
