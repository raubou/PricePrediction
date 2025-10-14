using System.Collections.Generic;
using System.ComponentModel;
//using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TMDInvestments.Models
{

    public sealed class SchwabAccountInfo
    {
        public SecuritiesAccount securitiesAccount { get; set; }
    }
    //[Table("securitiesAccount")]
    public sealed class SecuritiesAccount
    {
        public string type { get; set; }
        public string accountNumber { get; set; }
        public int roundTrips { get; set; }
        public bool isDayTrader { get; set; }
        public bool isClosingOnlyRestricted { get; set; }
        public bool pfcbFlag { get; set; }
        public List<Positions> Positions { get; set; }
        public InitialBalances InitialBalances { get; set; }
    }
    public sealed class Positions
    {
        public decimal shortQuantity { get; set; }
        public decimal averagePrice { get; set; }
        public decimal currentDayProfitLoss { get; set; }
        public decimal currentDayProfitLossPercentage { get; set; }
        public decimal longQuantity { get; set; }
        public decimal settledLongQuantity { get; set; }
        public decimal settledShortQuantity { get; set; }

        [JsonPropertyName("Instrument")]
        public Stock Instruments { get; set; } = new Stock();

        public decimal marketValue { get; set; }
        public decimal maintenanceRequirement { get; set; }
        public decimal averageShortPrice { get; set; }
        public decimal taxLotAverageShortPrice { get; set; }
        public decimal shortOpenProfitLoss { get; set; }
        public decimal longOpenProfitLoss { get; set; }
        public decimal previousSessionShortQuantity { get; set; }
        public decimal currentDayCost { get; set; }
    }
    public sealed class Stock
    {
        public string assetType { get; set; }
        public string cusip { get; set; }
        public string symbol { get; set; }
        public decimal netChange { get; set; }
    }

    public sealed class InitialBalances
    {
        public decimal accruedInterest { get; set; }
        public decimal availableFundsNonMarginableTrade { get; set; }
        public decimal bondValue { get; set; }
        public decimal buyingPower { get; set; }
        public decimal cashBalance { get; set; }
        public decimal cashAvailableForTrading { get; set; }
        public decimal cashReceipts { get; set; }
        public decimal dayTradingBuyingPower { get; set; }
        public decimal dayTradingBuyingPowerCall { get; set; }
        public decimal dayTradingEquityCall { get; set; }
        public decimal equity { get; set; }
        public decimal equityPercentage { get; set; }
        public decimal liquidationValue { get; set; }
        public decimal longMarginValue { get; set; }
        public decimal longOptionMarketValue { get; set; }
        public decimal longStockValue { get; set; }
        public decimal maintenanceCal { get; set; }
        public decimal maintenanceRequirement { get; set; }
        public decimal margin { get; set; }
        public decimal marginEquity { get; set; }
        public decimal moneyMarketFund { get; set; }
        public decimal mutualFundValue { get; set; }
        public decimal regTCall { get; set; }
        public decimal shortMarginValue { get; set; }
        public decimal shortOptionMarketValue { get; set; }
        public decimal shortStockValue { get; set; }
        public decimal totalCash { get; set; }
        public bool isInCall { get; set; }
        public decimal pendingDeposits { get; set; }
        public decimal marginBalance { get; set; }
        public decimal shortBalance { get; set; }
        public decimal accountValue { get; set; }
    }
}
