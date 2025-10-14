using System;
using System.Collections.Generic;
using System.Text;

namespace Coinbase.AdvancedTrade.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class PortfolioBreakdown
    {
        public Portfolio Portfolio { get; set; }
        public portfolio_balances Portfolio_balances { get; set; }

    }
    /// <summary>
    /// 
    /// </summary>
    public class portfolio_balances
    {
        public total_balance total_Balance { get; set; }
        public total_cash_equivalent_balance total_cash_equivalent_balance { get; set; }
        public total_crypto_balance total_crypto_balance { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class total_balance
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class total_cash_equivalent_balance {
        public string value { get; set; }
        public string currency { get; set; }
    }
    public class total_crypto_balance {
        public string value { get; set; }
        public string currency { get; set; }
    }
}
