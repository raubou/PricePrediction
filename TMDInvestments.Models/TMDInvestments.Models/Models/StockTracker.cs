using System;
using System.ComponentModel.DataAnnotations;

namespace TMDInvestments.Models
{
    public sealed class StockTracker
    {
        public StockTracker() { }
        [Key]
        public long? Id { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public int? TradeDirection { get; set; }
        public int? TradePlatform { get; set; }
        public bool? IsReversed { get; set; }   
        public decimal? PricePurchased { get; set; }
        public DateTime? DatePurchased { get; set; }
        public decimal? PriceSold { get; set; }
        public DateTime? DateSold { get; set; }
        public int Quantity { get; set; }
        public decimal? LastPrice { get; set; }
        public decimal? PurchasedRsi { get; set; }
        public decimal? PurchasedSma200 { get; set; }
        public decimal? PurchasedEma200 { get; set; }
        public decimal? PurchasedVwap { get; set; }
        public decimal? PurchasedEma9 { get; set; }
        public decimal? PurchasedEma12 { get; set; }
        public decimal? PurchasedEma26 { get; set; }
        public decimal? PurchasedMacd { get; set; }
        public decimal? SoldRsi { get; set; }
        public decimal? SoldSma200 { get; set; }
        public decimal? SoldEma200 { get; set; }
        public decimal? SoldVwap { get; set; }
        public decimal? SoldEma9 { get; set; }
        public decimal? SoldEma12 { get; set; }
        public decimal? SoldEma26 { get; set; }
        public decimal? SoldMacd { get; set; }
        public string Status { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
    }
}
