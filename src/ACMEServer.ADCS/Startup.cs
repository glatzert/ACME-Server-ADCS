using ACMEServer.Storage.FileSystem.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.AspNetCore.Middleware;
using Th11s.ACMEServer.CertProvider.ADCS.Extensions;

namespace Th11s.ACMEServer.ADCS
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            if(_configuration.GetValue("Logging:EnableHttpLogging", false))
            {
                services.AddHttpLogging(opt => { });
            }

            services.AddControllers()
                .AddJsonOptions(opt =>
                {
                    opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            services.AddACMEServer(_configuration, "AcmeServer");
            services.AddACMEFileStore(_configuration, "AcmeFileStore");
            services.AddADCSIssuer(_configuration, "ADCSIssuer");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (_configuration.GetValue("Logging:EnableHttpLogging", false))
            {
                app.UseHttpLogging();
            }
            app.UseRouting();

            app.UseAcmeServer();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
