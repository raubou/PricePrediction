using Coinbase.AdvancedTrade.Interfaces;
using Coinbase.AdvancedTrade.Models;
using Coinbase.AdvancedTrade.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coinbase.AdvancedTrade.ExchangeManagers
{
    /// <summary>
    /// Manages account-related activities for the Coinbase Advanced Trade API.
    /// </summary>
    public class AccountsManager : BaseManager, IAccountsManager
    {
        /// <summary>
        /// Initializes a new instance of the AccountsManager class.
        /// </summary>
        /// <param name="authenticator">The Coinbase authenticator.</param>
        public AccountsManager(CoinbaseAuthenticator authenticator) : base(authenticator) { }

        /// <inheritdoc/>
        public async Task<List<Account>> ListAccountsAsync(int limit = 250, string cursor = null)
        {
            try
            {
                var parameters = new { limit, cursor };
                //var response = await _authenticator.SendAuthenticatedRequestAsync("GET", "/api/v3/brokerage/accounts", UtilityHelper.ConvertToDictionary(parameters)) ?? new Dictionary<string, object>();
                var response = await _authenticator.SendAuthenticatedRequestAsync("GET", "/api/v3/brokerage/accounts?limit=" + limit);
                bool hasNext = UtilityHelper.DeserializeJsonElement<bool>(response, "has_next");
                int size = UtilityHelper.DeserializeJsonElement<int>(response, "size");
                cursor = UtilityHelper.DeserializeJsonElement<string>(response, "cursor");
                //return UtilityHelper.DeserializeJsonElement<List<Account>>(response, "accounts");
                List<Account> accounts = UtilityHelper.DeserializeJsonElement<List<Account>>(response, "accounts");
                if (hasNext)
                {
                    var response2 = await _authenticator.SendAuthenticatedRequestAsync("GET", "/api/v3/brokerage/accounts?limit=" + limit + "&cursor=" + cursor);
                    accounts.AddRange(UtilityHelper.DeserializeJsonElement<List<Account>>(response2, "accounts"));
                }
                return accounts;
            }
            catch (Exception ex)
            {
                // Wrap and rethrow exceptions to provide more context.
                throw new InvalidOperationException("Failed to list accounts", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Account> GetAccountAsync(string accountUuid)
        {
            // Check if the provided UUID is valid.
            if (string.IsNullOrEmpty(accountUuid))
            {
                throw new ArgumentException("Account UUID cannot be null or empty", nameof(accountUuid));
            }

            try
            {
                var response = await _authenticator.SendAuthenticatedRequestAsync("GET", $"/api/v3/brokerage/accounts/{accountUuid}") ?? new Dictionary<string, object>();

                return UtilityHelper.DeserializeJsonElement<Account>(response, "account");
            }
            catch (Exception ex)
            {
                // Wrap and rethrow exceptions to provide more context.
                throw new InvalidOperationException($"Failed to get account with UUID {accountUuid}", ex);
            }
        }
    }
}
