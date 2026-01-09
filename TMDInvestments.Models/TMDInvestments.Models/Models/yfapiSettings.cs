using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class yfapiSettings
    {
     public string URL { get; set; }
    public string MarketSummary { get; set; }
        //yfapi.Trending = "/v1/finance/trending/US";
        public string GainersLosers { get; set; }
        public string yfapiKey { get; set; }
    }
}
