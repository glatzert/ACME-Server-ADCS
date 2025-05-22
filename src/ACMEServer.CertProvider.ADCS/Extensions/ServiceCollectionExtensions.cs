using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.Model.Services;

namespace Th11s.ACMEServer.CertProvider.ADCS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddADCSIssuer(this IServiceCollection services, IConfiguration configuration,
            string sectionName = "ADCSIssuer")
        {
            services.AddScoped<ICSRValidator, CSRValidator>();
            services.AddScoped<ICertificateIssuer, CertificateIssuer>();

            services.AddOptions<ADCSOptions>()
                .Bind(configuration.GetSection(sectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}
