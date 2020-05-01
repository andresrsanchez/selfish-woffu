using core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace woffu_function
{
    public class WoffuTimer
    {
        private readonly IConfiguration _configuration;

        public WoffuTimer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("WoffuTimer")]
        public async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            var user = _configuration.GetValue<string>("User");
            var password = _configuration.GetValue<string>("Password");
            var woffu = new Woffu();
            await woffu.TryToSignTodayAsync(user, password);
        }
    }
}
