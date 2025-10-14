using System;
using System.Collections.Generic;
using System.Text;

namespace TMDInvestments.Models
{
    public sealed class Quote
    {
        public string symbol { get; set; }
        public string description { get; set; }
        public float bidPrice { get; set; }
        public float bidSize { get; set; }
        public float askPrice { get; set; }
        public float askSize { get; set; }
        public float lastPrice { get; set; }
        public float lastSize { get; set; }
        public float openPrice { get; set; }
        public float highPrice { get; set; }
        public float lowPrice { get; set; }
        public float closePrice { get; set; }
        public float netChange { get; set; }
        public float totalVolume { get; set; }
        public float quoteTimeInLong { get; set; }
        public float tradeTimeInLong { get; set; }
        public float mark { get; set; }
        public string exchangeName { get; set; }
        public bool marginable { get; set; }
        public bool shortable { get; set; }
        public float volatility { get; set; }
        public float digits { get; set; }
        //[JsonProperty("52WkHigh")]
        public float _52WkHigh { get; set; }
        //[JsonProperty("52WkLow")]
        public float _52WkLow { get; set; }
        //public float nAV { get; set; }
        public float peRatio { get; set; }
        public float divAmount { get; set; }
        public float divYield { get; set; }
        //[NotMapped]
        public string divDate { get; set; }
        public string securityStatus { get; set; }
        public float regularMarketLastPrice { get; set; }
        public float regularMarketLastSize { get; set; }
        public float regularMarketNetChange { get; set; }
        public float regularMarketTradeTimeInLong { get; set; }
        public float netPercentChangeInDouble { get; set; }
        public float markChangeInDouble { get; set; }
        public float markPercentChangeInDouble { get; set; }
        public float regularMarketPercentChangeInDouble { get; set; }
        public bool delayed { get; set; }
        public bool realtimeEntitled { get; set; }
    }
}
