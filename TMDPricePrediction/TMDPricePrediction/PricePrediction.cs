using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TMD.PricePrediction;
using TMD.PricePrediction.Classes;
using TMDInvestments.Models;
using TMDInvestments.Models.Models;

namespace TMDPricePrediction
{
    public class PricePrediction
    {
        private ILogger? _logger;
        IOptions<SchwabSettings> _SchwabSettings;
        IOptions<ETradeSettings> _ETradeSettings;
        IOptions<CoinBaseSettings> _CoinBaseSettings;
        ExecStockPricePredictionSchwab? exec1 = null;
        ExecStockPricePredictionETrade? exec3 = null;
        ExecCryptoPricePrediction? exec2 = null;
        List<BuySellCommodity>? BuySellStockList = null;
        readonly IHttpClientFactory? httpClientFactory = null;
        Helper? helper = null;

        public PricePrediction(ILoggerFactory loggerFactory, IOptions<SchwabSettings> SchwabSettings, IOptions<ETradeSettings> ETradeSettings, IOptions<CoinBaseSettings> CoinBaseSettings)
        {
            _logger = loggerFactory.CreateLogger("Price Prediction");
            _CoinBaseSettings = CoinBaseSettings;
            _SchwabSettings = SchwabSettings;
            _ETradeSettings = ETradeSettings;
            BuySellStockList = new List<BuySellCommodity>();
        }

        [Function("PricePrediction")]
        //0 */5 * * * *
        public void Run([TimerTrigger("0 */15 * * * *", RunOnStartup = true)] TimerInfo myTimer)
        {

            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            Versioning versioning = new Versioning();
            _logger.LogInformation("Technical Analysis! Version " + versioning.GetAssemblyVersion());

            //var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            //var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

            var client = new ServiceCollection().AddHttpClient("TMD")
        .UseSocketsHttpHandler((handler, _) =>
            handler.PooledConnectionLifetime = TimeSpan.FromMinutes(7)) // Recreate connection every 2 minutes
        .SetHandlerLifetime(Timeout.InfiniteTimeSpan); // Disable rotation, as it is handled by PooledConnectionLifetime
            var serviceProvider = client.Services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            helper = new Helper(_logger, _SchwabSettings.Value, _ETradeSettings.Value, httpClientFactory);

            exec1 = new ExecStockPricePredictionSchwab(_logger, _SchwabSettings.Value, httpClientFactory);
            exec3 = new ExecStockPricePredictionETrade(_logger, _ETradeSettings.Value, _SchwabSettings.Value, httpClientFactory);
            exec2 = new ExecCryptoPricePrediction(_logger, _CoinBaseSettings.Value, httpClientFactory);

            decimal totalAvailiableforLongTradeSchwab = 0m;
            decimal totalAvailiableforLongTradeETrade = 0m;

            _logger.LogInformation($"Run Time {DateTime.Now}");
            try
            {
                if (helper.IsNotHolidayorWeekend())
                {
                    helper.UnloadAgingStocks();
                    helper.CheckSchwabOrderStatus(ref totalAvailiableforLongTradeSchwab);
                    helper.CheckETradeOrderStatus(ref totalAvailiableforLongTradeETrade);


                    if (totalAvailiableforLongTradeSchwab > 100 || totalAvailiableforLongTradeETrade > 100)
                        BuySellStockList = helper.GetQuoteList();

                    if (totalAvailiableforLongTradeSchwab > 100)
                        exec1.Exec(totalAvailiableforLongTradeSchwab, ref BuySellStockList);
                    else
                        _logger.LogInformation($"Not enough buying power in Schwab account to make a trade. ${totalAvailiableforLongTradeSchwab}");
                }
                else
                {
                    helper.Log(_logger, LogLevel.Warning, "Time is outside of trading window.");
                    goto ExecCrypto;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
            try
            {
                if (totalAvailiableforLongTradeETrade > 100)
                    exec3.Exec(totalAvailiableforLongTradeETrade, ref BuySellStockList);
                else
                    _logger.LogInformation($"Not enough buying power in ETrade account to make a trade. ${totalAvailiableforLongTradeETrade}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
        ExecCrypto:
            try
            {
                exec2.Exec();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
            exec1 = null;
            exec2 = null;
            exec3 = null;
            _logger.LogInformation($"Stop Time {DateTime.Now}");


            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
        //public void ExecEtrade(List<BuySellCommodity>? BuySellStockList)
        //{
        //    int authRetry = 0;
        //accountAuthRetry:
        //    //Get Account Info
        //    XElement account = null;
        //    try
        //    {
        //        account = services.Account();
        //    }
        //    catch (Exception ex)
        //    {
        //        if (authRetry < 3)
        //        {
        //            helper.Log(_logger, LogLevel.Error, "Authenication from ETrade failed. Retrying");
        //            authRetry++;
        //            goto accountAuthRetry;
        //        }
        //        else
        //        {
        //            helper.Log(_logger, LogLevel.Error, $"Authenication from schwab failed {authRetry} Retries. Exiting");
        //            return;
        //        }
        //    }

        //    helper.Log(_logger, LogLevel.Information, $"Get account info for ETrade");

        //    XElement securitiesAccount = null;
        //    try
        //    {
        //        securitiesAccount = account.Element("Computed");
        //    }
        //    catch (Exception ex)
        //    {
        //        if (authRetry < 3)
        //        {
        //            helper.Log(_logger, LogLevel.Error, "Authenication from ETrade failed. Retrying");
        //            authRetry++;
        //            goto accountAuthRetry;
        //        }
        //        else
        //        {
        //            helper.Log(_logger, LogLevel.Error, $"Authenication from schwab failed {authRetry} Retries. Exiting");
        //            return;
        //        }
        //    }

        //    decimal cash = Convert.ToDecimal(securitiesAccount.Element("cashBuyingPower").Value.ToString());
        //    decimal margin = Convert.ToDecimal(securitiesAccount.Element("marginBuyingPower").Value.ToString());
        //    //totalAvailiableforLongTrade = Math.Round((cash + margin), 2);
        //    totalAvailiableforLongTrade = Math.Round((cash + margin) * .9m, 2);
        //    //totalAvailiableforLongTrade = 200m;

        //    helper.Log(_logger, LogLevel.Information, $"Cash Balance ${cash}. Margin Balance ${margin}. Total Availiable ${totalAvailiableforLongTrade}");

        //    if (helper.IsNotHolidayorWeekend())
        //        Run(BuySellStockList);
        //    else
        //        helper.Log(_logger, LogLevel.Warning, "Time is outside of trading window.");
        //}
        //public void ExecSchwab(List<BuySellCommodity>? BuySellStockList)
        //{
        //    JsonElement account = new JsonElement();
        //    services.GetAccountInfo(ref account);
        //    //_logger.LogInformation($"Get account info for Charles Schwab");
        //    helper.Log(_logger, LogLevel.Information, $"Get account info for Charles Schwab");

        //    JsonElement securitiesAccount = new JsonElement();
        //    JsonElement initialBalances = new JsonElement();
        //    try
        //    {
        //        securitiesAccount = account.GetProperty("securitiesAccount");
        //        initialBalances = securitiesAccount.GetProperty("initialBalances");
        //    }
        //    catch (Exception ex)
        //    {
        //        helper.Log(_logger, LogLevel.Error, $"Authenication from schwab failed {3} Retries. Exiting");
        //        return;

        //    }

        //    decimal cash = Convert.ToDecimal(initialBalances.GetProperty("cashBalance").ToString());
        //    decimal margin = Convert.ToDecimal(initialBalances.GetProperty("margin").ToString());
        //    totalAvailiableforLongTrade = Math.Round((cash + margin) * .9m, 2);
        //    //totalAvailiableforLongTrade = 0m;

        //    helper.Log(_logger, LogLevel.Information, $"Cash Balance ${cash}. Margin Balance ${margin}. Total Availiable ${totalAvailiableforLongTrade}");

        //    if (helper.IsNotHolidayorWeekend())
        //        Run(BuySellStockList);
        //    else
        //        helper.Log(_logger, LogLevel.Warning, "Time is outside of trading window.");
        //}
        public class Versioning
        {
            public string GetAssemblyVersion()
            {
                return GetType().Assembly.GetName().Version.ToString();
            }
        }
    }
}
