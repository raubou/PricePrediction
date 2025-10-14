
namespace TMDInvestment.Models
{
    public sealed class AccountBalances 
    {
        public SecuritiesAccount securitiesAccount { get; set; }
    }

    public sealed class SecuritiesAccount
    {
        public string type { get; set; }
        public string accountId { get; set; }
        public int roundTrips { get; set; }
        public bool isDayTrader { get; set; }
        public bool isClosingOnlyRestricted { get; set; }
        public InitialBalances initialBalances { get; set; }
        public CurrentBalances currentBalances { get; set; }
        public ProjectedBalances projectedBalances { get; set; }
    }

    public sealed class InitialBalances
    {
        public double accruedInterest { get; set; }
        public double availableFundsNonMarginableTrade { get; set; }
        public double bondValue { get; set; }
        public double buyingPower { get; set; }
        public double cashBalance { get; set; }
        public double cashAvailableForTrading { get; set; }
        public double cashReceipts { get; set; }
        public double dayTradingBuyingPower { get; set; }
        public double dayTradingBuyingPowerCall { get; set; }
        public double dayTradingEquityCall { get; set; }
        public double equity { get; set; }
        public double equityPercentage { get; set; }
        public double liquidationValue { get; set; }
        public double longMarginValue { get; set; }
        public double longOptionMarketValue { get; set; }
        public double longStockValue { get; set; }
        public double maintenanceCall { get; set; }
        public double maintenanceRequirement { get; set; }
        public double margin { get; set; }
        public double marginEquity { get; set; }
        public double moneyMarketFund { get; set; }
        public double mutualFundValue { get; set; }
        public double regTCall { get; set; }
        public double shortMarginValue { get; set; }
        public double shortOptionMarketValue { get; set; }
        public double shortStockValue { get; set; }
        public double totalCash { get; set; }
        public bool isInCall { get; set; }
        public double pendingDeposits { get; set; }
        public double marginBalance { get; set; }
        public double shortBalance { get; set; }
        public double accountValue { get; set; }
    }

    public sealed class CurrentBalances
    {
        public double accruedInterest { get; set; }
        public double cashBalance { get; set; }
        public double cashReceipts { get; set; }
        public double longOptionMarketValue { get; set; }
        public double liquidationValue { get; set; }
        public double longMarketValue { get; set; }
        public double moneyMarketFund { get; set; }
        public double savings { get; set; }
        public double shortMarketValue { get; set; }
        public double pendingDeposits { get; set; }
        public double availableFunds { get; set; }
        public double availableFundsNonMarginableTrade { get; set; }
        public double buyingPower { get; set; }
        public double buyingPowerNonMarginableTrade { get; set; }
        public double dayTradingBuyingPower { get; set; }
        public double equity { get; set; }
        public double equityPercentage { get; set; }
        public double longMarginValue { get; set; }
        public double maintenanceCall { get; set; }
        public double maintenanceRequirement { get; set; }
        public double marginBalance { get; set; }
        public double regTCall { get; set; }
        public double shortBalance { get; set; }
        public double shortMarginValue { get; set; }
        public double shortOptionMarketValue { get; set; }
        public double sma { get; set; }
        public double mutualFundValue { get; set; }
        public double bondValue { get; set; }
    }

    public sealed class ProjectedBalances
    {
        public double availableFunds { get; set; }
        public double availableFundsNonMarginableTrade { get; set; }
        public double buyingPower { get; set; }
        public double dayTradingBuyingPower { get; set; }
        public double dayTradingBuyingPowerCall { get; set; }
        public double maintenanceCall { get; set; }
        public double regTCall { get; set; }
        public bool isInCall { get; set; }
        public double stockBuyingPower { get; set; }
    }
}
