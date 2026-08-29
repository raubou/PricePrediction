using Coinbase.AdvancedTrade.Models.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using TMD.Services;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.Polygon;
using TMDInvestments.Services;

namespace TMD.PricePrediction
{
    public sealed class Helper
    {
        SchwabSettings? _settings = null;
        ETradeSettings? _settings2 = null;
        static RulesEngine? rulesEngine = null;
        TraderGoService _traderGoService = null;
        private readonly ILogger? _logger = null;
        private readonly IHttpClientFactory? _factory = null;
        SchwabServices? schwabServices = null;
        ETradeService? services2 = null;
        static TmdContext? _ctx = null;
        Polygon? _polygon = null;
        yfapiSettings? _yfapiSettings = null;
        static DateTime runTime;// = easternTime;
        static public DateTime easternTime;
        static CancellationTokenSource cts = null;
        static CancellationToken token;
        static readonly System.Threading.Lock _transactLock = new();

        static Helper()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json");
            IConfigurationRoot config = builder.Build();
            var ConnectionStrings = config.GetSection("ConnectionStrings").Get<ConnectionSettings>();
            var services = new ServiceCollection();
            services.AddDbContextFactory<TmdContext>(options =>
                options.UseSqlServer(ConnectionStrings.Connection));
            services.AddTransient<Helper>();
            var serviceProvider = services.BuildServiceProvider();
            var ctx = serviceProvider.GetService<TmdContext>();
            _ctx = ctx;
            //using (var scope = serviceProvider.CreateScope())
            //{
            //    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
            //    var context = ctxFactory.CreateDbContext();
            //    _ctx = context;
            //}
            rulesEngine = _ctx.RulesEngine.FirstOrDefault();
            SetTimeZone();
        }
        //public Helper(ILogger _logger, SchwabSettings settings, ETradeSettings settings2)
        //{
        //    //IConfigurationBuilder builder = new ConfigurationBuilder()
        //    //.AddJsonFile("appsettings.json");
        //    //IConfigurationRoot config = builder.Build();
        //    //var ConnectionStrings = config.GetSection("ConnectionStrings").Get<ConnectionSettings>();
        //    //var services = new ServiceCollection();
        //    //services.AddDbContextFactory<TmdContext>(options =>
        //    //    options.UseSqlServer(ConnectionStrings.Connection));
        //    ////services.AddTransient<PricePrediction>();
        //    //_serviceProvider = services.BuildServiceProvider();
        //    //using (var scope = _serviceProvider.CreateScope())
        //    //{
        //    //    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
        //    //    var context = ctxFactory.CreateDbContext();
        //    //    _ctx = context;
        //    //}
        //    //this._logger = _logger;
        //    //_settings = settings;
        //    //_settings2 = settings2;
        //    //schwabServices = new SchwabServices(_logger, _settings, _factory);
        //    //if(settings2 != null) 
        //    //    services2 = new ETradeService(_logger, _settings2, _factory);
        //    ////_traderGoService = new TraderGoService(_yfapiSettings, _logger, _factory, false);
        //    //rulesEngine = _ctx.RulesEngine.FirstOrDefault();
        //    SetTimeZone();
        //}
        public Helper(ILogger logger, SchwabSettings settings, ETradeSettings settings2, yfapiSettings yfapiSettings, IDbContextFactory<TmdContext> contextFactory)
        {
            //var services = new ServiceCollection();
            //var serviceProvider = services.BuildServiceProvider();
            //var ctx = serviceProvider.GetService<TmdContext>();
            _ctx = contextFactory.CreateDbContext();
            //using (var scope = serviceProvider.CreateScope())
            //{
            //    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
            //    var context = ctxFactory.CreateDbContext();
            //    _ctx = context;
            //}
            this._logger = logger;
            _settings = settings;
            _settings2 = settings2;
            _yfapiSettings = yfapiSettings;
            //_ctx = ctx;
            if (_settings != null)
                schwabServices = new SchwabServices(_logger, _settings, _factory, contextFactory);
            if (_settings2 != null)
                services2 = new ETradeService(_logger, _settings2, _factory, contextFactory);
            if (_yfapiSettings != null)
                _traderGoService = new TraderGoService(_yfapiSettings, _logger, _factory, false, contextFactory);
            lock (_transactLock)
            {
                rulesEngine = _ctx.RulesEngine.FirstOrDefault();
            }            
            SetTimeZone();
        }
        //public Helper(ILogger _logger)
        //{
        //    var services = new ServiceCollection();
        //    var serviceProvider = services.BuildServiceProvider();
        //    var ctx = serviceProvider.GetService<TmdContext>();
        //    _ctx = ctx;
        //    //using (var scope = serviceProvider.CreateScope())
        //    //{
        //    //    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
        //    //    var context = ctxFactory.CreateDbContext();
        //    //    _ctx = context;
        //    //}
        //    this._logger = _logger;
        //    //schwabServices = new SchwabServices(_logger, _settings, _factory);
        //    //services2 = new ETradeService(_logger, _settings2, _factory);
        //    rulesEngine = _ctx.RulesEngine.FirstOrDefault();
        //    SetTimeZone();
        //}
        //public Helper(ServiceProvider serviceProvider)
        //{
        //    SetTimeZone();
        //    //ctx = new TmdContext()
        //    //_ctx = ctx;
        //    _serviceProvider = serviceProvider;
        //    using (var scope = _serviceProvider.CreateScope())
        //    {
        //        var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
        //        var context = ctxFactory.CreateDbContext();
        //        _ctx = context;
        //        //rulesEngine = context.RulesEngine.FirstOrDefault();
        //    }
        //    cts = new CancellationTokenSource();
        //    token = cts.Token;
        //}
        //public Helper(TmdContext ctx)
        //{
        //    SetTimeZone();
        //    //ctx = new TmdContext()
        //    _ctx = ctx;
        //    cts = new CancellationTokenSource();
        //    token = cts.Token;
        //}
        //public Helper(ILogger logger, SchwabSettings settings, IHttpClientFactory factory, ServiceProvider serviceProvider)
        //{
        //    _logger = logger;
        //    _settings = settings;
        //    _factory = factory;
        //    //_ctx = ctx;
        //    _serviceProvider = serviceProvider;
        //    cts = new CancellationTokenSource();
        //    token = cts.Token;
        //    //rulesEngine = ctx.RulesEngine.FirstOrDefault();
        //    using (var scope = serviceProvider.CreateScope())
        //    {
        //        var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
        //        var context = ctxFactory.CreateDbContext();
        //        _ctx = context;
        //        rulesEngine = context.RulesEngine.FirstOrDefault();
        //    }
        //    services = new SchwabServices(_logger, _settings, _factory, serviceProvider);
        //    SetTimeZone();
        //}
        //public Helper(ILogger logger, SchwabSettings settings, ETradeSettings settings2, IHttpClientFactory factory, ServiceProvider serviceProvider)
        //{
        //    _logger = logger;
        //    _settings = settings;
        //    _settings2 = settings2;
        //    _factory = factory;
        //    //_ctx = ctx;
        //    //ctx = new TmdContext();
        //    _serviceProvider = serviceProvider;
        //    cts = new CancellationTokenSource();
        //    token = cts.Token;
        //    //rulesEngine = ctx.RulesEngine.FirstOrDefault();
        //    using (var scope = serviceProvider.CreateScope())
        //    {
        //        var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
        //        var context = ctxFactory.CreateDbContext();
        //        _ctx = context;
        //        rulesEngine = context.RulesEngine.FirstOrDefault();
        //    }
        //    services = new SchwabServices(_logger, _settings, _factory, serviceProvider);
        //    services2 = new ETradeService(_logger, _settings2, _factory, this);
        //    SetTimeZone();
        //}
        //public Helper(ILogger logger, SchwabSettings settings, ETradeSettings settings2, yfapiSettings yfapiSettings, IHttpClientFactory factory, ServiceProvider serviceProvider)
        //{
        //    _logger = logger;
        //    _settings = settings;
        //    _settings2 = settings2;
        //    _factory = factory;
        //    //_ctx = ctx;
        //    //ctx = new TmdContext();
        //    _serviceProvider = serviceProvider;
        //    cts = new CancellationTokenSource();
        //    token = cts.Token;
        //    //rulesEngine = _ctx.RulesEngine.FirstOrDefault();
        //    using (var scope = serviceProvider.CreateScope())
        //    {
        //        var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
        //        var context = ctxFactory.CreateDbContext();
        //        _ctx = context;
        //        rulesEngine = context.RulesEngine.FirstOrDefault();
        //    }
        //    services = new SchwabServices(_logger, _settings, _factory, serviceProvider);
        //    services2 = new ETradeService(_logger, _settings2, _factory, this);
        //    _traderGoService = new TraderGoService(yfapiSettings, _logger, _factory, false);
        //    SetTimeZone();
        //}
        public List<BuySellCommodity> GetQuoteList()
        {
            int? shortCnt = 0;
            int? longCnt = 0;
            int? stockCnt = 0;
            int? vixsandpLongCnt = 0;
            int? vixsandpShortCnt = 0;
            //int? SandPLong = 0;
            //int? SandPShort = 0;
            //int? DOWLong = 0;
            //int? DOWShort = 0;
            //int? NASDAQLong = 0;
            //int? NASDAQShort = 0;

            StockTracker2 stockTrackerInd = null;
            var BuySellStockList = new List<BuySellCommodity>();


            //Get stock already in trade being tracked.
            var stockTracker = _ctx.StockTracker2.AsNoTracking().Where(x => x.PriceSold == null && x.DateSold == null && x.Status == "Filled"/* && x.TradePlatform == (int)Platform.CharlesSchwab*/);//.ToList();

            //if (stockTracker.Where(x => x.TradePlatform == (int)Platform.CharlesSchwab).Count() >= (rulesEngine.MaxNumberofStockstoBuy / 2) &&
            //    stockTracker.Where(x => x.TradePlatform == (int)Platform.ETrade).Count() >= (rulesEngine.MaxNumberofStockstoBuy / 2))
            //{
            //    Log(_logger, LogLevel.Warning, $"Stock count for Schwab and Etrade exceed max setting of {rulesEngine.MaxNumberofStockstoBuy}");
            //    return BuySellStockList;
            //}
            
            var stockTrackerList = stockTracker.Select(y => y.Symbol);
            Log(_logger, LogLevel.Information, $"Get existing stock tracker");

            //Get list of stocks to trade not in tracker
            var stockList = _ctx.StockList.AsNoTracking().Where(x => !stockTrackerList.Contains(x.Symbol) && x.ExcludeFromAutomation != true).OrderBy(y => Guid.NewGuid());//.ToList();
            Log(_logger, LogLevel.Information, $"Get stock list to trade");

            //stock sold/brought to cover in the last 24 hrs
            DateTime? minDate = DateTime.MinValue;
            var StockHold24Hr = _ctx.StockTracker2.AsNoTracking().AsEnumerable().Where(x => x.PriceSold != null && x.DateSold != null && Math.Abs((x.DateSold - easternTime).Value.TotalHours) < 24).Select(y => new { Symbol = y.Symbol, DateSold = y.DateSold, DateUpdated = y.DateUpdated }).ToList();
            //Include cancelled item for a 24h hold
            StockHold24Hr.AddRange(_ctx.StockTracker2.AsNoTracking().AsEnumerable().Where(x => x.Status == "Cancelled" && Math.Abs((x.DateUpdated - easternTime).Value.TotalHours) < 24).Select(y => new { Symbol = y.Symbol, DateSold = y.DateSold ?? minDate, DateUpdated = y.DateUpdated }));

            ///Test blank out to test stock quote list count
            //if (stockTracker.Where(x => x.TradePlatform == (int)Platform.CharlesSchwab).Count() >= (settings.MaxNumberofStockstoBuy / 2) &&
            //    stockTracker.Where(x => x.TradePlatform == (int)Platform.ETrade).Count() >= (settings.MaxNumberofStockstoBuy / 2))
            //{
            //    Log(_logger, LogLevel.Warning, $"Stock count for Schwab and Etrade exceed max setting of {settings.MaxNumberofStockstoBuy}");
            //    return new List<BuySellCommodity>();
            //}

            //S&P
            //var sandp = services.GetQuote("$SPX", true, false);
            //var vix = services.GetQuote("$VIX", true, false);

            ////DOW JONES
            //var dow = services.GetQuote("$DJI", true, false);
            //var vxd = services.GetQuote("$VXD", true, false);

            ////NASDAQ
            //var nasdaq = services.GetQuote("$NDX", true, false);
            //var vxn = services.GetQuote("$VXN", true, false);

            //if (!CheckMarketIndexes(vxn) || !CheckMarketIndexes(vxd) || !CheckMarketIndexes(vix) || !CheckMarketIndexes(sandp) || !CheckMarketIndexes(dow) || !CheckMarketIndexes(nasdaq))
            //{
            //    Log(_logger, LogLevel.Error, "Failed to get VIX or S&P or Dow or Nasdaq quote.");
            //    return null;
            //}
            //Log(_logger, LogLevel.Information, $"S&P: {sandp["lastPrice"].ToString()}. Dow: {dow["lastPrice"].ToString()}. Nasdaq: {nasdaq["lastPrice"].ToString()}");

            //Disabling Calculate VIX and S&P points
            //CheckDowNasdaqandSandP(vix, sandp, dow, vxd, nasdaq, vxn, settings?.StockLongCount, settings?.StockShortCount, _logger, ref SandPLong, ref SandPShort, ref DOWLong, ref DOWShort, ref NASDAQLong, ref NASDAQShort);
            //Log(_logger, LogLevel.Information, $"DOW Long: {DOWLong}. S&P Long: {SandPLong}. NASDAQ Long: {NASDAQLong}");
            //Log(_logger, LogLevel.Information, $"DOW Short: {DOWShort}. S&P Short: {SandPShort}. NASDAQ Short: {NASDAQShort}");

            //if (SandPLong == 1)
            //{
            //    vixsandpLongCnt = 1;
            //}
            //if (SandPShort == 1)
            //{
            //    vixsandpShortCnt = 1;
            //}
            //if (DOWLong > 0 && SandPLong > 0 && NASDAQLong > 0)
            //{
            //    vixsandpLongCnt = 1;
            //}
            //if (DOWShort > 0 && SandPShort > 0 && NASDAQShort > 0)
            //{
            //    vixsandpShortCnt = 1;
            //}

            if(runTime.Hour == 11 && (runTime.Minute >= 00 && runTime.Minute <= 29))
            {
                Log(_logger, LogLevel.Information, $"Getting movers from Yahoo");
                var GainersandLosers = _traderGoService.GetGainersandLosers(true);
                foreach (var gainer in GainersandLosers.Gainers)
                {
                    shortCnt = 0;
                    longCnt = 0;
                    if (gainer == null || string.IsNullOrEmpty(gainer["Symbol"].ToString()))
                        continue;
                    //if (gainer?["Exchange"]?.ToString()?.ToLower()?.Contains("otc"))
                    //    continue;
                    string symbol = gainer["Symbol"].ToString().Trim();
                    try
                    {
                        stockTrackerInd = stockTracker.AsNoTracking().Where(x => x.Symbol == symbol && x.PriceSold == null && x.DateSold == null && x.Status == "Filled").FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Error, $"Error in retrieving quote {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                        continue;
                    }

                    if (stockTrackerInd == null)
                    {
                        _logger.LogInformation($"Checking {gainer["Symbol"].ToString()} - ({gainer["Symbol"].ToString()})");
                        //if (Convert.ToDecimal(gainer["volume"].ToString()) < 40000)
                        //{
                        //    Log(_logger, LogLevel.Information, $"Not buying/selling short {gainer["Symbol"].ToString()} - ({gainer["Symbol"].ToString()}). Volume fall below min of 40000");
                        //    continue;
                        //}
                        if (Convert.ToDecimal(gainer["lastPrice"].ToString()) >= rulesEngine.MinStockPrice && Convert.ToDecimal(gainer["lastPrice"].ToString()) <= rulesEngine.MaxStockPrice)
                        {
                            //CalculatePoints(gainer, ref shortCnt, ref longCnt, .1m, true);
                            CalculatePoints(gainer, ref longCnt, ref shortCnt, .005m, true);
                        }
                        else
                        {
                            Log(_logger, LogLevel.Information, $"Not buying/selling short {gainer["Symbol"].ToString()} - ({gainer["Symbol"].ToString()}). Stock fall above or below min stock price or is excluded");
                            continue;
                        }

                        Log(_logger, LogLevel.Information, $"{gainer["Symbol"].ToString()} - ({gainer["Name"].ToString()}) Score. Long score: {longCnt}. Short score {shortCnt}");

                        if ((longCnt + vixsandpLongCnt) >= rulesEngine.StockLongCount)
                        {
                            Log(_logger, LogLevel.Information, $"Adding {gainer["Symbol"].ToString()} - ({gainer["Name"].ToString()}) to potential long list.");
                            if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == gainer["Symbol"].ToString()))
                            {
                                BuySellStockList.Add(new BuySellCommodity((longCnt + vixsandpLongCnt), TradeDirection.BUY, gainer));
                                stockCnt++;
                            }
                        }
                        else
                        if ((shortCnt + vixsandpShortCnt) >= rulesEngine.StockShortCount && (bool)rulesEngine.ShortEnabled)
                        {
                            Log(_logger, LogLevel.Information, $"Adding {gainer["Symbol"].ToString()} - ({gainer["Name"].ToString()}) to potential short list.");
                            if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == gainer["Symbol"].ToString()))
                            {
                                BuySellStockList.Add(new BuySellCommodity((shortCnt + vixsandpShortCnt), TradeDirection.SELL_SHORT, gainer));
                                stockCnt++;
                            }
                        }
                        else
                        {
                            Log(_logger, LogLevel.Information, $"Not buying/selling short {gainer["Symbol"].ToString()} - ({gainer["Name"].ToString()}). Long score: {longCnt}. Short score {shortCnt}");
                        }
                    }
                }

                foreach (var loser in GainersandLosers.Losers)
                {
                    shortCnt = 0;
                    longCnt = 0;
                    if (loser == null || string.IsNullOrEmpty(loser["Symbol"].ToString()))
                        continue;
                    //if (loser?["Exchange"]?.ToString()?.ToLower()?.Contains("otc"))
                    //    continue;
                    string symbol = loser["Symbol"].ToString().Trim();
                    try
                    {
                        stockTrackerInd = stockTracker.AsNoTracking().Where(x => x.Symbol == symbol && x.PriceSold == null && x.DateSold == null && x.Status == "Filled").FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Error, $"Error in retrieving quote {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                        continue;
                    }

                    if (stockTrackerInd == null)
                    {
                        _logger.LogInformation($"Checking {loser["Symbol"].ToString()} - ({loser["Symbol"].ToString()})");
                        //if (Convert.ToDecimal(loser["volume"].ToString()) < 40000)
                        //{
                        //    Log(_logger, LogLevel.Information, $"Not buying/selling short {loser["Symbol"].ToString()} - ({loser["Symbol"].ToString()}). Volume fall below min of 40000");
                        //    continue;
                        //}
                        if (Convert.ToDecimal(loser["lastPrice"].ToString()) >= rulesEngine.MinStockPrice && Convert.ToDecimal(loser["lastPrice"].ToString()) <= rulesEngine.MaxStockPrice)
                        {
                            CalculatePoints(loser, /*true, false,*/ ref shortCnt, ref longCnt, .1m, true);
                        }
                        else
                        {
                            Log(_logger, LogLevel.Information, $"Not buying/selling short {loser["Symbol"].ToString()} - ({loser["Symbol"].ToString()}). Stock fall above or below min stock price or is excluded");
                            continue;
                        }

                        Log(_logger, LogLevel.Information, $"{loser["Symbol"].ToString()} - ({loser["Name"].ToString()}) Score. Long score: {longCnt}. Short score {shortCnt}");

                        if ((longCnt + vixsandpLongCnt) >= rulesEngine.StockLongCount)
                        {
                            Log(_logger, LogLevel.Information, $"Adding {loser["Symbol"].ToString()} - ({loser["Name"].ToString()}) to potential long list.");
                            if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == loser["Symbol"].ToString()))
                            {
                                BuySellStockList.Add(new BuySellCommodity((longCnt + vixsandpLongCnt), TradeDirection.BUY, loser));
                                stockCnt++;
                            }
                        }
                        else
                        if ((shortCnt + vixsandpShortCnt) >= rulesEngine.StockShortCount && (bool)rulesEngine.ShortEnabled)
                        {
                            Log(_logger, LogLevel.Information, $"Adding {loser["Symbol"].ToString()} - ({loser["Name"].ToString()}) to potential short list.");
                            if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == loser["Symbol"].ToString()))
                            {
                                BuySellStockList.Add(new BuySellCommodity((shortCnt + vixsandpShortCnt), TradeDirection.SELL_SHORT, loser));
                                stockCnt++;
                            }
                        }
                        else
                        {
                            Log(_logger, LogLevel.Information, $"Not buying/selling short {loser["Symbol"].ToString()} - ({loser["Name"].ToString()}). Long score: {longCnt}. Short score {shortCnt}");
                        }
                    }
                }
            }


            Log(_logger, LogLevel.Information, $"Getting movers from Schwab");           
            JsonObject[] movers = schwabServices.GetMovers();

            foreach(JsonObject mover in movers)
            {
                shortCnt = 0;
                longCnt = 0;
                if(mover == null || string.IsNullOrEmpty(mover["Symbol"].ToString()))
                    continue;
                if ((bool)mover?["Exchange"]?.ToString()?.ToLower()?.Contains("otc"))
                    continue;
                string symbol = mover["Symbol"].ToString().Trim();
                try
                {
                    stockTrackerInd = stockTracker.AsNoTracking().Where(x => x.Symbol == symbol && x.PriceSold == null && x.DateSold == null && x.Status == "Filled").FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Error, $"Error in retrieving quote {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                    continue;
                }

                if (stockTrackerInd == null)
                {
                    _logger.LogInformation($"Checking {mover["Symbol"].ToString()} - ({mover["Symbol"].ToString()})");
                    //if (Convert.ToDecimal(mover["volume"].ToString()) < 40000)
                    //{
                    //    Log(_logger, LogLevel.Information, $"Not buying/selling short {mover["Symbol"].ToString()} - ({mover["Symbol"].ToString()}). Volume fall below min of 40000");
                    //    continue;
                    //}
                    if (Convert.ToDecimal(mover["lastPrice"].ToString()) >= rulesEngine.MinStockPrice && Convert.ToDecimal(mover["lastPrice"].ToString()) <= rulesEngine.MaxStockPrice)
                    {
                        CalculatePoints(mover, /*true, false,*/ ref shortCnt, ref longCnt, .1m, true);
                    }
                    else
                    {
                        Log(_logger, LogLevel.Information, $"Not buying/selling short {mover["Symbol"].ToString()} - ({mover["Symbol"].ToString()}). Stock fall above or below min stock price or is excluded");
                        continue;
                    }

                    Log(_logger, LogLevel.Information, $"{mover["Symbol"].ToString()} - ({mover["Name"].ToString()}) Score. Long score: {longCnt}. Short score {shortCnt}");

                    if ((longCnt + vixsandpLongCnt) >= rulesEngine.StockLongCount)
                    {
                        Log(_logger, LogLevel.Information, $"Adding {mover["Symbol"].ToString()} - ({mover["Name"].ToString()}) to potential long list.");
                        if(!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == mover["Symbol"].ToString()))
                        {
                            BuySellStockList.Add(new BuySellCommodity((longCnt + vixsandpLongCnt), TradeDirection.BUY, mover));
                            stockCnt++;
                        }

                    }
                    else
                    if ((shortCnt + vixsandpShortCnt) >= rulesEngine.StockShortCount && (bool)rulesEngine.ShortEnabled)
                    {
                        Log(_logger, LogLevel.Information, $"Adding {mover["Symbol"].ToString()} - ({mover["Name"].ToString()}) to potential short list.");
                        if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == mover["Symbol"].ToString())) 
                        {
                            BuySellStockList.Add(new BuySellCommodity((shortCnt + vixsandpShortCnt), TradeDirection.SELL_SHORT, mover));
                            stockCnt++;
                        }
                    }
                    else
                    {
                        Log(_logger, LogLevel.Information, $"Not buying/selling short {mover["Symbol"].ToString()} - ({mover["Name"].ToString()}). Long score: {longCnt}. Short score {shortCnt}");
                    }
                }
            }

            Log(_logger, LogLevel.Information, $"Getting quotes from stock list");
            string[] quotes = stockList.AsNoTracking().Select(x => x.Symbol).ToArray();
            JsonObject[] quoteList = schwabServices.GetQuotes(string.Join(",", quotes), true, false);
            quoteList = quoteList.Where(item => item != null).ToArray();
            foreach (var quote in quoteList)
            {
                shortCnt = 0;
                longCnt = 0;
                if ((bool)quote?["Exchange"]?.ToString()?.ToLower()?.Contains("otc"))
                    continue;
                string symbol = quote?["Symbol"]?.ToString().Trim();
                var stock = stockList.AsNoTracking().Where(x => x.Symbol == symbol).FirstOrDefault();
                if (stock?.QuoteRetry >= 50)
                    continue;
                //Check it stock has been brought in the last 24 hrs
                if (StockHold24Hr.Any(y => y.Symbol == stock.Symbol))
                {
                    //if stock was cancelled, check updated date
                    try
                    {
                        Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) purchased {Math.Round(Math.Abs((StockHold24Hr.Where(y => y.Symbol == stock.Symbol).Select(z => z.DateSold).FirstOrDefault() - easternTime).Value.TotalHours), 2)} hours ago was purchased less than 24h ago, skipping");
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Warning, $"Stock {stock.Symbol} - ({stock.Name}) cancelled {Math.Round(Math.Abs((StockHold24Hr.Where(y => y.Symbol == stock.Symbol).Select(z => z.DateUpdated).FirstOrDefault() - easternTime).Value.TotalHours), 2)} hours ago was purchased less than 24h ago, skipping");
                    }
                    continue;
                }
                _logger.LogInformation($"Checking {stock.Symbol} - ({stock.Name})");

                //dynamic? results = null;
                //try
                //{
                //    results = services.GetQuoteAsync(stock.Symbol, true, false).Result;
                //}
                //catch (Exception ex)
                //{
                //    Log(_logger, LogLevel.Error, $"Error in retrieving quote for {stock.Symbol} - ({stock.Name}) {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                //    var st = stock;
                //    st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                //    _ctx.StockList.Attach(st);
                //    _ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                //    _ctx.SaveChanges();
                //    _ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                //    continue;
                //}
                //try
                //{
                //    if (results is JsonObject)
                //    {
                //        if (((JsonObject)results).TryGetPropertyValue("errors", out JsonNode errors))
                //        {
                //            //failed to get quote.
                //            Log(_logger, LogLevel.Error, $"Failed to get quote for {stock.Symbol} - ({stock.Name}) {errors.ToString()}");
                //            var st = stock;
                //            st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                //            _ctx.StockList.Attach(st);
                //            _ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                //            _ctx.SaveChanges();
                //            _ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                //            continue;
                //        }
                //    }
                //    else
                //    {
                //        if (((JsonElement)results).TryGetProperty("errors", out JsonElement errors))
                //        {
                //            //failed to get quote.
                //            Log(_logger, LogLevel.Error, $"Failed to get quote for {stock.Symbol} - ({stock.Name}) {errors.ToString()}");
                //            var st = stock;
                //            st.QuoteRetry = (st.QuoteRetry == null) ? 1 : st.QuoteRetry + 1;
                //            _ctx.StockList.Attach(st);
                //            _ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                //            _ctx.SaveChanges();
                //            _ctx.Entry(st).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                //            continue;
                //        }
                //    }
                //}
                //catch (Exception ex)
                //{
                //    Log(_logger, LogLevel.Error, $"Error in retrieving quote for {stock.Symbol} - ({stock.Name}) {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                //    continue;
                //}


                Log(_logger, LogLevel.Information, $"Exchange for {stock.Symbol} - ({quote?["Exchange"]?.ToString()})");

                //if (Convert.ToDecimal(results["volume"].ToString()) < 40000)
                //{
                //    Log(_logger, LogLevel.Information, $"Not buying/selling short {results["Symbol"].ToString()} - ({results["Symbol"].ToString()}). Volume fall below min of 40000");
                //    continue;
                //}
                if (Convert.ToDecimal(quote?["lastPrice"]?.ToString()) >= rulesEngine.MinStockPrice && Convert.ToDecimal(quote?["lastPrice"]?.ToString()) <= rulesEngine.MaxStockPrice)
                {
                    CalculatePoints(quote, /*true, false,*/ ref shortCnt, ref longCnt, .1m, false);
                }
                else
                {
                    Log(_logger, LogLevel.Information, $"Not buying/selling short {quote?["Symbol"]?.ToString()} - ({quote?["Symbol"]?.ToString()}). Stock fall below min stock price or is excluded");
                    continue;
                }

                Log(_logger, LogLevel.Information, $"{stock.Symbol} - ({stock.Name}) Score. Long score: {longCnt}. Short score {shortCnt}");

                if ((longCnt + vixsandpLongCnt) >= rulesEngine.StockLongCount)
                {
                    Log(_logger, LogLevel.Information, $"Adding {stock.Symbol} - ({stock.Name}) to potential long list.");
                    if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == quote?["Symbol"]?.ToString()))
                        BuySellStockList.Add(new BuySellCommodity((longCnt + vixsandpLongCnt), TradeDirection.BUY, quote));
                    stockCnt++;
                }
                else
                    if ((shortCnt + vixsandpShortCnt) >= rulesEngine.StockShortCount && (bool)rulesEngine.ShortEnabled)
                    {
                        Log(_logger, LogLevel.Information, $"Adding {stock.Symbol} - ({stock.Name}) to potential short list.");
                        if (!BuySellStockList.Any(x => x.Results["Symbol"].ToString() == quote?["Symbol"]?.ToString()))
                            BuySellStockList.Add(new BuySellCommodity((shortCnt + vixsandpShortCnt), TradeDirection.SELL_SHORT, quote));
                        stockCnt++;
                    }
                    else
                    {
                        Log(_logger, LogLevel.Information, $"Not buying/selling short {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}");
                    }
            }
            //BuySellStockList = BuySellStockList.Where(x => Convert.ToDecimal(x.Results?["RSI"]?.ToString()) <= 30 || Convert.ToDecimal(x.Results?["RSI"]?.ToString()) >= 70).ToList();
            return BuySellStockList;
        }
        public bool CheckMarketIndexes(dynamic index)
        {
            bool areValid = true;
            Type type = index.GetType();
            if (index is JsonObject)
            {
                if (((JsonObject)index).TryGetPropertyValue("errors", out JsonNode errors))
                {
                    //failed to get quote.
                    Log(_logger, LogLevel.Error, $"Failed to get quote for {type.Name} {errors.ToString()}");
                    areValid = false;
                }
            }
            if (index is JsonElement)
            {
                if (((JsonElement)index).TryGetProperty("errors", out JsonElement errors))
                {
                    //failed to get quote.
                    Log(_logger, LogLevel.Error, $"Failed to get quote for {type.Name} {errors.ToString()}");
                    areValid = false;
                }
            }
            return areValid;
        }
        public decimal CalculatePoints(JsonObject? results, ref int? shortCnt, ref int? longCnt, decimal? emaToVwapDiff, bool useRSI) {
            int? count = (useRSI) ? 3 : 6;
            //if (Convert.ToDecimal(results["VWAP"].ToString()) < Convert.ToDecimal(results["EMA9"].ToString()) && decimal.Parse(results["EMA9TOVWAPDIFF"].ToString(), System.Globalization.NumberStyles.Float) < emaToVwapDiff)
            //{ longCnt = longCnt + count; }

            //if (Convert.ToDecimal(results["VWAP"].ToString()) > Convert.ToDecimal(results["EMA9"].ToString()) && decimal.Parse(results["EMA9TOVWAPDIFF"].ToString(), System.Globalization.NumberStyles.Float) < emaToVwapDiff)
            //{ shortCnt = shortCnt + count; }

            if (Convert.ToDecimal(results["lastPrice"].ToString()) > Convert.ToDecimal(results["EMA9"].ToString()) && decimal.Parse(results["lastPrice"].ToString()) > decimal.Parse(results["VWAP"].ToString()))
            { longCnt = longCnt + count; }

            if (Convert.ToDecimal(results["lastPrice"].ToString()) < Convert.ToDecimal(results["EMA9"].ToString()) && decimal.Parse(results["lastPrice"].ToString()) < decimal.Parse(results["VWAP"].ToString()))
            { shortCnt = shortCnt + count; }

            if (useRSI && Convert.ToDecimal(results["RSI"].ToString()) > 0)
            {
                if (Convert.ToDecimal(results["RSI"].ToString()) >= 70)
                {
                    shortCnt = shortCnt + count;
                }
                if (Convert.ToDecimal(results["RSI"].ToString()) <= 30)
                {
                    longCnt = longCnt + count;
                }
            }

            return decimal.Parse(results["EMA9TOVWAPDIFF"].ToString(), System.Globalization.NumberStyles.Float);
        }
        //public void CalculatePointsLegacy(JsonObject? results/**, bool use31016, bool use5138,*/, ref int? shortCnt, ref int? longCnt)
        //{
        //    shortCnt = 0;
        //    longCnt = 0;

        //    if (Convert.ToDecimal(results["RSI"].ToString()) > 0)
        //    {
        //        if (Convert.ToDecimal(results["RSI"].ToString()) >= 70)
        //        {
        //            shortCnt = shortCnt + 1;
        //        }
        //        if (Convert.ToDecimal(results["RSI"].ToString()) <= 30)
        //        {
        //            longCnt = longCnt + 1;
        //        }
        //    }
        //    try
        //    {
        //        if (Convert.ToDecimal(results["VWAP"].ToString()) < Convert.ToDecimal(results["lastPrice"].ToString()))
        //        { longCnt = longCnt + 1; }

        //        if (Convert.ToDecimal(results["VWAP"].ToString()) > Convert.ToDecimal(results["lastPrice"].ToString()))
        //        { shortCnt = shortCnt + 1; }
        //    }
        //    catch (Exception ex)
        //    {

                
        //    }
        //    if (Convert.ToDecimal(results["EMA5"].ToString()) > Convert.ToDecimal(results["EMA20"].ToString()))
        //    { longCnt = longCnt + 1; }
        //    if (Convert.ToDecimal(results["EMA5"].ToString()) > Convert.ToDecimal(results["EMA50"].ToString()))
        //    { longCnt = longCnt + 1; }
        //    if (Convert.ToDecimal(results["SMA9"].ToString()) > Convert.ToDecimal(results["SMA21"].ToString()))
        //    { longCnt = longCnt + 1; }
        //    if (Convert.ToDecimal(results["SMA9"].ToString()) > Convert.ToDecimal(results["SMA50"].ToString()))
        //    { longCnt = longCnt + 1; }

        //    if (Convert.ToDecimal(results["EMA5"].ToString()) < Convert.ToDecimal(results["EMA20"].ToString()))
        //    { shortCnt = shortCnt + 1; }
        //    if (Convert.ToDecimal(results["EMA5"].ToString()) < Convert.ToDecimal(results["EMA50"].ToString()))
        //    { shortCnt = shortCnt + 1; }
        //    if (Convert.ToDecimal(results["SMA9"].ToString()) < Convert.ToDecimal(results["SMA21"].ToString()))
        //    { shortCnt = shortCnt + 1; }
        //    if (Convert.ToDecimal(results["SMA9"].ToString()) < Convert.ToDecimal(results["SMA50"].ToString()))
        //    { shortCnt = shortCnt + 1; }
        //    //if (use31016)
        //    //{
        //    //// 3, 10, 16 STRATEGY
        //    //if (Convert.ToDecimal(results["EMA3"].ToString()) > Convert.ToDecimal(results["EMA10"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["EMA3"].ToString()) > Convert.ToDecimal(results["EMA16"].ToString()) &&
        //    //    Convert.ToDecimal(results["EMA10"].ToString()) > Convert.ToDecimal(results["EMA16"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA3"].ToString()) > Convert.ToDecimal(results["SMA10"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA3"].ToString()) > Convert.ToDecimal(results["SMA16"].ToString()) &&
        //    //    Convert.ToDecimal(results["SMA10"].ToString()) > Convert.ToDecimal(results["SMA16"].ToString()))
        //    //{ longCnt = longCnt + 1; }

        //    //if (Convert.ToDecimal(results["EMA3"].ToString()) < Convert.ToDecimal(results["EMA10"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    //if (Convert.ToDecimal(results["EMA3"].ToString()) < Convert.ToDecimal(results["EMA16"].ToString()) &&
        //    //    Convert.ToDecimal(results["EMA10"].ToString()) < Convert.ToDecimal(results["EMA16"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA3"].ToString()) < Convert.ToDecimal(results["SMA10"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA3"].ToString()) < Convert.ToDecimal(results["SMA16"].ToString()) &&
        //    //    Convert.ToDecimal(results["SMA10"].ToString()) < Convert.ToDecimal(results["SMA16"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    ////}
        //    ////else if (use5138)
        //    ////{
        //    //    //// 5, 13, 8 STRATEGY
        //    //if (Convert.ToDecimal(results["EMA5"].ToString()) > Convert.ToDecimal(results["EMA8"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["EMA5"].ToString()) > Convert.ToDecimal(results["EMA13"].ToString()) &&
        //    //    Convert.ToDecimal(results["EMA8"].ToString()) > Convert.ToDecimal(results["EMA13"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["EMA5"].ToString()) < Convert.ToDecimal(results["EMA8"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    //if (Convert.ToDecimal(results["EMA5"].ToString()) < Convert.ToDecimal(results["EMA13"].ToString()) &&
        //    //    Convert.ToDecimal(results["EMA8"].ToString()) < Convert.ToDecimal(results["EMA13"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA5"].ToString()) > Convert.ToDecimal(results["SMA8"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA5"].ToString()) > Convert.ToDecimal(results["SMA13"].ToString()) &&
        //    //    Convert.ToDecimal(results["SMA8"].ToString()) > Convert.ToDecimal(results["SMA13"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA5"].ToString()) < Convert.ToDecimal(results["SMA8"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA5"].ToString()) < Convert.ToDecimal(results["SMA13"].ToString()) &&
        //    //    Convert.ToDecimal(results["SMA8"].ToString()) < Convert.ToDecimal(results["SMA13"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    ////}
        //    ////else
        //    ////{
        //    //    //// 9, 12, 26 STRATEGY
        //    //if (Convert.ToDecimal(results["EMA9"].ToString()) > Convert.ToDecimal(results["EMA12"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["EMA12"].ToString()) > Convert.ToDecimal(results["EMA26"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["EMA9"].ToString()) < Convert.ToDecimal(results["EMA12"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    //if (Convert.ToDecimal(results["EMA12"].ToString()) < Convert.ToDecimal(results["EMA26"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA9"].ToString()) > Convert.ToDecimal(results["SMA12"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA12"].ToString()) > Convert.ToDecimal(results["SMA26"].ToString()))
        //    //{ longCnt = longCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA9"].ToString()) < Convert.ToDecimal(results["SMA12"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }
        //    //if (Convert.ToDecimal(results["SMA12"].ToString()) < Convert.ToDecimal(results["SMA26"].ToString()))
        //    //{ shortCnt = shortCnt + 1; }

        //    try
        //    {
        //        if (decimal.Parse(results["MACD"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture) > decimal.Parse(results["MACDSignal"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture) && (/*decimal.Parse(results["MACDSignal"].ToString()) < 0 &&*/ decimal.Parse(results["MACDHistogram"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture) > 0))
        //        {
        //            longCnt = longCnt + 1;
        //        }
        //        if (decimal.Parse(results["MACD"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture) < decimal.Parse(results["MACDSignal"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture) && (/*decimal.Parse(results["MACDSignal"].ToString()) > 0 &&*/ decimal.Parse(results["MACDHistogram"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture) < 0))
        //        {
        //            shortCnt = shortCnt + 1;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex?.Message}");
        //    }

        //    //if (Convert.ToDecimal(results["Upper Bound Price Difference Average"].ToString()) > Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }
        //    //if (Convert.ToDecimal(results["Upper Bound Price Difference Average"].ToString()) < Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
        //    //if (Convert.ToDecimal(results["Lower Bound Price Difference Average"].ToString()) > Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }
        //    //if (Convert.ToDecimal(results["Lower Bound Price Difference Average"].ToString()) < Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
        //    if (Convert.ToDecimal(results["Forecast Bound Price Difference Average"].ToString()) > Convert.ToDecimal(results["lastPrice"].ToString())) { longCnt++; }
        //    if (Convert.ToDecimal(results["Forecast Bound Price Difference Average"].ToString()) < Convert.ToDecimal(results["lastPrice"].ToString())) { shortCnt++; }
        //    System.Threading.Thread.Sleep(100); //Sleep for 150 milliseconds to avoid throttling
        //}

        //public void CheckDowNasdaqandSandP(JsonObject vix, JsonObject sandp, JsonObject dow, JsonObject vxd, JsonObject nasdaq, JsonObject vxn, int? longMax, int? shortMax, ILogger _logger, ref int? SandPLong, ref int? SandPShort, ref int? DOWLong, ref int? DOWShort, ref int? NASDAQLong, ref int? NASDAQShort)
        //{
        //    int? shortCnt = 0;
        //    int? longCnt = 0;
        //    //int? longTotal = 0;
        //    //int? shortTotal = 0;

        //    //_logger.LogInformation($"Checking VIX and S&P. VIX at {vix["lastPrice"].ToString()}. S&P at {sandp["lastPrice"].ToString()}.");

        //    CalculatePoints(vix, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"VIX Score. Long score: {longCnt}. Short score {shortCnt}");

        //    //Opposite for VIX
        //    if (longCnt >= (longMax)) { SandPShort++; }
        //    if (shortCnt >= (shortMax)) { SandPLong++; }
        //    shortCnt = 0;
        //    longCnt = 0;

        //    CalculatePoints(sandp, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"S&P Score. Long score: {longCnt}. Short score {shortCnt}");

        //    if (longCnt >= (longMax)) { SandPLong++; }
        //    if (shortCnt >= (shortMax)) { SandPShort++; }
        //    shortCnt = 0;
        //    longCnt = 0;

        //    CalculatePoints(vxd, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"VXD Score. Long score: {longCnt}. Short score {shortCnt}");

        //    //Opposite for VXD
        //    if (longCnt >= (longMax)) { DOWShort++; }
        //    if (shortCnt >= (shortMax)) { DOWLong++; }
        //    shortCnt = 0;
        //    longCnt = 0;

        //    CalculatePoints(dow, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"DOW Score. Long score: {longCnt}. Short score {shortCnt}");

        //    if (longCnt >= (longMax)) { DOWLong++; }
        //    if (shortCnt >= (shortMax)) { DOWShort++; }
        //    shortCnt = 0;
        //    longCnt = 0;

        //    CalculatePoints(vxn, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"VXN Score. Long score: {longCnt}. Short score {shortCnt}");

        //    //Opposite for VXN
        //    if (longCnt >= (longMax)) { NASDAQShort++; }
        //    if (shortCnt >= (shortMax)) { NASDAQLong++; }
        //    shortCnt = 0;
        //    longCnt = 0;

        //    CalculatePoints(nasdaq, false, false, ref shortCnt, ref longCnt);

        //    _logger.LogInformation($"NASDAQ Score. Long score: {longCnt}. Short score {shortCnt}");

        //    if (longCnt >= (longMax)) { NASDAQLong++; }
        //    if (shortCnt >= (shortMax)) { NASDAQShort++; }

        //    //vixsandpLongCnt = (longTotal > 0) ? 1 : 0;
        //    //vixsandpShortCnt = (shortTotal > 0) ? 1 : 0;

        //    //_logger.LogInformation($"Score. Long total {vixsandpLongCnt}. Short total {vixsandpShortCnt}.");
        //}
        public bool IsNotHolidayorWeekend()
        {
            bool run = true;
            //DateTime runTime = easternTime;
            TimeSpan startTime = new TimeSpan(9, 25, 0);
            TimeSpan endTime = new TimeSpan(15, 55, 0);

            //New Year Day
            if (runTime.Month == 1 && runTime.Day == 1)
                run = false;

            //Martin Luthor King Day 3rd Monday of January.
            if (runTime.Month == 1 && runTime.DayOfWeek == DayOfWeek.Monday && runTime.Day > 14 && runTime.Day <= 21)
                run = false;

            //Presidents Day 3rd Monday of February.
            if (runTime.Month == 2 && runTime.DayOfWeek == DayOfWeek.Monday && runTime.Day > 14 && runTime.Day <= 21)
                run = false;

            //Good Friday Easter Sunday
            DateTime goodFriday = new DateTime();
            DateTime easterSunday = new DateTime();
            EasterSunday(runTime.Year, ref goodFriday, ref easterSunday);
            if (runTime.Month == easterSunday.Month && runTime.Day == easterSunday.Day /*&& runTime.Day <= (easterSunday.Day - 2))*/)
                run = false;
            if (runTime.Month == goodFriday.Month && runTime.Day == goodFriday.Day /*&& runTime.Day <= (goodFriday.Day - 2))*/)
                run = false;

            //Memorial Day
            if (runTime.Month == 5 && runTime.DayOfWeek == DayOfWeek.Monday && runTime.Day > 21 && runTime.Day <= 31)
                run = false;

            //Juneteenth National Independence Day
            if (runTime.Month == 6 && runTime.Day == 19)
                run = false;

            //Independence Day (Forth of july)
            if (runTime.Month == 7 && runTime.Day == 4)
                run = false;

            //Labor Day
            if (runTime.Month == 9 && runTime.DayOfWeek == DayOfWeek.Monday && runTime.Day > 1 && runTime.Day <= 7)
                run = false;

            //Thanksgiving Day
            if ((runTime.Month == 11 && runTime.DayOfWeek == DayOfWeek.Thursday && runTime.Day > 24 && runTime.Day <= 30) ||
                (runTime.Month == 11 && runTime.DayOfWeek == DayOfWeek.Friday && runTime.Day > 24 && runTime.Day <= 30 && runTime.Hour >= 1))
                run = false;

            //Christmas
            if (runTime.Month == 12 && runTime.Day == 25)
                run = false;

            //Christmas eve after 1pm
            if (runTime.Month == 12 && runTime.Day == 24 && runTime.TimeOfDay >= new TimeSpan(13, 00, 0))
                run = false;

            //After Hours, Saturday and Sunday
            if ((runTime.DayOfWeek == DayOfWeek.Saturday || runTime.DayOfWeek == DayOfWeek.Sunday) || (runTime.TimeOfDay >= endTime || runTime.TimeOfDay <= startTime))
                run = false;

            return run;
        }
        public void EasterSunday(int year, ref DateTime goodFriday, ref DateTime easterSunday)
        {
            int day = 0;
            int month = 0;

            int g = year % 19;
            int c = year / 100;
            int h = (c - (int)(c / 4) - (int)((8 * c + 13) / 25) + 19 * g + 15) % 30;
            int i = h - (int)(h / 28) * (1 - (int)(h / 28) * (int)(29 / (h + 1)) * (int)((21 - g) / 11));

            day = i - ((year + (int)(year / 4) + i + 2 - c + (int)(c / 4)) % 7) + 28;
            month = 3;

            if (day > 31)
            {
                month++;
                day -= 31;
            }

            DateTime ee = new DateTime(year, month, day);
            DateTime gf = ee.AddDays(-2);
            goodFriday = gf;
            easterSunday = ee;
        }
        public int CalculateBusinessDays(DateTime startDate, DateTime endDate)
        {
            var weekdays = 0;
            for (var currentDate = startDate; currentDate < endDate; currentDate = currentDate.AddDays(1))
            {
                if (IsWorkDay(currentDate))
                {
                    weekdays++;
                }
            }
            return weekdays;
        }
        private bool IsWorkDay(DateTime date)
        {
            return date.DayOfWeek is not DayOfWeek.Saturday
                and not DayOfWeek.Sunday;
        }
        public string CreateTradeObject(dynamic results, TradeDirection direction, int? qty)
        {//to be done
            string order = "{" +
                "\"orderType\" : \"MARKET\"," +
                "\"session\" : \"NORMAL\"," +
                //"\"price\": \"72\"," +
                "\"duration\" : \"DAY\"," +
                "\"orderStrategyType\" : \"SINGLE\"," +
                "\"orderLegCollection\" : [{" +
                "\"instruction\" : \"" + Enum.GetName(typeof(TradeDirection), direction).ToUpper() + "\"," +
                    "\"quantity\" : " + qty + "," +
                    "\"instrument\" :{" +
                    "\"symbol\" : \"" + results["Symbol"].ToString() + "\"," +
                        "\"assetType\" : \"EQUITY\"" +
                    "}" +
                "}" +
                "]" +
            "}";
            return order;
        }

        public bool SaveStockTracker(StockTracker2 stockTracker, Platform tradePlatform, TradeDirection direction, int? qty, bool IsReversed, dynamic results, string status, ILogger _logger)
        {
            bool isNew = false;
            if (stockTracker != null && (direction == TradeDirection.SELL || direction == TradeDirection.BUY_TO_COVER))
            {
                stockTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.SoldRsi = Convert.ToDecimal(results["RSI"].ToString());
                stockTracker.SoldEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                stockTracker.SoldEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                stockTracker.SoldEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                stockTracker.SoldEma3 = Convert.ToDecimal(results["EMA3"].ToString());
                stockTracker.SoldEma10 = Convert.ToDecimal(results["EMA10"].ToString());
                stockTracker.SoldEma16 = Convert.ToDecimal(results["EMA16"].ToString());
                stockTracker.SoldEma5 = Convert.ToDecimal(results["EMA5"].ToString());
                stockTracker.SoldEma8 = Convert.ToDecimal(results["EMA8"].ToString());
                stockTracker.SoldEma13 = Convert.ToDecimal(results["EMA13"].ToString());
                stockTracker.SoldSma9 = Convert.ToDecimal(results["SMA9"].ToString());
                stockTracker.SoldSma12 = Convert.ToDecimal(results["SMA12"].ToString());
                stockTracker.SoldSma26 = Convert.ToDecimal(results["SMA26"].ToString());
                stockTracker.SoldSma3 = Convert.ToDecimal(results["SMA3"].ToString());
                stockTracker.SoldSma10 = Convert.ToDecimal(results["SMA10"].ToString());
                stockTracker.SoldSma16 = Convert.ToDecimal(results["SMA16"].ToString());
                stockTracker.SoldSma5 = Convert.ToDecimal(results["SMA5"].ToString());
                stockTracker.SoldSma8 = Convert.ToDecimal(results["SMA8"].ToString());
                stockTracker.SoldSma13 = Convert.ToDecimal(results["SMA13"].ToString());
                stockTracker.SoldMacd = Decimal.Parse(results["MACD"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.SoldMacdSignal = Decimal.Parse(results["MACDSignal"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.SoldMacdHistogram = Decimal.Parse(results["MACDHistogram"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.DateSold = easternTime;
                stockTracker.DateUpdated = easternTime;
                stockTracker.Status = "FILLED";// status;
                _ctx?.StockTracker2?.Attach(stockTracker);
                _ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                if (stockTracker == null)
                {
                    stockTracker = new StockTracker2();
                    isNew = true;
                }
                //stockTracker = new StockTracker2();
                stockTracker.Symbol = results["Symbol"].ToString();
                stockTracker.Name = results["Name"].ToString();
                stockTracker.TradeDirection = (int)direction;
                stockTracker.TradePlatform = (int)tradePlatform;
                stockTracker.PricePurchased = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                stockTracker.IsReversed = IsReversed;
                stockTracker.Quantity = qty;
                stockTracker.PurchasedRsi = Convert.ToDecimal(results["RSI"].ToString());
                stockTracker.PurchasedEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                stockTracker.PurchasedEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                stockTracker.PurchasedEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                stockTracker.PurchasedEma3 = Convert.ToDecimal(results["EMA3"].ToString());
                stockTracker.PurchasedEma10 = Convert.ToDecimal(results["EMA10"].ToString());
                stockTracker.PurchasedEma16 = Convert.ToDecimal(results["EMA16"].ToString());
                stockTracker.PurchasedEma5 = Convert.ToDecimal(results["EMA5"].ToString());
                stockTracker.PurchasedEma8 = Convert.ToDecimal(results["EMA8"].ToString());
                stockTracker.PurchasedEma13 = Convert.ToDecimal(results["EMA13"].ToString());
                stockTracker.PurchasedSma9 = Convert.ToDecimal(results["SMA9"].ToString());
                stockTracker.PurchasedSma12 = Convert.ToDecimal(results["SMA12"].ToString());
                stockTracker.PurchasedSma26 = Convert.ToDecimal(results["SMA26"].ToString());
                stockTracker.PurchasedSma3 = Convert.ToDecimal(results["SMA3"].ToString());
                stockTracker.PurchasedSma10 = Convert.ToDecimal(results["SMA10"].ToString());
                stockTracker.PurchasedSma16 = Convert.ToDecimal(results["SMA16"].ToString());
                stockTracker.PurchasedSma5 = Convert.ToDecimal(results["SMA5"].ToString());
                stockTracker.PurchasedSma8 = Convert.ToDecimal(results["SMA8"].ToString());
                stockTracker.PurchasedSma13 = Convert.ToDecimal(results["SMA13"].ToString());
                stockTracker.PurchasedMacd = Decimal.Parse(results["MACD"].ToString(),NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.PurchasedMacdSignal = Decimal.Parse(results["MACDSignal"].ToString(),NumberStyles.Float,
            CultureInfo.InvariantCulture);
                stockTracker.PurchasedMacdHistogram = Decimal.Parse(results["MACDHistogram"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);                
                stockTracker.DatePurchased = easternTime;
                stockTracker.DateCreated = easternTime;
                stockTracker.LastUpdatedBy = 1;
                stockTracker.Status = status;
                stockTracker.IsReversed = false;
                _ctx.StockTracker2.Add(stockTracker);
                _ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                if (isNew)
                {
                    _ctx.StockTracker2.Add(stockTracker);
                    _ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                }
                else
                {
                    _ctx.StockTracker2.Attach(stockTracker);
                    _ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
            }
            try
            {
                _ctx.SaveChanges();
                _ctx.Entry(stockTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            catch (Exception ex)
            {
                Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
            }
            return true;
        }
        public void CheckSchwabOrderStatus(ref decimal totalAvailiableforLongTrade)
        {
            Log(_logger, LogLevel.Information, $"Check Schwab Order Status");
            //decimal totalAvailiableforLongTrade = 0m;
            StockTracker2? stockTracker = null;
            int qty = 0;
            decimal price = 0m;
            TradeDirection direction;
            JsonObject? quote = null;
            string sym = string.Empty;
            string desc = string.Empty;
            int authRetry = 0;
            //accountAuthRetry:
            //Get Account Info
            JsonElement account = new JsonElement();
            schwabServices.GetAccountInfo(ref account);
            Log(_logger, LogLevel.Information, $"Get account info for checking stock status and updating");

            JsonElement securitiesAccount = new JsonElement();
            JsonElement initialBalances = new JsonElement();
            try
            {
                securitiesAccount = account.GetProperty("securitiesAccount");
                //initialBalances = securitiesAccount.GetProperty("initialBalances");
                initialBalances = securitiesAccount.GetProperty("currentBalances");
                //for testing
                //dynamic crap = JsonSerializer.Deserialize<dynamic>(initialBalances);
            }
            catch (Exception ex)
            {
                {
                    Log(_logger, LogLevel.Error, $"Authenication from schwab failed {authRetry} Retries. Exiting");
                    return;
                }

            }

            decimal cash1 = Convert.ToDecimal(initialBalances.GetProperty("availableFundsNonMarginableTrade").ToString());
            decimal cash = Convert.ToDecimal(initialBalances.GetProperty("cashBalance").ToString());
            decimal margin = 0m;// Convert.ToDecimal(initialBalances.GetProperty("margin").ToString());
            totalAvailiableforLongTrade = Math.Round((cash + margin) * .99m, 2);
            //totalAvailiableforLongTrade = 0m;

            Log(_logger, LogLevel.Information, $"Cash Balance ${cash}. Margin Balance ${margin}. Total Availiable ${totalAvailiableforLongTrade}");

            foreach (var item in securitiesAccount.GetProperty("positions").EnumerateArray())
            {
                sym = item.GetProperty("instrument").GetProperty("symbol").ToString();
                //if (sym == "FDP" || sym == "MAN")
                //{
                //    //
                //}
                quote = schwabServices.GetQuoteAsync(sym, true, false).Result;
                try
                {
                    if (quote == null || !CheckMarketIndexes(quote))
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                    continue;
                }
                try
                {
                    desc = quote?["Name"]?.ToString();
                    if (desc == null)
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                    continue;
                }

                Log(_logger, LogLevel.Information, $"Checking {sym} - ({desc}) for status Update");

                //check to see if more than one entry for the same stock, then delete duplicates
                stockTracker = null;
                var stockTrackerList = _ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                        && x.TradePlatform == (int)Platform.CharlesSchwab && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                        x.Status.Trim() == "FILLED"
                        /*&& (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))*/
                        && x.Symbol == sym
                        );//.ToList();
                if (stockTrackerList.Count() > 1)
                {
                    stockTracker = stockTrackerList.FirstOrDefault();
                    var removeList = stockTrackerList.Where(x => x.Id != stockTracker.Id).ToList();
                    foreach (var rem in removeList)
                    {
                        try
                        {
                            _ctx.StockTracker2.Attach(rem);
                            _ctx.Entry(rem).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                            _ctx.SaveChanges();
                            _ctx.Entry(rem).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        catch (Exception ex)
                        {
                            Log(_logger, LogLevel.Error, $"Error in removing duplicate {sym} - ({desc}) for status Update {ex.StackTrace} - {ex.Message}");
                        }
                    }
                }
                else
                {
                    stockTracker = stockTrackerList.FirstOrDefault();
                }

                if (stockTracker == null)
                {
                    stockTracker = _ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                    && x.TradePlatform == (int)Platform.CharlesSchwab && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                    x.Status.Trim() == "{\"$id\":\"1\",\"status\":\"Created\"}"
                    && (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))
                    && x.Symbol == sym
                    ).FirstOrDefault();
                }

                if (stockTracker == null)
                {
                    stockTracker = new StockTracker2();
                    stockTracker.Symbol = sym;
                    stockTracker.Name = desc;
                    stockTracker.PurchasedRsi = Convert.ToDecimal(quote?["RSI"].ToString());
                    stockTracker.PurchasedEma9 = Convert.ToDecimal(quote?["EMA9"].ToString());
                    stockTracker.PurchasedEma12 = Convert.ToDecimal(quote?["EMA12"].ToString());
                    stockTracker.PurchasedEma26 = Convert.ToDecimal(quote?["EMA26"].ToString());
                    stockTracker.PurchasedEma3 = Convert.ToDecimal(quote?["EMA3"].ToString());
                    stockTracker.PurchasedEma10 = Convert.ToDecimal(quote?["EMA10"].ToString());
                    stockTracker.PurchasedEma16 = Convert.ToDecimal(quote?["EMA16"].ToString());
                    stockTracker.PurchasedEma5 = Convert.ToDecimal(quote?["EMA5"].ToString());
                    stockTracker.PurchasedEma8 = Convert.ToDecimal(quote?["EMA8"].ToString());
                    stockTracker.PurchasedEma13 = Convert.ToDecimal(quote?["EMA13"].ToString());
                    stockTracker.PurchasedSma9 = Convert.ToDecimal(quote?["SMA9"].ToString());
                    stockTracker.PurchasedSma12 = Convert.ToDecimal(quote?["SMA12"].ToString());
                    stockTracker.PurchasedSma26 = Convert.ToDecimal(quote?["SMA26"].ToString());
                    stockTracker.PurchasedSma3 = Convert.ToDecimal(quote?["SMA3"].ToString());
                    stockTracker.PurchasedSma10 = Convert.ToDecimal(quote?["SMA10"].ToString());
                    stockTracker.PurchasedSma16 = Convert.ToDecimal(quote?["SMA16"].ToString());
                    stockTracker.PurchasedSma5 = Convert.ToDecimal(quote?["SMA5"].ToString());
                    stockTracker.PurchasedSma8 = Convert.ToDecimal(quote?["SMA8"].ToString());
                    stockTracker.PurchasedSma13 = Convert.ToDecimal(quote?["SMA13"].ToString());
                    stockTracker.PurchasedMacd = Decimal.Parse(quote?["MACD"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                    stockTracker.PurchasedMacdSignal = Decimal.Parse(quote?["MACDSignal"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                    stockTracker.PurchasedMacdHistogram = Decimal.Parse(quote?["MACDHistogram"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                    stockTracker.DatePurchased = easternTime;
                    stockTracker.DateCreated = easternTime;
                    stockTracker.Status = "FILLED";
                    stockTracker.LastUpdatedBy = 1;
                    stockTracker.IsReversed = false;
                }
                var sp = stockTracker;
                price = Convert.ToDecimal(item.GetProperty("averagePrice").ToString());
                if (Convert.ToDecimal(item.GetProperty("shortQuantity").ToString()) > 0)
                {
                    qty = (int)Convert.ToDecimal(item.GetProperty("shortQuantity").ToString());
                    direction = TradeDirection.SELL_SHORT;
                }
                else
                {
                    qty = (int)Convert.ToDecimal(item.GetProperty("longQuantity").ToString());
                    direction = TradeDirection.BUY;
                }

                if (stockTracker == null)
                {
                    stockTracker = _ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                    && x.TradePlatform == (int)Platform.ETrade && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                    x.Status.Trim() == "Success"
                    && (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))
                    && x.Symbol == sym
                    ).FirstOrDefault();
                }
                try
                {
                    try
                    {
                        sp.LastPrice = Convert.ToDecimal(quote?["lastPrice"]?.ToString());
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                        continue;
                    }
                    sp.Status = "FILLED";
                    sp.TradeDirection = (int)direction;
                    sp.TradePlatform = (int)Platform.CharlesSchwab;
                    sp.PricePurchased = price;
                    sp.Quantity = qty;
                    sp.DateUpdated = easternTime;
                    _ctx.StockTracker2.Attach(sp);
                    _ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    _ctx.SaveChanges();
                    _ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                catch (Exception ex)
                {
                    try
                    {
                        _ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        _ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        _ctx.SaveChanges();
                        _ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    catch (Exception ex2)
                    {
                        Log(_logger, LogLevel.Error, $"Error in saving {sym} - ({desc}) for status Update {ex?.StackTrace} - {ex?.InnerException?.StackTrace} - {ex?.InnerException?.Message} - {ex?.Message}");
                    }
                }
            }

            var stockTrkList = _ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                        && x.TradePlatform == (int)Platform.CharlesSchwab &&
                        x.Status.Trim() == "FILLED"
                        );//.ToList();
            foreach (var stk in stockTrkList)
            {
                //if (stk.Symbol == "GNK")
                //{

                //}
                var pos = securitiesAccount.GetProperty("positions").EnumerateArray().Where(x => x.GetProperty("instrument").GetProperty("symbol").ToString() == stk.Symbol).FirstOrDefault();
                if (!(pos is JsonElement) || pos.ValueKind == JsonValueKind.Undefined)
                {
                    _ctx.StockTracker2.Attach(stk);
                    _ctx.Entry(stk).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                    try
                    {
                        _ctx.SaveChanges();
                        _ctx.Entry(stk).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Error, $"Error in updating sold status {stk.Symbol} - ({stk.Name}) for status Update {ex.StackTrace} - {ex.Message}");
                    }
                }
            }
        }
        public void CheckETradeOrderStatus(ref decimal totalAvailiableforLongTrade)
        {
            Log(_logger, LogLevel.Information, $"Check ETrade Order Status");
            StockTracker2? stockTracker = null;
            int qty = 0;
            decimal price = 0m;
            TradeDirection direction;
            JsonObject? quote = null;
            string sym = string.Empty;
            string desc = string.Empty;
            XElement account = null;
            XElement securitiesAccount = null;
            IEnumerable<XElement> positions;// = new List<XElement>();
            try
            {
                //account = services2.Portfolio();
                try
                {
                    account = services2.Account();
                    securitiesAccount = account.Element("Computed");
                }
                catch (Exception)
                {
                    try
                    {
                        account = services2.Account();
                        securitiesAccount = account.Element("Computed");
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Error, $"Error in accessig account info {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                        return;
                    }
                }

                Log(_logger, LogLevel.Information, $"CheckOrderStatus - Portfolio");
            }
            catch (Exception ex)
            {
                Log(_logger, LogLevel.Error, $"Error in CheckOrderStatus {ex.Message.ToString()} - {ex.StackTrace.ToString()}");
                return;
            }

            //var positions = securitiesAccount?.Element("AccountPortfolio")?.Elements("Position").ToList();
            //List<XElement> positions = services2?.Portfolio().Element("AccountPortfolio")?.Elements("Position").ToList();
            var portfolio = services2?.Portfolio();
            positions = portfolio?.Element("AccountPortfolio")?.Elements("Position");//.ToList();

            //decimal cash = Convert.ToDecimal(securitiesAccount.Element("cashBuyingPower").Value.ToString());
            decimal cash = Convert.ToDecimal(securitiesAccount.Element("cashAvailableForInvestment").Value.ToString());
            decimal margin = 0m;// Convert.ToDecimal(securitiesAccount.Element("marginBuyingPower").Value.ToString());
            //totalAvailiableforLongTrade = Math.Round((cash + margin), 2);
            totalAvailiableforLongTrade = Math.Round((cash + margin) * .99m, 2);
            //totalAvailiableforLongTrade = 200m;

            Log(_logger, LogLevel.Information, $"Cash Balance ${cash}. Margin Balance ${margin}. Total Availiable ${totalAvailiableforLongTrade}");

            if(positions != null)
            foreach (var position in positions)
            {
                sym = position?.Element("Product")?.Element("symbol")?.Value;
                //if (sym == "LION")
                //{
                //    //int a = 1;
                //}
                //if (sym == "MSGE")
                //{
                //    //int a = 1;
                //}
                quote = schwabServices.GetQuoteAsync(sym, true, false).Result;
                try
                {
                    if (quote == null || !CheckMarketIndexes(quote))
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                    continue;
                }
                try
                {
                    desc = quote?["Name"]?.ToString();
                    //continue;
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                    continue;
                }

                Log(_logger, LogLevel.Information, $"Checking {sym} - ({desc}) for status Update");

                //check to see if more than one entry for the same stock, then delete duplicates
                stockTracker = null;
                    var stockTrackerList = _ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                            && x.TradePlatform == (int)Platform.ETrade && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                            x.Status.Trim() == "FILLED"
                            /*&& (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))*/
                            && x.Symbol == sym
                            );//.ToList();
                if (stockTrackerList.Count() > 1)
                {
                    stockTracker = stockTrackerList.FirstOrDefault();
                    var removeList = stockTrackerList.Where(x => x.Id != stockTracker.Id).ToList();
                    foreach (var rem in removeList)
                    {
                            lock (_transactLock)
                            {
                                try
                                {
                                    _ctx.StockTracker2.Attach(rem);
                                    _ctx.Entry(rem).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                                    _ctx.SaveChanges();
                                    _ctx.Entry(rem).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                }
                                catch (Exception ex)
                                {
                                    Log(_logger, LogLevel.Error, $"Error in removing duplicate {sym} - ({desc}) for status Update {ex.StackTrace} - {ex.Message}");
                                }
                            }
                    }
                }
                else
                {
                    stockTracker = stockTrackerList.FirstOrDefault();
                }

                if (stockTracker == null)
                {
                    stockTracker = _ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
                    && x.TradePlatform == (int)Platform.ETrade && /*(x.Status != "Cancelled" && x.Status != "Filled")*/
                    x.Status.Trim() == "Success"
                    && (x.DateCreated >= easternTime.AddDays(-1) && x.DateCreated <= easternTime.AddDays(1))
                    && x.Symbol == sym
                    ).FirstOrDefault();
                }

                if (stockTracker == null)
                {
                    try
                    {
                        stockTracker = new StockTracker2();
                        stockTracker.Symbol = sym;
                        stockTracker.Name = desc;
                        stockTracker.PurchasedRsi = Convert.ToDecimal(quote?["RSI"].ToString());
                        stockTracker.PurchasedEma9 = Convert.ToDecimal(quote?["EMA9"].ToString());
                        stockTracker.PurchasedEma12 = Convert.ToDecimal(quote?["EMA12"].ToString());
                        stockTracker.PurchasedEma26 = Convert.ToDecimal(quote?["EMA26"].ToString());
                        stockTracker.PurchasedEma3 = Convert.ToDecimal(quote?["EMA3"].ToString());
                        stockTracker.PurchasedEma10 = Convert.ToDecimal(quote?["EMA10"].ToString());
                        stockTracker.PurchasedEma16 = Convert.ToDecimal(quote?["EMA16"].ToString());
                        stockTracker.PurchasedEma5 = Convert.ToDecimal(quote?["EMA5"].ToString());
                        stockTracker.PurchasedEma8 = Convert.ToDecimal(quote?["EMA8"].ToString());
                        stockTracker.PurchasedEma13 = Convert.ToDecimal(quote?["EMA13"].ToString());
                        stockTracker.PurchasedSma9 = Convert.ToDecimal(quote?["SMA9"].ToString());
                        stockTracker.PurchasedSma12 = Convert.ToDecimal(quote?["SMA12"].ToString());
                        stockTracker.PurchasedSma26 = Convert.ToDecimal(quote?["SMA26"].ToString());
                        stockTracker.PurchasedSma3 = Convert.ToDecimal(quote?["SMA3"].ToString());
                        stockTracker.PurchasedSma10 = Convert.ToDecimal(quote?["SMA10"].ToString());
                        stockTracker.PurchasedSma16 = Convert.ToDecimal(quote?["SMA16"].ToString());
                        stockTracker.PurchasedSma5 = Convert.ToDecimal(quote?["SMA5"].ToString());
                        stockTracker.PurchasedSma8 = Convert.ToDecimal(quote?["SMA8"].ToString());
                        stockTracker.PurchasedSma13 = Convert.ToDecimal(quote?["SMA13"].ToString());
                        stockTracker.PurchasedMacd = Decimal.Parse(quote?["MACD"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                        stockTracker.PurchasedMacdSignal = Decimal.Parse(quote?["MACDSignal"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                        stockTracker.PurchasedMacdHistogram = Decimal.Parse(quote?["MACDHistogram"].ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture);
                        stockTracker.DatePurchased = easternTime;
                        stockTracker.DateCreated = easternTime;
                        stockTracker.Status = "FILLED";
                        stockTracker.LastUpdatedBy = 1;
                        stockTracker.IsReversed = false;
                    }
                    catch (Exception)
                    {
                        Log(_logger, LogLevel.Error, $"Cannot add stock info to stock tracker for {sym} for Etrade");
                        continue;
                    }

                }
                var sp = stockTracker;
                price = Convert.ToDecimal(position?.Element("pricePaid")?.Value);
                qty = (int)Convert.ToInt32(position?.Element("quantity").Value);
                direction = (qty > 0) ? TradeDirection.BUY : TradeDirection.SELL_SHORT;
                try
                {
                    try
                    {
                        sp.LastPrice = Convert.ToDecimal(quote?["lastPrice"]?.ToString());
                    }
                    catch (Exception ex)
                    {
                        Log(_logger, LogLevel.Information, $"Error for quote on {sym} - {ex.Message}");
                        continue;
                    }

                    sp.Status = "FILLED";
                    sp.TradeDirection = (int)direction;
                    sp.TradePlatform = (int)Platform.ETrade;
                    sp.PricePurchased = price;
                    sp.Quantity = qty;
                    sp.DateUpdated = easternTime;
                        lock (_transactLock)
                        {
                            _ctx.StockTracker2.Attach(sp);
                            _ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            _ctx.SaveChanges();
                            _ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                }
                catch (Exception ex)
                {
                        lock (_transactLock)
                        {
                            try
                            {
                                _ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                                _ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                                _ctx.SaveChanges();
                                _ctx.Entry(sp).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                            }
                            catch (Exception ex2)
                            {
                                Log(_logger, LogLevel.Error, $"Error in saving {sym} - ({desc}) for status Update {ex?.StackTrace} - {ex?.InnerException?.StackTrace} - {ex?.InnerException?.Message} - {ex?.Message}");
                            }
                        }
                }
            }
            var stockTrkList = _ctx.StockTracker2.Where(x => x.PriceSold == null && x.DateSold == null
            && x.TradePlatform == (int)Platform.ETrade &&
            x.Status.Trim() == "FILLED"
            );//.ToList();
            foreach (var stk in stockTrkList)
            {
                //if (stk.Symbol == "GNK")
                //{

                //}
                var pos = positions.Where(x => x.Element("Product")?.Element("symbol")?.Value.ToString() == stk.Symbol).FirstOrDefault();
                if (pos is null)
                {
                    lock (_transactLock)
                    {
                        _ctx.StockTracker2.Attach(stk);
                        _ctx.Entry(stk).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                        try
                        {
                            _ctx.SaveChanges();
                            _ctx.Entry(stk).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        catch (Exception ex)
                        {
                            Log(_logger, LogLevel.Error, $"Error in updating sold status {stk.Symbol} - ({stk.Name}) for status Update {ex.StackTrace} - {ex.Message}");
                        }
                    }
                }
            }
        }
        private static void SetTimeZone()
        {
            // Get the TimeZoneInfo object for Eastern Standard Time
            // The identifier "Eastern Standard Time" is commonly used on Windows systems.
            // On Linux/macOS, you might need "America/New_York".
            TimeZoneInfo? easternTimeZone;
            try
            {
                easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback for non-Windows systems or if the ID is different
                easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            }
            // Get the current UTC time
            DateTime utcNow = DateTime.UtcNow;

            // Convert the UTC time to Eastern Time
            easternTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, easternTimeZone);
            runTime = easternTime;
        }
        //public void UnloadAgingStocks()
        //{
        //    var agingStock = _ctx?.StockTracker2?.ToList()?.Where(x => x.PriceSold == null && x.DateSold == null && x.Status == "FILLED"
        //    && Math.Abs((x.DateCreated - easternTime).Value.TotalDays) > 5).ToList();
        //    foreach (var stock in agingStock)
        //    {
        //        var results = services.GetQuote(stock.Symbol);
        //        Platform tradePlatform = (Platform)stock.TradePlatform;
        //        TradeDirection currentTradeDirection = (TradeDirection)Enum.Parse(typeof(TradeDirection), stock.TradeDirection.ToString(), true);
        //        TradeDirection tradeDirection = (currentTradeDirection == TradeDirection.BUY) ? TradeDirection.SELL : TradeDirection.BUY_TO_COVER;
        //        switch (tradePlatform)
        //        {
        //            case Platform.CharlesSchwab:
        //                BuySellStock(stock, results, tradeDirection, stock.Quantity, true, services);
        //                break;
        //            case Platform.ETrade:
        //                BuySellStock(stock, results, tradeDirection, stock.Quantity, true, services2);
        //                break;
        //        }
        //    }
        //}
        public void CalculatePurchaseAmount(decimal? availiabletoTrade, ref List<BuySellCommodity> BuySellStockList, RulesEngine settings, Platform platform)
        {
            int stockCnt = 0;
            var stockTracker = _ctx?.StockTracker2?.AsNoTracking().Where(x => x.PriceSold == null && x.DateSold == null && x.Status.Trim() == "Filled" && x.TradePlatform == (int)platform);//.ToList();

            if (stockTracker?.Count() >= (settings?.MaxNumberofStockstoBuy / 2))
                return;
            
            //decimal? perStockSet = (BuySellStockList.Count() < (settings.MaxNumberofStockstoBuy - BuySellStockList.Count())) ? availiabletoTrade / BuySellStockList.Count() : availiabletoTrade / (settings.MaxNumberofStockstoBuy - stockTracker.Count());//(availiabletoTrade / BuySellStockList.Count) > (availiabletoTrade * (settings.MaxPercentofAvailBalancePerOrder / 100)) ? (availiabletoTrade * (settings.MaxPercentofAvailBalancePerOrder / 100)) : (availiabletoTrade / BuySellStockList.Count);
            //if ((int)(settings.MaxNumberofStockstoBuy / 2) - stockTracker.Count() < 1)
            //    return;
            //decimal? perStockSet = availiabletoTrade / Math.Abs((int)(settings.MaxNumberofStockstoBuy / 2) - stockTracker.Count());
            //var availiableStockForPlatform = (settings.MaxNumberofStockstoBuy / 2) - stockTracker.Count();
            //var pricePerStock = availiabletoTrade / availiableStockForPlatform;
            var stockList = new List<BuySellCommodity>(BuySellStockList);
            foreach (var item in stockList)
            {
                if ((stockTracker.Count() + stockCnt) >= (settings.MaxNumberofStockstoBuy / 2) || availiabletoTrade <= 0)
                    break;
                //var result = item.Key;
                //var direction = item.Value;
                var result = item.Results;
                var direction = item.Direction;
                var lastPrice = (decimal?)Convert.ToDecimal(result?["lastPrice"]?.ToString());
                //var qty = Convert.ToInt32(perStockSet / lastPrice);
                //var qty = Math.Abs((Convert.ToInt32(perStockSet / lastPrice) > settings.MaxNumberofSharesPerStock) ? (int)settings.MaxNumberofSharesPerStock : Convert.ToInt32(perStockSet / lastPrice));
                //var qty = (Convert.ToInt32(pricePerStock / lastPrice) < settings.MaxNumberofSharesPerStock) ? Convert.ToInt32(pricePerStock / lastPrice) : settings.MaxNumberofSharesPerStock;
                var qty = (Convert.ToInt32(availiabletoTrade / lastPrice) > 2) ? 2 : Convert.ToInt32(availiabletoTrade / lastPrice);
                
                if (qty >= 2 && lastPrice >= 5m)
                {
                    //BuySellStock(null, result, direction, (int)qty, false, services);
                    lock (_transactLock)
                    {
                        switch (platform)
                        {
                            case Platform.CharlesSchwab:
                                //Testing Point
                                BuySellStock(null, result, direction, (int)qty, true, schwabServices);
                                break;
                            case Platform.ETrade:
                                //Testing Point
                                BuySellStock(null, result, direction, (int)qty, true, services2);
                                break;
                        }
                    }
                    availiabletoTrade = availiabletoTrade - (qty * lastPrice);
                    stockCnt++;
                    BuySellStockList.Remove(item);
                }
            }
        }
        public bool BuySellStock(StockTracker2 stocktracker, dynamic order, TradeDirection direction, int? qty, bool IsReversed, SchwabServices service)
        {
            //return true;
            string jsonOrder = CreateTradeObject(order, direction, qty);
            dynamic results = service.SetOrder(jsonOrder);
            //if direction is buy or sell short, stocktracker will be null.
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                string output = System.Text.Json.JsonSerializer.Serialize(results, options);
                if (output.Contains("message"))
                {
                    Log(_logger, LogLevel.Error, $"stock trade failed {order["Symbol"].ToString()} {output}");
                }
                else
                {
                    Log(_logger, LogLevel.Information, $"stock trade results {output}");
                    SaveStockTracker(stocktracker, Platform.CharlesSchwab, direction, qty, IsReversed, order, output, _logger/*, ref _ctx*/);
                }
            }
            catch (Exception ex)
            {
                Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
            }
            return true;
        }
        public bool BuySellStock(StockTracker2 stocktracker, dynamic order, TradeDirection direction, int? qty, bool IsReversed, ETradeService service)
        {
            //return true;
            qty = Math.Abs((int)qty);
            dynamic results = service.SetOrder(order["Symbol"].ToString(), "MARKET",string.Empty, qty, direction);
            //if direction is buy or sell short, stocktracker will be null.
            //string output = System.Text.Json.JsonSerializer.Serialize(results);
            //((XElement)results).Element("code")?.Value?.ToString();
            lock (_transactLock)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.Preserve
                    };
                    string output = System.Text.Json.JsonSerializer.Serialize(results, options);
                    if (output.Contains("errors"))
                    {
                        Log(_logger, LogLevel.Error, $"Stock trade failed {order["Symbol"].ToString()} {output}");
                        //Try limit order
                        string limitPrice = order["lastPrice"].ToString();
                        results = service.SetOrder(order["Symbol"].ToString(), "LIMIT", limitPrice, qty, direction);
                        if (output.Contains("errors"))
                        {
                            Log(_logger, LogLevel.Error, $"Stock trade failed {order["Symbol"].ToString()} {output}");
                            return false;
                        }
                    }
                    Log(_logger, LogLevel.Information, $"Stock trade results {order["Symbol"].ToString()} Success");
                    //SaveStockTracker(stocktracker, direction, qty, order, "Success");
                    SaveStockTracker(stocktracker, Platform.ETrade, direction, qty, IsReversed, order, "Success", _logger/*, ref _ctx*/);
                    return true;
                }
                catch (Exception ex)
                {
                    Log(_logger, LogLevel.Error, $"Error in buying stock - {ex.Message}");
                }
            }
            return true;
        }
        public bool BuySellCrypto(CryptoTracker2 cryptoTracker, dynamic results, TradeDirection direction, decimal amount, CoinBaseService coinBaseService, List<PublicProduct> publicProducts)
        {
            string symbol = (results["Symbol"].ToString().Substring((results["Symbol"].ToString().Length - 1), 1) == "C") ? results["Symbol"].ToString().Substring(0, (results["Symbol"].ToString().Length - 1)) : results["Symbol"].ToString();
            PublicProduct? product = publicProducts.Where(x => x.DisplayName == symbol).FirstOrDefault();
            string precision = product.BaseIncrement.Substring((product.BaseIncrement.IndexOf(".") + 1), (product.BaseIncrement.Length - (product.BaseIncrement.IndexOf(".") + 1)));
            string[] currency = product.DisplayName.Split('-');
            var account = coinBaseService.GetAccounts().Where(x => x.Currency == currency[0] && Convert.ToDecimal(x.AvailableBalance.Value) > 0m).FirstOrDefault();
            decimal minSubtract = 0m;
            decimal baseSize = 0m;

            Coinbase.AdvancedTrade.Models.Order output = null;
            Coinbase.AdvancedTrade.Enums.OrderSide coinbaseDirection = (direction == TradeDirection.BUY || direction == TradeDirection.BUY_TO_COVER) ? Coinbase.AdvancedTrade.Enums.OrderSide.BUY : Coinbase.AdvancedTrade.Enums.OrderSide.SELL;
            lock (_transactLock)
            {
                switch (direction)
                {
                    case TradeDirection.BUY:

                        var perItem = amount.ToString();
                        var amountSize = Convert.ToDecimal(perItem.Substring(0, ((perItem.Length - perItem.IndexOf(".")) - 1)));
                        output = coinBaseService.SetOrder(product.DisplayName, coinbaseDirection, amountSize);
                        break;
                    case TradeDirection.SELL:

                        switch (precision.Length)
                        {
                            case 1:
                                minSubtract = .1m;
                                break;
                            case 2:
                                minSubtract = .01m;
                                break;
                            case 3:
                                minSubtract = .001m;
                                break;
                            case 4:
                                minSubtract = .0001m;
                                break;
                            case 5:
                                minSubtract = .00001m;
                                break;
                            case 6:
                                minSubtract = .000001m;
                                break;
                            case 7:
                                minSubtract = .0000001m;
                                break;
                            case 8 or 9:
                                minSubtract = .00000001m;
                                break;
                                //case 9:
                                //    minSubtract = .000000001m;
                                //    break;
                        }
                        baseSize = decimal.Round(Convert.ToDecimal(account.AvailableBalance.Value), precision.Length) - minSubtract;

                        output = coinBaseService.SetOrder(product.DisplayName, coinbaseDirection, baseSize);
                        if (output == null)
                        {
                            baseSize = (int)Convert.ToDecimal(account.AvailableBalance.Value);
                            output = coinBaseService.SetOrder(product.DisplayName, coinbaseDirection, baseSize);
                        }

                        break;
                }

                if (output != null)
                {
                    Helper.Log(_logger, LogLevel.Information, $"Crypto trade results for {symbol}, {output.Status}");
                }
                else
                {
                    Helper.Log(_logger, LogLevel.Error, $"Crypto trade results order {symbol} failed.");
                }

                if (output != null && (output.Status.ToLower() == "filled" || output.Status.ToLower() == "open" || output.Status.ToLower() == "pending"))
                    SaveCryptoTracker(cryptoTracker, direction, amount, results, output, output.Status);
            }
            System.Threading.Thread.Sleep(250);
            return true;
        }
        bool SaveCryptoTracker(CryptoTracker2 cryptoTracker, TradeDirection direction, decimal currentBalance, dynamic results, Coinbase.AdvancedTrade.Models.Order order, string buyStatus)
        {
            bool isNew = false;
            lock (_transactLock)
            {
                if (cryptoTracker != null && (direction == TradeDirection.SELL || direction == TradeDirection.BUY_TO_COVER))
                {
                    cryptoTracker.SoldRsi = Convert.ToDecimal(results["RSI"].ToString());
                    cryptoTracker.SoldEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                    cryptoTracker.SoldEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                    cryptoTracker.SoldEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                    cryptoTracker.SoldEma3 = Convert.ToDecimal(results["EMA3"].ToString());
                    cryptoTracker.SoldEma10 = Convert.ToDecimal(results["EMA10"].ToString());
                    cryptoTracker.SoldEma16 = Convert.ToDecimal(results["EMA16"].ToString());
                    cryptoTracker.SoldEma5 = Convert.ToDecimal(results["EMA5"].ToString());
                    cryptoTracker.SoldEma8 = Convert.ToDecimal(results["EMA8"].ToString());
                    cryptoTracker.SoldEma13 = Convert.ToDecimal(results["EMA13"].ToString());
                    cryptoTracker.SoldSma9 = Convert.ToDecimal(results["SMA9"].ToString());
                    cryptoTracker.SoldSma12 = Convert.ToDecimal(results["SMA12"].ToString());
                    cryptoTracker.SoldSma26 = Convert.ToDecimal(results["SMA26"].ToString());
                    cryptoTracker.SoldSma3 = Convert.ToDecimal(results["SMA3"].ToString());
                    cryptoTracker.SoldSma10 = Convert.ToDecimal(results["SMA10"].ToString());
                    cryptoTracker.SoldSma16 = Convert.ToDecimal(results["SMA16"].ToString());
                    cryptoTracker.SoldSma5 = Convert.ToDecimal(results["SMA5"].ToString());
                    cryptoTracker.SoldSma8 = Convert.ToDecimal(results["SMA8"].ToString());
                    cryptoTracker.SoldSma13 = Convert.ToDecimal(results["SMA13"].ToString());
                    cryptoTracker.SoldMacd = Decimal.Parse(results["MACD"].ToString(), NumberStyles.Float,
                CultureInfo.InvariantCulture);
                    cryptoTracker.SoldMacdSignal = Decimal.Parse(results["MACDSignal"].ToString(), NumberStyles.Float,
                CultureInfo.InvariantCulture);
                    cryptoTracker.SoldMacdHistogram = Decimal.Parse(results["MACDHistogram"].ToString(), NumberStyles.Float,
                CultureInfo.InvariantCulture);
                    cryptoTracker.CurrentBalance = /*(direction == TradeDirection.SELL) ? cryptoTracker.CurrentBalance :*/ currentBalance;
                    cryptoTracker.SellOrderId = (order != null) ? Guid.Parse(order?.OrderId) : null;
                    cryptoTracker.SellStatus = (order != null) ? order.Status : null;
                    cryptoTracker.DateSold = Helper.easternTime;
                    cryptoTracker.DateUpdated = Helper.easternTime;
                    cryptoTracker.PriceSold = Convert.ToDecimal(results["lastPrice"].ToString());
                    cryptoTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                    _ctx?.CryptoTracker2.Attach(cryptoTracker);
                    _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
                else
                {
                    if (cryptoTracker == null)
                    {
                        cryptoTracker = new CryptoTracker2();
                        isNew = true;
                    }
                    cryptoTracker.Coin = results["Symbol"].ToString();
                    cryptoTracker.BuyOrderId = (order != null) ? Guid.Parse((order?.OrderId ?? null)) : null;
                    //cryptoTracker.BuyStatus = (order != null) ? order.Status: buyStatus;
                    cryptoTracker.Name = results["Name"].ToString();
                    cryptoTracker.TradeDirection = (int)direction;
                    cryptoTracker.PricePurchased = Convert.ToDecimal(results["lastPrice"].ToString());
                    cryptoTracker.Amount = currentBalance;
                    cryptoTracker.PurchasedRsi = Convert.ToDecimal(results["RSI"].ToString());
                    cryptoTracker.PurchasedEma9 = Convert.ToDecimal(results["EMA9"].ToString());
                    cryptoTracker.PurchasedEma12 = Convert.ToDecimal(results["EMA12"].ToString());
                    cryptoTracker.PurchasedEma26 = Convert.ToDecimal(results["EMA26"].ToString());
                    cryptoTracker.PurchasedEma3 = Convert.ToDecimal(results["EMA3"].ToString());
                    cryptoTracker.PurchasedEma10 = Convert.ToDecimal(results["EMA10"].ToString());
                    cryptoTracker.PurchasedEma16 = Convert.ToDecimal(results["EMA16"].ToString());
                    cryptoTracker.PurchasedEma5 = Convert.ToDecimal(results["EMA5"].ToString());
                    cryptoTracker.PurchasedEma8 = Convert.ToDecimal(results["EMA8"].ToString());
                    cryptoTracker.PurchasedEma13 = Convert.ToDecimal(results["EMA13"].ToString());
                    cryptoTracker.PurchasedSma9 = Convert.ToDecimal(results["SMA9"].ToString());
                    cryptoTracker.PurchasedSma12 = Convert.ToDecimal(results["SMA12"].ToString());
                    cryptoTracker.PurchasedSma26 = Convert.ToDecimal(results["SMA26"].ToString());
                    cryptoTracker.PurchasedSma3 = Convert.ToDecimal(results["SMA3"].ToString());
                    cryptoTracker.PurchasedSma10 = Convert.ToDecimal(results["SMA10"].ToString());
                    cryptoTracker.PurchasedSma16 = Convert.ToDecimal(results["SMA16"].ToString());
                    cryptoTracker.PurchasedSma5 = Convert.ToDecimal(results["SMA5"].ToString());
                    cryptoTracker.PurchasedSma8 = Convert.ToDecimal(results["SMA8"].ToString());
                    cryptoTracker.PurchasedSma13 = Convert.ToDecimal(results["SMA13"].ToString());
                    cryptoTracker.PurchasedMacd = Decimal.Parse(results["MACD"].ToString(), NumberStyles.Float,
                CultureInfo.InvariantCulture);
                    cryptoTracker.PurchasedMacdSignal = Decimal.Parse(results["MACDSignal"].ToString(), NumberStyles.Float,
                CultureInfo.InvariantCulture);
                    cryptoTracker.PurchasedMacdHistogram = Decimal.Parse(results["MACDHistogram"].ToString(), NumberStyles.Float,
                CultureInfo.InvariantCulture);
                    cryptoTracker.LastPrice = Convert.ToDecimal(results["lastPrice"].ToString());
                    cryptoTracker.DatePurchased = Helper.easternTime;
                    cryptoTracker.DateCreated = Helper.easternTime;
                    cryptoTracker.LastUpdatedBy = 1;
                    cryptoTracker.BuyStatus = (order != null) ? order.Status : (!string.IsNullOrEmpty(buyStatus)) ? buyStatus : null;
                    if (isNew)
                    {
                        _ctx.CryptoTracker2.Add(cryptoTracker);
                        _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    }
                    else
                    {
                        _ctx.CryptoTracker2.Attach(cryptoTracker);
                        _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    }
                }
                try
                {
                    _ctx.SaveChanges();
                    _ctx.Entry(cryptoTracker).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                catch (Exception ex)
                {
                    Helper.Log(_logger, LogLevel.Error, $"Source: {ex.Source} stack trace: {ex.StackTrace} inner exception {ex?.InnerException?.Message ?? ""} error: {ex.Message}");
                }
            }
            return true;
        }
        //void QueueStocktoBuy(dynamic order, Platform platform, TradeDirection direction, int qty)
        //{
        //    SaveStockTracker(null, platform, direction, qty, false, order, "QUEUED" , _logger);
        //}
        //public void CheckforQueuedStock(RulesEngine settings, Platform platform)
        //{
        //    int? longCnt = 0;
        //    int? shortCnt = 0;
        //    var queuedStock = _ctx.StockTracker2.Where(x => x.Status == "QUEUED" && x.TradePlatform == (int)platform).ToList();
        //    foreach (var stock in queuedStock)
        //    {
        //        dynamic? results = services.GetQuote(stock.Symbol);
        //        CalculatePoints(results, /*false, true,*/ ref shortCnt, ref longCnt);
        //        if (longCnt >= settings.StockLongCount || (shortCnt >= settings.StockShortCount && (bool)settings.ShortEnabled))
        //        {
        //            switch(platform)
        //            {
        //                case Platform.CharlesSchwab:
        //                    BuySellStock(stock, results, (TradeDirection)stock.TradeDirection, stock.Quantity, false, services/*, true*/);
        //                    break;
        //                case Platform.ETrade:
        //                    BuySellStock(stock, results, (TradeDirection)stock.TradeDirection, stock.Quantity, false, services2/*, true*/);
        //                    break;
        //            }
        //            //BuySellStock(stock, results, (TradeDirection)stock.TradeDirection, stock.Quantity, false, services/*, true*/);
        //        }
        //        else
        //        {
        //            SaveStockTracker(stock, platform, (TradeDirection)stock.TradeDirection, stock.Quantity, false, results, "CANCEL", _logger);
        //        }
        //    }
        //}
        //public bool ReverseStock(StockTracker stock, JsonObject? results, dynamic services)
        //{
        //    //reverse stock
        //    Log(_logger, LogLevel.Information, $"Attempting Reversing stock {stock.Symbol} - ({stock.Name})");
        //    TradeDirection tradeDirection = (TradeDirection)stock.TradeDirection;
        //    int? shortCnt = 0; int? longCnt = 0;
        //    Log(_logger, LogLevel.Information, $"Checking {stock.Symbol} - ({stock.Name}) Points");
        //    CalculatePoints(results, false, false, ref shortCnt, ref longCnt);
        //    if ((longCnt) >= (rulesEngine.StockLongCount - 1) && tradeDirection == TradeDirection.SELL_SHORT)
        //    {
        //        BuySellStock(null, results, TradeDirection.BUY, stock.Quantity, true, services);
        //        Log(_logger, LogLevel.Information, $"Reversing stock {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}.");
        //    }
        //    else
        //    if ((shortCnt >= rulesEngine.StockShortCount - 1) && tradeDirection == TradeDirection.BUY)
        //    {
        //        BuySellStock(null, results, TradeDirection.SELL_SHORT, stock.Quantity, true, services);
        //        Log(_logger, LogLevel.Information, $"Reversing stock {stock.Symbol} - ({stock.Name}). Long score: {longCnt}. Short score {shortCnt}.");
        //    }
        //    else
        //    {
        //        Log(_logger, LogLevel.Information, $"Not Reversing stock {stock.Symbol} - ({stock.Name}) as it does not meet the points criteria. Long score: {longCnt}. Short score {shortCnt}.");
        //    }
        //    return true;
        //}
        public static void Log(ILogger? log, LogLevel logLevel, string message)
        {
            int attachAttemptCount = 0;
            log.Log(logLevel, message);
            log.Log(LogLevel.Information, Environment.NewLine);
            if (logLevel == LogLevel.Error)
            {
            ExceptionFiling:
                lock (_transactLock)
                {
                    try
                    {
                        var exception = new Exceptions { Message = message, DateCreated = easternTime, LastUpdateBy = 1 };

                        //using (var scope = _serviceProvider.CreateScope())
                        //{
                        //    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
                        //    var context = ctxFactory.CreateDbContext();
                        _ctx.Exceptions.Add(exception);
                        _ctx.Entry(exception).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        int results = _ctx.SaveChangesAsync(token).Result;
                        _ctx.Entry(exception).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

                        //}
                        System.Threading.Thread.Sleep(250);
                    }
                    catch (Exception ex)
                    {
                        attachAttemptCount++;
                        if (attachAttemptCount < 3)
                        {
                            System.Threading.Thread.Sleep(500);
                            goto ExceptionFiling;
                        }
                        log.Log(LogLevel.Error, $"Error in DB logging: {ex.Message}{Environment.NewLine}");
                    }
                }
            }
        }
        ~Helper()
        {
            //_ctx?.Dispose();
            //_ctx = null;
            schwabServices = null;
            services2 = null;
        }
    }
}
