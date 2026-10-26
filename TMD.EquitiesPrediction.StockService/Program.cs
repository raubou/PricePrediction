using Microsoft.EntityFrameworkCore;
using TMD.EquitiesPrediction.StockService;
using TMD.PricePrediction;
using TMD.PricePrediction.Classes;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using static Org.BouncyCastle.Math.EC.ECCurve;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SchwabSettings>(builder.Configuration.GetSection("SchwabAPI"));
builder.Services.Configure<ETradeSettings>(builder.Configuration.GetSection("ETradeAPI"));
builder.Services.Configure<yfapiSettings>(builder.Configuration.GetSection("yfapi"));
builder.Services.AddHttpClient("TMD")
        .UseSocketsHttpHandler((handler, _) =>
            handler.PooledConnectionLifetime = TimeSpan.FromMinutes(7)) // Recreate connection every 2 minutes
        .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
builder.Services.AddDbContextFactory<TmdContext>(options =>
    options.UseSqlServer(builder.Configuration.GetSection("ConnectionStrings").GetSection("connection").Value));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
