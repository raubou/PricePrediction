using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestment.Models
{
    public sealed class Product
    {
        public string id { get; set; }
        public string display_name { get; set; }
        public string base_currency { get; set; }
        public string quote_currency { get; set; }
        public string base_increment { get; set; }
        public string quote_increment { get; set; }
        public string base_min_size { get; set; }
        public string base_max_size { get; set; }
        public string min_market_funds { get; set; }
        public string max_market_funds { get; set; }
        public string status { get; set; }
        public string status_message { get; set; }
        public bool cancel_only { get; set; }
        public bool limit_only { get; set; }
        public bool post_only { get; set; }
        public bool trading_disabled { get; set; }
        public bool fx_stablecoin { get; set; }

        public Ticker ticker { get; set; }

        public List<HistoricData> history { get; set; }
    }

    //public sealed class ProductTicker
    //{
    //    public int trade_id { get; set; }
    //    public string price { get; set; }
    //    public string size { get; set; }
    //    public string bid { get; set; }
    //    public string ask { get; set; }
    //    public string volume { get; set; }
    //    public DateTime time { get; set; }
    //}
}
