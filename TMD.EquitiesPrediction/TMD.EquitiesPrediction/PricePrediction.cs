// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TMD.PricePrediction;
using TMD.PricePrediction.Classes;
using TMD.Services.Service;
using TMDInvestments.Models;
using TMDInvestments.Models.Models;

namespace TMD.EquitiesPrediction;
class PricePrediction //: BackgroundService
{
    static void Main(string[] args)
    //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //Console.WriteLine("Loading Account Info, StockkList, IPO's, and Earnings");
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration config = builder.Build();

        var ConnectionStrings = config.GetSection("ConnectionStrings").Get<ConnectionSettings>();
        var MailSettings = config.GetSection("MailSettings").Get<MailSettings>();
        var FinnHub = config.GetSection("finnhub").Get<FinnHub>();
        var Polygon = config.GetSection("polygon").Get<Polygon>();
        var SchwabSettings = config.GetSection("SchwabAPI").Get<SchwabSettings>();
        var CoinBaseSettings = config.GetSection("CoinBase").Get<CoinBaseSettings>();
        var ETadeSettings = config.GetSection("ETradeAPI").Get<ETradeSettings>();
        var TaAPISettings = config.GetSection("TaApi").Get<TaAPISettings>();
        var yfapiSettings = config.GetSection("yfapi").Get<yfapiSettings>();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            //builder.AddDebugOutput();
            builder.SetMinimumLevel(LogLevel.Trace);
        }
        );
  
        var logger = loggerFactory.CreateLogger("Price Prediction");
        Versioning versioning = new Versioning();

        //var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
        //var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

        var client = new ServiceCollection().AddHttpClient("TMD")
    .UseSocketsHttpHandler((handler, _) =>
        handler.PooledConnectionLifetime = TimeSpan.FromMinutes(7)) // Recreate connection every 2 minutes
    .SetHandlerLifetime(Timeout.InfiniteTimeSpan); // Disable rotation, as it is handled by PooledConnectionLifetime
        var serviceProvider = client.Services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

        ExecStockPricePredictionSchwab? exec1 = null;
        ExecStockPricePredictionETrade? exec3 = null;
        ExecCryptoPricePrediction? exec2 = null;
        Helper? helper = null;
        List<BuySellCommodity>? BuySellStockList = new List<BuySellCommodity>();
        decimal totalAvailiableforLongTradeSchwab = 0m;
        decimal totalAvailiableforLongTradeETrade = 0m;
        do
        {
            logger.LogInformation("Technical Analysis! Version " + versioning.GetAssemblyVersion());
            logger.LogInformation($"Run Time {DateTime.Now}");
            exec1 = new ExecStockPricePredictionSchwab(logger, SchwabSettings, TaAPISettings, httpClientFactory);
            exec3 = new ExecStockPricePredictionETrade(logger, ETadeSettings, SchwabSettings, httpClientFactory);
            exec2 = new ExecCryptoPricePrediction(logger, CoinBaseSettings, TaAPISettings, httpClientFactory);
            helper = new Helper(logger, SchwabSettings, ETadeSettings, yfapiSettings, httpClientFactory);
            try
            {
                //if (helper.IsNotHolidayorWeekend())
                //{
                    //helper.UnloadAgingStocks();
                    helper.CheckSchwabOrderStatus(ref totalAvailiableforLongTradeSchwab);
                    helper.CheckETradeOrderStatus(ref totalAvailiableforLongTradeETrade);

                    if (totalAvailiableforLongTradeSchwab > 100 || totalAvailiableforLongTradeETrade > 100)
                        BuySellStockList = helper.GetQuoteList();

                    //    if (totalAvailiableforLongTradeSchwab > 100)
                    exec1.Exec(totalAvailiableforLongTradeSchwab, ref BuySellStockList);
                    //    else
                    //        logger.LogInformation($"Not enough buying power in Schwab account to make a trade. ${totalAvailiableforLongTradeSchwab}");
                //}
                //else
                //{
                //    logger.LogError("Time is outside of trading window.");
                //    goto ExecCrypto;
                //}
            }
            catch (Exception ex)
            {
                logger.LogError($"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
            try
            {
                //if (totalAvailiableforLongTradeETrade > 100)
                    exec3.Exec(totalAvailiableforLongTradeETrade, ref BuySellStockList);
                //else
                //    logger.LogInformation($"Not enough buying power in ETrade account to make a trade. ${totalAvailiableforLongTradeETrade}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
        ExecCrypto:
            try
            {
                exec2.Exec();
            }
            catch (Exception ex)
            {
                logger.LogError($"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
            logger.LogInformation($"Stop Time {DateTime.Now}");
            exec1 = null;
            exec2 = null;
            exec3 = null;
            System.Threading.Thread.Sleep(240000); //four minutes
        } while (true);

    }
}

public class Versioning
{
    public string GetAssemblyVersion()
    {
        return GetType().Assembly.GetName().Version.ToString();
    }
}
