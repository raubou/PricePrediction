using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestment.Models
{
    public sealed class Order
    {
        public string id { get; set; }
        public string product_id { get; set; }
        public string profile_id { get; set; }
        public string side { get; set; }
        public string funds { get; set; }
        public string specified_funds { get; set; }
        public string type { get; set; }
        public bool post_only { get; set; }
        public DateTime created_at { get; set; }
        public string done_at { get; set; }
        public string done_reason { get; set; }
        public string fill_fees { get; set; }
        public string filled_size { get; set; }
        public string executed_value { get; set; }
        public string status { get; set; }
        public bool settled { get; set; }
        public List<Fills> fills { get; set; }
}
    public sealed class Fills
    {
        public int trade_id { get; set; }
        public string product_id { get; set; }
        public string price { get; set; }
        public string size { get; set; }
        public string order_id { get; set; }
        public DateTime created_at { get; set; }
        public string liquidity { get; set; }
        public string fee { get; set; }
        public bool settled { get; set; }
        public string side { get; set; }
    }
}
