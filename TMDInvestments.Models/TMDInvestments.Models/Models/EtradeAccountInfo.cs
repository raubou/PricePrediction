using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TMDInvestments.Models
{
    [XmlRoot("BalanceResponse", Namespace = "")]
    public sealed class EtradeAccountInfo2
    {
        public string accountId { get; set; }        

        [XmlElement("Computed", Type = typeof(Computed))]
        public Computed Computed { get; set; } = new Computed();

    }
    [XmlRoot("PortfolioResponse", Namespace = "")]
    public sealed class EtradeAccountInfo
    {
        public AccountPortfolio AccountPortfolio { get; set; }
    }
    public sealed class AccountPortfolio
    {
        public string accountId { get; set; }

        [XmlElement("Computed", Type = typeof(Computed))]
        public Computed Computed { get; set; } = new Computed();

        [XmlElement("Position", Type = typeof(Position))]
        public List<Position> Positions { get; set; }
    }
    public sealed class Position
    {
       public string positionId { get; set; }
        public Product Product { get; set; }
        public string symbolDescription { get; set; }
        public string dateAcquired { get; set; }
        public string pricePaid { get; set; }
        public string commissions { get; set; }
        public string otherFees { get; set; }
        public string quantity { get; set; }
        public string positionIndicator { get; set; }
        public string positionType { get; set; }
        public string daysGain { get; set; }
        public string daysGainPct { get; set; }
        public string marketValue { get; set; }
        public string totalCost { get; set; }
        public string totalGain { get; set; }
        public string totalGainPct { get; set; }
        public string pctOfPortfolio { get; set; }
        public string costPerShare { get; set; }
        public string todayCommissions { get; set; }
        public string todayFees { get; set; }
        public string todayPricePaid { get; set; }
        public string todayQuantity { get; set; }
        public string adjPrevClose { get; set; }
    }
    public sealed class Product
    {
        public string expiryDay { get; set; }
        public string expiryMonth { get; set; }
        public string expiryYear { get; set; }
        public string securityType { get; set; }
        public string strikePrice { get; set; }
        public string symbol { get; set; }
    }
    public sealed class Computed
    {
        public string regtEquity { get; set; }
        public string cashAvailableForWithdrawal { get; set; }
        public string cashAvailableForInvestment { get; set; }
        public string dtMarginBuyingPower { get; set; } 

        [XmlElement("RealTimeValues", Type = typeof(RealTimeValues))]
        public RealTimeValues RealTimeValues { get; set; } = new RealTimeValues();
    }
    public sealed class RealTimeValues 
    {
        public string netMvLong { get; set; }
        public string netMvShort { get; set; }
    }

}
