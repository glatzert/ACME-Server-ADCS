using System;
using System.Linq;
using DnsClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ACME.Server.ADCS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var asService = args.Contains("--AsService", StringComparer.OrdinalIgnoreCase);

            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, config) =>
                {
                    config.AddJsonFile("appsettings-custom.json", true);
                    config.AddJsonFile($"appsettings-custom.{ctx.HostingEnvironment.EnvironmentName}.json", true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging((ctx, logging) =>
                {
                    if (!ctx.Configuration.GetSection("Logging:EnableFileLog").Get<bool>())
                        return;

                    logging.AddFile(ctx.Configuration.GetSection("Logging"));
                });

            if (asService)
            {
                hostBuilder.UseWindowsService();
            }

            return hostBuilder;
        }
    }
}
