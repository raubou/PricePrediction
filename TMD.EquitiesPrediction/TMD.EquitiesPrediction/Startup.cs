using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(TMD.EquitiesPrediction.Startup))]
namespace TMD.EquitiesPrediction
{
    class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient("TMD")
            .UseSocketsHttpHandler((handler, _) =>
                handler.PooledConnectionLifetime = TimeSpan.FromMinutes(7)) // Recreate connection every 2 minutes
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan); // Disable rotation, as it is handled by PooledConnectionLifetime
        }
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddHttpClient();
        }

    }
}
