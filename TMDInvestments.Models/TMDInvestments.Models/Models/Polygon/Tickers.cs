using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models.Models.Polygon
{
    public sealed class Tickers
    {
        public string ticker { get; set; }
        public string name { get; set; }
        public string market { get; set; }
        public string locale { get; set; }
        public string type { get; set; }
        public bool active { get; set; }
        public string currency_name { get; set; }
        public DateTime last_updated_utc { get; set; }
        public string cik { get; set; }
        public string composite_figi { get; set; }
        public string primary_exchange { get; set; }
        public string share_class_figi { get; set; }
    }
}
