using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using woffu_function;

[assembly: FunctionsStartup(typeof(Startup))]
namespace woffu_function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddUserSecrets<Startup>()
               .AddEnvironmentVariables()
               .Build();

            builder.Services.AddSingleton<IConfiguration>(config);
        }
    }
}
