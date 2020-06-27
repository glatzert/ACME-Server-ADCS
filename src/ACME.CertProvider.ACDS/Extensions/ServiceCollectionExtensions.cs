using Microsoft.Extensions.Configuration;
using TGIT.ACME.Protocol.IssuanceServices;
using TGIT.ACME.Protocol.IssuanceServices.ACDS;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddACDSIssuer(this IServiceCollection services, IConfiguration configuration,
            string sectionName = "ACDSIssuer")
        {
            services.AddScoped<ICsrValidator, CsrValidator>();
            services.AddScoped<ICertificateIssuer, CertificateIssuer>();

            services.AddOptions<ACDSOptions>()
                .Bind(configuration.GetSection(sectionName))
                .ValidateDataAnnotations();

            return services;
        }
    }
}
