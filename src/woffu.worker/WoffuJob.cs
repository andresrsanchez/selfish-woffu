using core;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading.Tasks;

namespace woffu.worker
{
    //public record woffuoptions(string user, string password)
    [DisallowConcurrentExecution]
    public class WoffuJob : IJob
    {
        private readonly ILogger<WoffuJob> _logger;
        private readonly Woffu _woffu;

        public WoffuJob(ILogger<WoffuJob> logger, Woffu woffu)
        {
            _logger = logger;
            _woffu = woffu;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Sign job running at: {time}", DateTimeOffset.Now);

            try
            {
                await _woffu.TryToSignTodayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}