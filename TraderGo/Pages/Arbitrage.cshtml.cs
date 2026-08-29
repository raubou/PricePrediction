using Coinbase.AdvancedTrade.Models.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TMD.PricePrediction;
using TMD.Services;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Services;
using System.Text.Json.Nodes;

namespace TraderGo.Pages
{
    public class ArbitrageModel : PageModel
    {
        [BindProperty] public JsonObject[]? movers { get; set; }

        private readonly ILogger<IndexModel>? _logger;
        private readonly SchwabSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        SchwabServices SchwabService = null;
        Helper? _helper = null;
        public ArbitrageModel(ILogger<IndexModel> logger, IOptions<SchwabSettings> settings, IHttpClientFactory factory, IDbContextFactory<TmdContext> contextFactory)
        {
            _logger = logger;
            _settings = settings.Value;
            _factory = factory;
            SchwabService = new SchwabServices(_logger, _settings, _factory, contextFactory);
            _helper = new Helper(_logger, null, null, null, contextFactory);
        }
        public void OnGet()
        {
            DateTime startDate = DateTime.Now.AddDays(-45);
            DateTime endDate = DateTime.Now;
            float percentHigher = 0f;
            float percentLower = 0f;
            int higherCount = 0;
            int lowerCount = 0;
            int? shortCnt = 0;
            int? longCnt = 0;

            movers = SchwabService.GetMovers();
            foreach (var mover in movers)
            {
                if (mover == null) {  continue; }
                higherCount = 0;
                lowerCount = 0;
                shortCnt = 0;
                longCnt = 0;
                var results = SchwabService.GetPriceHistory(mover["Symbol"].ToString(), "month", "1", "daily", 1, startDate, endDate).Result;
                var ser = JsonSerializer.Serialize(results.GetProperty("candles"));
                var candles90 = JsonSerializer.Deserialize<List<TMDInvestment.Models.CandleItems>>(ser).OrderByDescending(x => x.datetime).ToList();
                var candleFirst = candles90.FirstOrDefault();
                mover.Add("lastopChange", candleFirst?.opChange ?? 0f);
                mover.Add("lastopChangePercent", candleFirst?.opPercent ?? 0f);
                mover.Add("lastOpen", candleFirst?.open ?? 0f);
                mover.Add("lastClose", candleFirst?.close ?? 0f);
                foreach (var candle in candles90)
                {
                    if (candle.opChange > 0)
                    {
                        higherCount++;

                    }
                    else if (candle.opChange < 0)
                    {
                        lowerCount++;
                    }
                }
                percentHigher = ((float)higherCount / (float)candles90.Count) * 100f;
                percentLower = ((float)lowerCount / (float)candles90.Count) * 100f;
                mover.Add("PercentHigher", percentHigher);
                mover.Add("PercentLower", percentLower);
                _helper?.CalculatePoints(mover, ref shortCnt, ref longCnt, .08m, true);
                mover.Add("shortCnt", shortCnt);
                mover.Add("longCnt", longCnt);
            }
        }
    }
}
