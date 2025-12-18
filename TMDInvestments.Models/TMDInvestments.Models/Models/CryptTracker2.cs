using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMDInvestment.Models;
using TMDInvestments.Models;

namespace TMDInvestments.Models
{
    public sealed class CryptoTracker2
    {
        public long? Id { get; set; }
        public Guid? BuyOrderId { get; set; }
        public Guid? SellOrderId { get; set; }
        public string Coin { get; set; }
        public string Name { get; set; }
        public int TradeDirection { get; set; }
        public decimal? PricePurchased { get; set; }
        public DateTime? DatePurchased { get; set; }
        public decimal? PriceSold { get; set; }
        public DateTime? DateSold { get; set; }
        public decimal? Amount { get; set; }
        public decimal? CurrentBalance { get; set; }
        public decimal? LastPrice { get; set; }
        public decimal? PurchasedRsi { get; set; }
        public decimal? PurchasedSma26 { get; set; }
        public decimal? PurchasedSma12 { get; set; }
        public decimal? PurchasedSma9 { get; set; }
        public decimal? PurchasedSma3 { get; set; }
        public decimal? PurchasedSma10 { get; set; }
        public decimal? PurchasedSma16 { get; set; }
        public decimal? PurchasedSma5 { get; set; }
        public decimal? PurchasedSma8 { get; set; }
        public decimal? PurchasedSma13 { get; set; }
        public decimal? PurchasedEma26 { get; set; }
        public decimal? PurchasedEma12 { get; set; }
        public decimal? PurchasedEma9 { get; set; }
        public decimal? PurchasedEma3 { get; set; }
        public decimal? PurchasedEma10 { get; set; }
        public decimal? PurchasedEma16 { get; set; }
        public decimal? PurchasedEma5 { get; set; }
        public decimal? PurchasedEma8 { get; set; }
        public decimal? PurchasedEma13 { get; set; }
        public decimal? PurchasedVwap { get; set; }
        public decimal? PurchasedMacd { get; set; }
        public decimal? PurchasedMacdSignal { get; set; }
        public decimal? PurchasedMacdHistogram { get; set; }
        public decimal? SoldRsi { get; set; }
        public decimal? SoldSma26 { get; set; }
        public decimal? SoldSma12 { get; set; }
        public decimal? SoldSma9 { get; set; }
        public decimal? SoldSma3 { get; set; }
        public decimal? SoldSma10 { get; set; }
        public decimal? SoldSma16 { get; set; }
        public decimal? SoldSma5 { get; set; }
        public decimal? SoldSma8 { get; set; }
        public decimal? SoldSma13 { get; set; }
        public decimal? SoldEma26 { get; set; }
        public decimal? SoldEma12 { get; set; }
        public decimal? SoldEma9 { get; set; }
        public decimal? SoldEma3 { get; set; }
        public decimal? SoldEma10 { get; set; }
        public decimal? SoldEma16 { get; set; }
        public decimal? SoldEma5 { get; set; }
        public decimal? SoldEma8 { get; set; }
        public decimal? SoldEma13 { get; set; }
        public decimal? SoldVwap { get; set; }
        public decimal? SoldMacd { get; set; }
        public decimal? SoldMacdSignal { get; set; }
        public decimal? SoldMacdHistogram { get; set; }
        public string BuyStatus { get; set; }
        public string SellStatus { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int? LastUpdatedBy { get; set; }
    }
}
