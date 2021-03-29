using core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Serilog;

namespace woffu.worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((builderContext, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();

                    var logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(builderContext.Configuration)
                        .WriteTo.File($@"/var/log/woffu/log.txt", rollingInterval: RollingInterval.Day)
                        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext} {Message}{NewLine}{Exception}")
                        .CreateLogger();

                    loggingBuilder.AddSerilog(logger, dispose: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(x => new WoffuOptions(hostContext.Configuration["WoffuUser"], hostContext.Configuration["WoffuPassword"]));
                    services.AddTransient<Woffu>();
                    services
                        .AddQuartz(q =>
                        {
                            q.UseMicrosoftDependencyInjectionScopedJobFactory();

                            var job = nameof(WoffuJob);
                            var jobKey = new JobKey(job);

                            q.AddJob<WoffuJob>(opts => opts.WithIdentity(jobKey));
                            q.AddTrigger(opts => opts
                                .ForJob(jobKey)
                                .WithIdentity($"{job}-morning-trigger")
                                .WithCronSchedule(hostContext.Configuration["QuartzMorning"]));
                            q.AddTrigger(opts => opts
                                .ForJob(jobKey)
                                .WithIdentity($"{job}-afternoon-trigger")
                                .WithCronSchedule(hostContext.Configuration["QuartzAfternoon"]));

                        }).AddQuartzHostedService(x => x.WaitForJobsToComplete = true);
                });
    }
}
