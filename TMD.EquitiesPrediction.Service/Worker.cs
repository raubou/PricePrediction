using GraphQLParser;
using log4net.Repository.Hierarchy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using TMD.PricePrediction;
using TMD.PricePrediction.Classes;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;

namespace TMD.EquitiesPrediction.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker>? _logger;
        private readonly KalshiSettings? _kalshiSettings;
        private readonly IHttpClientFactory? _httpClientFactory;
        private IDbContextFactory<TmdContext>? _contextFactory;
        CoinBaseSettings? _coinBaseSettings;
        ExecKalshiPricePrediction? execKalshiPricePrediction = null;
        KalshiBalanceAndBuy kalshiBalanceAndBuy = null;
        Helper? _helper = null;
        Task? task1 = null;
        TmdContext? ctx = null;
        public Worker(IOptions<KalshiSettings> kalshiSettings, IOptions<CoinBaseSettings> coinBaseSettings, IHttpClientFactory httpClientFactory, IDbContextFactory<TmdContext> contextFactory, ILogger<Worker> logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _kalshiSettings = kalshiSettings.Value;
            _coinBaseSettings = coinBaseSettings.Value;
            _contextFactory = contextFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Helper.Log(_logger, LogLevel.Information, "Technical Analysis! Version " + GetAssemblyVersion());
            kalshiBalanceAndBuy = new KalshiBalanceAndBuy(_logger, _httpClientFactory, _contextFactory);
            var context = await _contextFactory.CreateDbContextAsync();
                                                                       
            ctx = context;
            //task1 = new Task(() =>
            //{

            //    kalshiBalanceAndBuy.Exec(stoppingToken);

            //}, stoppingToken, TaskCreationOptions.LongRunning);
            //Task.Run(() => task1.Start());
            _helper = new Helper(_logger, null, null, null, _contextFactory);
            execKalshiPricePrediction = new ExecKalshiPricePrediction(_logger, _coinBaseSettings, _httpClientFactory, _helper, _contextFactory);
            KalshiBalanceAndBuy(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    //if (_logger.IsEnabled(LogLevel.None))
                    //{
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);




                    execKalshiPricePrediction.Exec(stoppingToken);
                    //execKalshiPricePrediction = null;
                    //_helper = null;
                    //}
                    await Task.Delay(250, stoppingToken);
                    if (task1.Status == TaskStatus.RanToCompletion || task1.Status == TaskStatus.Faulted || task1.Status == TaskStatus.Canceled)
                    {
                        KalshiBalanceAndBuy(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Helper.Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
                }
            }

        }
        private void KalshiBalanceAndBuy(CancellationToken token)
        {
            task1 = new Task(() =>
            {
                kalshiBalanceAndBuy.Exec(token);
            }, token, TaskCreationOptions.LongRunning);
            task1.Start();
        }
        private string GetAssemblyVersion()
        {
            return GetType().Assembly.GetName().Version.ToString();
        }
        ~Worker()
        {
            execKalshiPricePrediction = null;
            kalshiBalanceAndBuy = null;
            _helper = null;
            task1 = null;
            ctx.Dispose();
            ctx = null;
            _contextFactory = null;
        }
    }

}

