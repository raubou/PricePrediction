using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class CryptoTracker
    {
        [Key]
        public long? Id { get; set; }
        public Guid? BuyOrderId { get; set; }
        public Guid? SellOrderId { get; set; }
        //public Guid OrderId { get; set; }
        public string Coin { get; set; }
        public string Name { get; set; }
        public int? TradeDirection { get; set; }
        public decimal? PricePurchased { get; set; }
        public DateTime? DatePurchased { get; set; }
        public decimal? PriceSold { get; set; }
        public DateTime? DateSold { get; set; }
        public decimal? Amount { get; set; }
        public decimal? CurrentBalance { get; set; }
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
        public string BuyStatus { get; set; }
        public string SellStatus { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
    }
}
