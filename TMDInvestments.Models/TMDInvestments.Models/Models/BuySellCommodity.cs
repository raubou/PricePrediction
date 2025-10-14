using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TMDInvestments.Models.Models
{
    public sealed class BuySellCommodity
    {
        public int? Points { get; set; }
        public TradeDirection Direction { get; set; }
        public JsonObject Results { get; set; }
        public BuySellCommodity(int? points, TradeDirection direction, JsonObject results) {
            Points = points;
            Direction = direction;
            Results = results;
        }
    }
}
