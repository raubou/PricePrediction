//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using TMDInvestments.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TMDInvestment.Models
{
    public sealed class Stock
    {
        public List<Movers> moversUp { get; set; }
        public List<Movers> moversDown { get; set; }
    }
    public sealed class Movers
    {
        public readonly Direction _direction;
        public Movers()
        {

        }
        public Movers(Direction direction)
        {
            _direction = direction;
        }
        public List<Items> items { get; set; } = new List<Items>();
    }
    public sealed class Items
    {
        public string symbol { get; set; }
        public float change { get; set; }
        public string description { get; set; }
        public float last { get; set; }
        public int totalVolume { get; set; }
        public History history { get; set; }
    }
    public sealed class History
    {
        public Candles candles = new Candles();
    }
    //[Description(results)]
    public sealed class Candles
    {
        //[JsonProperty("candles")]
        public List<CandleItems> candles { get; set; }
    }
    //[Description(candles)]
    public sealed class CandleItems
    {
        public float open { get; set; }
        public float high { get; set; }
        public float low { get; set; }
        public float close { get; set; }
        public long volume { get; set; }
        public long datetime { get { return new DateTimeOffset(time).ToUnixTimeMilliseconds(); } set { time = DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime; } }
        public DateTime time { get; set; }// { get { return new DateTimeOffset(time).DateTime; } set; }
        public float opChange { get { return close - open; } set; }
        public float opPercent { get { return (close - open) / ((close + open) / 2) * 100; } set; }
    }

    //public class Quote
    //{
    //    public string symbol { get; set; }
    //    public string description { get; set; }
    //    public float bidPrice { get; set; }
    //    public float bidSize { get; set; }
    //    public float askPrice { get; set; }
    //    public float askSize { get; set; }
    //    public float lastPrice { get; set; }
    //    public float lastSize { get; set; }
    //    public float openPrice { get; set; }
    //    public float highPrice { get; set; }
    //    public float lowPrice { get; set; }
    //    public float closePrice { get; set; }
    //    public float netChange { get; set; }
    //    public float totalVolume { get; set; }
    //    public float quoteTimeInLong { get; set; }
    //    public float tradeTimeInLong { get; set; }
    //    public float mark { get; set; }
    //    public string exchangeName { get; set; }
    //    public bool marginable { get; set; }
    //    public bool shortable { get; set; }
    //    public float volatility { get; set; }
    //    public float digits { get; set; }
    //    [JsonProperty("52WkHigh")]
    //    public float _52WkHigh { get; set; }
    //    [JsonProperty("52WkLow")]
    //    public float _52WkLow { get; set; }
    //    //public float nAV { get; set; }
    //    public float peRatio { get; set; }
    //    public float divAmount { get; set; }
    //    public float divYield { get; set; }
    //    //[NotMapped]
    //    public string divDate { get; set; }
    //    public string securityStatus { get; set; }
    //    public float regularMarketLastPrice { get; set; }
    //    public float regularMarketLastSize { get; set; }
    //    public float regularMarketNetChange { get; set; }
    //    public float regularMarketTradeTimeInLong { get; set; }
    //    public float netPercentChangeInDouble { get; set; }
    //    public float markChangeInDouble { get; set; }
    //    public float markPercentChangeInDouble { get; set; }
    //    public float regularMarketPercentChangeInDouble { get; set; }
    //    public bool delayed { get; set; }
    //    public bool realtimeEntitled { get; set; }
    //}
}
