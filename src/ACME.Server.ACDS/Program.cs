using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ACME.Server.ACDS
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
                });

            if (asService)
            {
                hostBuilder.UseWindowsService();
            }

            return hostBuilder;
        }
    }
}
