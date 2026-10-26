
using System.ComponentModel;
using System.Reflection;

namespace TMDInvestments.Models.Enum
{
    public enum CryptoList
    {
        [Description("Cardano")]
        ADA,
        [Description("Bitcoin")]
        BTC,
        [Description("Bitcoin Cash")]
        BCH,
        [Description("Binance Coin")]
        BNB,
        [Description("Ethereum")]
        ETH,
        [Description("Hyper liquid")]
        HYPE,
        [Description("Litecoin")]
        LTC,
        [Description("Polkadot")]
        DOT,
        [Description("Chainlink")]
        LINK,
        [Description("Solana")]
        SOL,
        [Description("Tron")]
        TRX,
        [Description("Stellar")]
        XLM,
        [Description("Ripple")]
        XRP,
        [Description("Dogecoin")]
        DOGE
    }
    public static class EnumExtensions
    {
        public static string GetDescription(this System.Enum value)
        {
            // Get the field information for the enum value
            FieldInfo field = value.GetType().GetField(value.ToString());

            if (field == null)
                return value.ToString();

            // Check for the DescriptionAttribute
            DescriptionAttribute attribute = field.GetCustomAttribute<DescriptionAttribute>();

            // Return description if found; otherwise, fall back to the name
            return attribute != null ? attribute.Description : value.ToString();
        }
    }
}
