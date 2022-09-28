using Microsoft.Extensions.Configuration;
using TGIT.ACME.Protocol.IssuanceServices;
using TGIT.ACME.Protocol.IssuanceServices.ADCS;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddADCSIssuer(this IServiceCollection services, IConfiguration configuration,
            string sectionName = "ADCSIssuer")
        {
            services.AddScoped<ICsrValidator, CsrValidator>();
            services.AddScoped<ICertificateIssuer, CertificateIssuer>();

            services.AddOptions<ADCSOptions>()
                .Bind(configuration.GetSection(sectionName))
                .ValidateDataAnnotations();

            return services;
        }
    }
}
