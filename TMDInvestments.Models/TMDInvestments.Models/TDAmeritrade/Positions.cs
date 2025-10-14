using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestment.Models
{
    public sealed class AccountPositions
    {
        public securitiesAccount securitiesAccount { get; set; }
    }

    public sealed class securitiesAccount
    {
        public string type { get; set; }
        public string accountId { get; set; }
        public int roundTrips { get; set; }
        public bool isDayTrader { get; set; }
        public bool isClosingOnlyRestricted { get; set; }
        public List<positions> positions { get; set; }
        public InitialBalances initialBalances { get; set; }
        public CurrentBalances currentBalances { get; set; }
        public ProjectedBalances projectedBalances { get; set; }
    }

    public sealed class positions
    {
        public double shortQuantity { get; set; }
        public double averagePrice { get; set; }
        public double currentDayProfitLoss { get; set; }
        public double currentDayProfitLossPercentage { get; set; }
        public double longQuantity { get; set; }
        public double settledLongQuantity { get; set; }
        public double settledShortQuantity { get; set; }
        public double marketValue { get; set; }
        public Instrument Instrument { get; set; }
    }
}
