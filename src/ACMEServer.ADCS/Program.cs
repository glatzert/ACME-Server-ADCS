namespace Th11s.ACMEServer.ADCS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
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
                })
                .UseWindowsService();

            return hostBuilder;
        }
    }
}
