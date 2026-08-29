// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Runtime;
using NewsAggregator.models.returnModel;
using System.Configuration;
using TMD.PricePrediction;
using TMD.PricePrediction.Classes;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using static Tensorboard.ApiDef.Types;


namespace TMD.EquitiesPrediction;
class PricePrediction //: BackgroundService
{
    static ExecStockPricePredictionSchwab? exec1 = null;
    static ExecStockPricePredictionETrade? exec3 = null;
    static ExecCryptoPricePrediction? exec2 = null;
    static ExecKalshiPricePrediction? exec4 = null;
    //static KalshiBalanceAndBuy kalshiBalanceAndBuy = null;
    static Thread[] orderThread = new Thread[3];
    static Task? task1 = null;
    static CancellationTokenSource? cts = null;
    static TmdContext? ctx = null;
    //static Versioning? versioning = null;
    static Helper? helper = null;
    static CryptoPriceCheck? cryptoPriceCheck = null;
    static void Main(string[] args)
    //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //Console.WriteLine("Loading Account Info, StockkList, IPO's, and Earnings");
        //var builder = new ConfigurationBuilder();
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        //IConfiguration config = builder.Build();
        IConfiguration config = builder.Configuration;

        var ConnectionStrings = config.GetSection("ConnectionStrings").Get<ConnectionSettings>();
        var MailSettings = config.GetSection("MailSettings").Get<MailSettings>();
        var FinnHub = config.GetSection("finnhub").Get<FinnHub>();
        var Polygon = config.GetSection("polygon").Get<Polygon>();
        var SchwabSettings = config.GetSection("SchwabAPI").Get<SchwabSettings>();
        var CoinBaseSettings = config.GetSection("CoinBase").Get<CoinBaseSettings>();
        var ETadeSettings = config.GetSection("ETradeAPI").Get<ETradeSettings>();
        var TaAPISettings = config.GetSection("TaApi").Get<TaAPISettings>();
        var yfapiSettings = config.GetSection("yfapi").Get<yfapiSettings>();
       
        // 1. Create the service collection
        //var services = new ServiceCollection();

        // 2. Register the DbContextFactory with your database connection string
        //services.AddDbContextFactory<TmdContext>(options =>
        //    options.UseSqlServer(ConnectionStrings.Connection));

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            //builder.AddDebugOutput();
            builder.SetMinimumLevel(LogLevel.Trace);
        }
        );
  
        var logger = loggerFactory.CreateLogger("Price Prediction");
        //versioning = new Versioning();

        //Helper.Log(logger, LogLevel.Error, "Technical Analysis! Version Test Log: " + versioning.GetAssemblyVersion());
        // 1. Create the service collection
        //var services = new ServiceCollection();//.AddHttpClient("TMD")
        //.UseSocketsHttpHandler((handler, _) =>
        //    handler.PooledConnectionLifetime = TimeSpan.FromMinutes(7)) // Recreate connection every 2 minutes
        //.SetHandlerLifetime(Timeout.InfiniteTimeSpan); // Disable rotation, as it is handled by PooledConnectionLifetime

        // 2. Register the DbContextFactory with your database connection string
        builder.Services.AddDbContextFactory<TmdContext>(options =>
            options.UseSqlServer(ConnectionStrings.Connection));

        //// 3. Register your main application logic worker/service
        builder.Services.AddTransient<PricePrediction>();

        // 4. Execute your application logic
        //var worker = services.GetRequiredService<AppWorker>();
        //await worker.ExecuteAsync();

        //var host = builder.Build();

        var client = builder.Services.AddHttpClient("TMD")
        .UseSocketsHttpHandler((handler, _) =>
            handler.PooledConnectionLifetime = TimeSpan.FromMinutes(7)) // Recreate connection every 2 minutes
        .SetHandlerLifetime(Timeout.InfiniteTimeSpan); // Disable rotation, as it is handled by PooledConnectionLifetime
        builder.Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

        var serviceProvider = client.Services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        ctx = serviceProvider.GetService<TmdContext>();
        // 4. Execute your application logic
        //var scope = serviceProvider.CreateScope();
        //var worker = scope.ServiceProvider.GetRequiredService<PricePrediction>();
        var ctxFactory = serviceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
        //using (var scope = serviceProvider.CreateScope())
        //{
        //    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
        //    var context = ctxFactory.CreateDbContext();
        //    var cryptoList = context.CryptoList.ToList();
        //}
        var host = builder.Build();
        var serviceCollection = client.Services;
        //ctx = new TmdContext();
        //Helper? helper = new Helper(logger, SchwabSettings, ETadeSettings, yfapiSettings, httpClientFactory, serviceProvider);
        //helper = new Helper(logger, SchwabSettings, ETadeSettings, yfapiSettings, ctxFactory);

        List<BuySellCommodity>? BuySellStockList = new List<BuySellCommodity>();
        exec1 = new ExecStockPricePredictionSchwab(logger, ETadeSettings, SchwabSettings, httpClientFactory, ctxFactory);
        exec3 = new ExecStockPricePredictionETrade(logger, ETadeSettings, SchwabSettings, httpClientFactory, ctxFactory);
        exec2 = new ExecCryptoPricePrediction(logger, CoinBaseSettings, httpClientFactory, ctxFactory);
        //exec4 = new ExecKalshiPricePrediction(logger, CoinBaseSettings, httpClientFactory, helper/*, serviceProvider*/);
        decimal totalAvailiableforLongTradeSchwab = 0m;
        decimal totalAvailiableforLongTradeETrade = 0m;
        cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;
        //kalshiBalanceAndBuy = new KalshiBalanceAndBuy(logger, httpClientFactory);

        //orderThread[0] = new Thread(() => kalshiBalanceAndBuy.Exec(token));
        //orderThread[0].Start();
        cryptoPriceCheck = new CryptoPriceCheck(logger, CoinBaseSettings, httpClientFactory, ctxFactory);

        //task1 = new Task(() =>
        //{
            
        //    cryptoPriceCheck.Exec(token);
        //    //cryptoPriceCheck = null;
        //}, token, TaskCreationOptions.LongRunning);
        ////Task.Run(() => task1.Start());
        //task1.Start();
        StartCryptoCheckTask(token);
        do
        {
            helper = new Helper(logger, SchwabSettings, ETadeSettings, yfapiSettings, ctxFactory);
            Helper.Log(logger, LogLevel.Information, "Technical Analysis! Version " + GetAssemblyVersion());
            Helper.Log(logger, LogLevel.Information, $"Run Time {DateTime.Now}");

            //try
            //{

            //goto ExecCrypto;
            //Stocks:
            //task1 = new Task(() =>   /*Task.Run(() =>*/
            //{
            //do
            //{
            //try
            //{
            //    if (helper.IsNotHolidayorWeekend())
            //    {
            //        //helper.UnloadAgingStocks();
            //        helper.CheckSchwabOrderStatus(ref totalAvailiableforLongTradeSchwab);
            //        helper.CheckETradeOrderStatus(ref totalAvailiableforLongTradeETrade);
            //        //totalAvailiableforLongTradeSchwab = 100;
            //        //totalAvailiableforLongTradeETrade = 100;
            //        if (totalAvailiableforLongTradeSchwab > 5 || totalAvailiableforLongTradeETrade > 5)
            //            BuySellStockList = helper.GetQuoteList();
            //        exec1.Exec(totalAvailiableforLongTradeSchwab, token, ref BuySellStockList);
            //        exec3.Exec(totalAvailiableforLongTradeETrade, token, ref BuySellStockList);
            //    }
            //    else
            //    {
            //        Helper.Log(logger, LogLevel.Error, "Time is outside of trading window.");
            //        //goto ExecCrypto;
            //    }
            //}
            //catch (Exception exe)
            //{
            //    Helper.Log(logger, LogLevel.Error, $"Error: {exe.Source} - {exe.StackTrace} - {exe?.InnerException?.Message ?? ""} - {exe.Message}");
            //}
            //        System.Threading.Thread.Sleep(5000);
            //    } while (true);
            //}, token, TaskCreationOptions.LongRunning);
            //Task.Run(() => task1.Start());

            //    }
            //catch (Exception ex)
            //{
            //    helper.Log(logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            //}
            //try
            //{
            //    exec3.Exec(totalAvailiableforLongTradeETrade, token, ref BuySellStockList);
            //}
            //catch (Exception ex)
            //{
            //    helper.Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            //}
        ExecCrypto:
            try
            {
                //orderThread[1] = new Thread(() =>
                //{
                //    do
                //    {
                        exec2.Exec(token);
                //        System.Threading.Thread.Sleep(5000);
                //    } while (true);
                //});
                //orderThread[1].Start();
            }
            catch (Exception ex)
            {
                Helper.Log(logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
        //goto Stocks;
        //try
        //{
            //orderThread[2] = new Thread(() =>
            //{
            //    do
            //    {
            //        exec4.Exec(token);
            //        System.Threading.Thread.Sleep(5000);
            //    } while (true);
            //});
            //orderThread[2].Start();
        //}
        //catch (Exception ex)
        //{
        //    Helper.Log(logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
        //}
        Helper.Log(logger, LogLevel.Information, $"Stop Time {DateTime.Now}");
            //exec1 = null;
            //exec2 = null;
            //exec3 = null;
            //exec4 = null;
            //System.Threading.Thread.Sleep(240000); //four minutes
            helper = null;
            Task.Delay(250, token); ; 
            if(task1.Status == TaskStatus.RanToCompletion || task1.Status == TaskStatus.Faulted || task1.Status == TaskStatus.Canceled)
            {
                StartCryptoCheckTask(token);
            }
        } while (true);

    }
    private static string GetAssemblyVersion()
    {
        return typeof(PricePrediction).Assembly.GetName().Version.ToString();
    }
    private static void StartCryptoCheckTask(CancellationToken token)
    {
        task1 = new Task(() =>
        {
            cryptoPriceCheck.Exec(token);
        }, token, TaskCreationOptions.LongRunning);
        task1.Start();
    }
    ~PricePrediction()
    {
        foreach (var thread in orderThread)
        {
            if (thread != null && thread.IsAlive)
            {
                thread.Join(); // Wait for the thread to finish
            }
        }

        cts.Cancel();
        orderThread = null;
        task1?.Dispose();
        task1 = null;
        cryptoPriceCheck = null;
        ctx?.Dispose();
        ctx = null;
        exec1 = null;
        exec2 = null;
        exec3 = null;
        exec4 = null;
        helper = null;
    }
}

//public class Versioning
//{
//    public string GetAssemblyVersion()
//    {
//        return GetType().Assembly.GetName().Version.ToString();
//    }
//}
