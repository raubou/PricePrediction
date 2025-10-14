using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using TMDInvestments.Models;
using static Org.BouncyCastle.Math.EC.ECCurve;

//[assembly: FunctionsStartup(typeof(TMDPricePrediction.Startup))]
namespace TMDPricePrediction
{
    //public class Startup : FunctionsStartup
    //{
    //    public override void Configure(IFunctionsHostBuilder builder)
    //    {
    //        //builder.Services.AddHttpClient();

    //        //builder.Services.AddSingleton<IMyService>((s) => {
    //        //    return new MyService();
    //        //});

    //        //builder.Services.AddSingleton<ILoggerProvider, MyLoggerProvider>();
    //        builder.Services.AddOptions<SchwabSettings>()
    //        .Configure<IConfiguration>((settings, configuration) =>
    //        {
    //            configuration.GetSection("SchwabAPI").Bind(settings);
    //        });
    //        builder.Services.AddOptions<CoinBaseSettings>()
    //        .Configure<IConfiguration>((settings, configuration) =>
    //        {
    //            configuration.GetSection("CoinBase").Bind(settings);
    //        });
    //        //var ConnectionStrings = config.GetSection("ConnectionStrings").Get<ConnectionSettings>();
    //        //var MailSettings = config.GetSection("MailSettings").Get<MailSettings>();
    //        //var FinnHub = config.GetSection("finnhub").Get<FinnHub>();
    //        //var Polygon = config.GetSection("polygon").Get<Polygon>();
    //        //var SchwabSettings = config.GetSection("SchwabAPI").Get<SchwabSettings>();
    //        //var CoinBaseSettings = config.GetSection("CoinBase").Get<CoinBaseSettings>();
    //    }
    //}
}
