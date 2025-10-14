using Coinbase.AdvancedTrade.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Coinbase.AdvancedTrade.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPortfolioManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountUuid"></param>
        /// <returns></returns>
        Task<PortfolioBreakdown> GetPortfolioAsync(string accountUuid);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="cursor"></param>
        /// <returns></returns>
        Task<List<Portfolio>> ListPortfolioAsync(int limit = 49, string cursor = null);
    }
}
