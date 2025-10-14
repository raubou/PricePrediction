using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class ETradeSettings
    {
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string productionKey { get; set; }
        public string productionSecret { get; set; }
        public string baseURL { get; set; }
        //public string sandBoxURL { get; set; }
        public string authURL { get; set; }
    }
}
