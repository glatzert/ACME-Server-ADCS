using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.CertProvider.ADCS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddADCSIssuer(this IServiceCollection services, string sectionName = "ADCSIssuer")
        {
            services.AddScoped<ICSRValidator, CSRValidator>();
            services.AddScoped<ICertificateIssuer, CertificateIssuer>();

            services.AddOptions<ADCSOptions>()
                .BindConfiguration(sectionName)
                .ValidateDataAnnotations();

            return services;
        }
    }
}
