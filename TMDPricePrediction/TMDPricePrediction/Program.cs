using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TMDInvestments.Models;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();
builder.Services.AddOptions<SchwabSettings>()
.Configure<IConfiguration>((settings, configuration) =>
{
    configuration.GetSection("SchwabAPI").Bind(settings);
});
builder.Services.AddOptions<ETradeSettings>()
.Configure<IConfiguration>((settings, configuration) =>
{
    configuration.GetSection("ETradeAPI").Bind(settings);
});
builder.Services.AddOptions<CoinBaseSettings>()
.Configure<IConfiguration>((settings, configuration) =>
{
    configuration.GetSection("CoinBase").Bind(settings);
});
builder.Services.AddOptions<yfapiSettings>()
.Configure<IConfiguration>((settings, configuration) =>
{
    configuration.GetSection("yfapi").Bind(settings);
});

await builder.Build().RunAsync();

