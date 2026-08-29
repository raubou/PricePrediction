using Microsoft.EntityFrameworkCore;
using TMD.EquitiesPrediction.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;
using static Org.BouncyCastle.Math.EC.ECCurve;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KalshiSettings>(builder.Configuration.GetSection("kalshi"));
builder.Services.Configure<CoinBaseSettings>(builder.Configuration.GetSection("CoinBase"));
builder.Services.AddHttpClient("TMD")
        .UseSocketsHttpHandler((handler, _) =>
            handler.PooledConnectionLifetime = TimeSpan.FromMinutes(7)) // Recreate connection every 2 minutes
        .SetHandlerLifetime(Timeout.InfiniteTimeSpan).RemoveAllLoggers();
builder.Services.AddDbContextFactory<TmdContext>(options =>
    options.UseSqlServer(builder.Configuration.GetSection("ConnectionStrings").GetSection("connection").Value));
builder.Services.AddHostedService<Worker>();
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
builder.Services.Configure<ConsoleLifetimeOptions>(options =>
{
    options.SuppressStatusMessages = true;
});
var host = builder.Build();
host.Run();
