using log4net.Repository.Hierarchy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using TMD.PricePrediction;
using TMD.PricePrediction.Classes;
using TMDInvestments.Context;
using TMDInvestments.Models;

namespace TMD.EquitiesPrediction.StockService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        Helper? helper = null;
        private readonly ETradeSettings? _eTradeSettings;
        private readonly SchwabSettings? _schwabSettings;
        private readonly IHttpClientFactory? _httpClientFactory;
        private IDbContextFactory<TmdContext>? _contextFactory;
        ExecStockPricePredictionSchwab? exec1 = null;
        ExecStockPricePredictionETrade? exec3 = null;


        public Worker(ILogger<Worker> logger, IOptions<ETradeSettings> eTradeSettings, IOptions<SchwabSettings> schwabSettings, IHttpClientFactory httpClientFactory, IDbContextFactory<TmdContext> contextFactory)
        {
            _logger = logger;
            _eTradeSettings = eTradeSettings.Value;
            _schwabSettings = schwabSettings.Value;
            _httpClientFactory = httpClientFactory;
            _contextFactory = contextFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Helper.Log(_logger, LogLevel.Information, "Technical Analysis! Version " + GetAssemblyVersion());
            while (!stoppingToken.IsCancellationRequested)
            {
                decimal totalAvailiableforLongTradeSchwab = 500m;
                decimal totalAvailiableforLongTradeETrade = 500m;
                List<BuySellCommodity>? BuySellStockList = new List<BuySellCommodity>();
                helper = new Helper(_logger, _schwabSettings, _eTradeSettings, null, _contextFactory);
                exec1 = new ExecStockPricePredictionSchwab(_logger, _eTradeSettings, _schwabSettings, _httpClientFactory, _contextFactory);
                exec3 = new ExecStockPricePredictionETrade(_logger, _eTradeSettings, _schwabSettings, _httpClientFactory, _contextFactory);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                //if (helper.IsNotHolidayorWeekend())
                //{
                    //helper.UnloadAgingStocks();
                    //helper.CheckSchwabOrderStatus(ref totalAvailiableforLongTradeSchwab);
                    //helper.CheckETradeOrderStatus(ref totalAvailiableforLongTradeETrade);
                    //totalAvailiableforLongTradeSchwab = 100;
                    //totalAvailiableforLongTradeETrade = 100;
                    if (totalAvailiableforLongTradeSchwab > 5 || totalAvailiableforLongTradeETrade > 5)
                        BuySellStockList = helper.GetQuoteList();
                    exec1.Exec(totalAvailiableforLongTradeSchwab, stoppingToken, ref BuySellStockList);
                    exec3.Exec(totalAvailiableforLongTradeETrade, stoppingToken, ref BuySellStockList);
                //}
                //else
                //{
                //    Helper.Log(_logger, LogLevel.Error, "Time is outside of trading window.");
                //    //goto ExecCrypto;
                //}
                exec1 = null;
                exec3 = null;
                helper = null;
                await Task.Delay(1000, stoppingToken);
            }
        }
        private string GetAssemblyVersion()
        {
            return GetType().Assembly.GetName().Version.ToString();
        }
        ~Worker()
        {
            exec1 = null;
            exec3 = null;
            helper = null;
            //task1 = null;
        }
    }
}
