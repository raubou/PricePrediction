using System.ComponentModel;
namespace TMDInvestments.Models
{
    public enum FrequencyType
    {
        [Description("minute")]
        minute,
        [Description("daily")]
        daily,
        [Description("weekly")]
        weekly,
        [Description("monthly")]
        monthly
    }
}
