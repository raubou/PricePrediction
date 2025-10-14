//using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMDInvestments.Models
{
    public sealed class RulesEngine
    {
        [Key]
        public long? Id { get; set; }
        public decimal? MaxPercentBalancetoUse { get; set; }
        public int? MinNumberofStockstoBuy { get; set; }
        public int? MaxNumberofStockstoBuy { get; set; }
        public int? MinNumberofCryptotoBuy { get; set; }
        public int? MaxNumberofCryptotoBuy { get; set; }
        public int? MaxNumberofSharesPerStock { get; set; }
        public decimal? MaxDollarAmountPerCrypto { get; set; }
        public decimal? MinStockPrice { get; set; }
        public bool? ShortEnabled { get; set; }
        public bool? SchwabAnalysisTradeEnabled { get; set; }
        public bool? EtradeAnalysisTradeEnabled { get; set; }
        public bool? SchwabDayTradeEnabled { get; set; }
        public bool? EtradeDayTradeEnabled { get; set; }
        //[Required(AllowEmptyStrings = true)]
        public decimal? StopLossPercentPurchasedCrypto { get; set; }
        public decimal? StopLossPercentPurchasedStock { get; set; }
        public decimal? StopLossPercentLastPriceCrypto { get; set; }
        public decimal? StopLossPercentLastPriceStock { get; set; }
        public int? StockLongCount { get; set; }
        public int? StockShortCount { get; set; }
        public int? CryptoLongCount { get; set; }
        public int? CryptoShortCount { get; set; }
        public decimal? MaxPercentofAvailBalancePerOrder { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int? LastUpdatedBy { get; set; }
    }
}
