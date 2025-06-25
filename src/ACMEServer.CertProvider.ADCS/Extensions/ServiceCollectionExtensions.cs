using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.CertProvider.ADCS.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddADCSIssuer(this IServiceCollection services)
    {
        services.AddScoped<ICertificateIssuer, CertificateIssuer>();

        return services;
    }
}
