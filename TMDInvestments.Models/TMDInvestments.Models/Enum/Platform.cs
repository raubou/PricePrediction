using System.ComponentModel;

namespace TMDInvestments.Models
{
    public enum Platform
    {
        [Description("CharlesSchwab")]
        CharlesSchwab = 1,
        [Description("ETrade")]
        ETrade = 2
    }
}