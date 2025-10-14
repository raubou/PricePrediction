using System.ComponentModel;

namespace TMDInvestments.Models
{
    public enum TradeDirection
    {
        [Description("Buy")]
        BUY = 1,
        [Description("Sell")]
        SELL = 2,
        [Description("Sell Short")]
        SELL_SHORT = 3,//SELL_SHORT
        [Description("Buy to Cover")]
        BUY_TO_COVER = 4,
    }
}