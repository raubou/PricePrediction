using Coinbase.AdvancedTrade.Interfaces;
using Coinbase.AdvancedTrade.Models;
using Coinbase.AdvancedTrade.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Coinbase.AdvancedTrade.ExchangeManagers
{
    /// <summary>
    /// 
    /// </summary>
    public class PortfolioManager : BaseManager, IPortfolioManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="authenticator"></param>
        public PortfolioManager(CoinbaseAuthenticator authenticator) : base(authenticator) { }

        /// <inheritdoc/>
        public async Task<List<Portfolio>> ListPortfolioAsync(int limit = 49, string cursor = null)
        {
            try
            {
                var parameters = new { limit, cursor };
                var response = await _authenticator.SendAuthenticatedRequestAsync("GET", "/api/v3/brokerage/portfolios", UtilityHelper.ConvertToDictionary(parameters)) ?? new Dictionary<string, object>();

                return UtilityHelper.DeserializeJsonElement<List<Portfolio>>(response, "portfolios");
            }
            catch (Exception ex)
            {
                // Wrap and rethrow exceptions to provide more context.
                throw new InvalidOperationException("Failed to list Portfolio", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<PortfolioBreakdown> GetPortfolioAsync(string portfoliouuid)
        {
            // Check if the provided UUID is valid.
            if (string.IsNullOrEmpty(portfoliouuid))
            {
                throw new ArgumentException("Portfolio UUID cannot be null or empty", nameof(portfoliouuid));
            }

            try
            {
                var response = await _authenticator.SendAuthenticatedRequestAsync("GET", $"/api/v3/brokerage/portfolios/{portfoliouuid}") ?? new Dictionary<string, object>();

                return UtilityHelper.DeserializeJsonElement<PortfolioBreakdown>(response, "breakdown");
            }
            catch (Exception ex)
            {
                // Wrap and rethrow exceptions to provide more context.
                throw new InvalidOperationException($"Failed to get Portfolio with UUID {portfoliouuid}", ex);
            }
        }
    }
}
