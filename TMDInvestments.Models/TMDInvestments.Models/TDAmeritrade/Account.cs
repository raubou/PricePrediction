using System.Collections.Generic;

namespace TMDInvestment.Models
{
    public sealed class Account
    {
        //public AccountBalances accountBalances { get; set; }
        //public List<Positions> positions { get; set; }
        public AccountPositions accountPositions { get; set; }
    }
}
