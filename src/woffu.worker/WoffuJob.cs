using Microsoft.Extensions.Logging;
using Quartz;
using System.Threading.Tasks;

namespace woffu.worker
{
    [DisallowConcurrentExecution]
    public class WoffuJob : IJob
    {
        private readonly ILogger<WoffuJob> _logger;
        public WoffuJob(ILogger<WoffuJob> logger)
        {
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Hello world!");
            return Task.CompletedTask;
        }
    }
}