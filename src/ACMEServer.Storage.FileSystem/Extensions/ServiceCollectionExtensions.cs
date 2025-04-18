using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.FileSystem.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddACMEFileStore(this IServiceCollection services, string sectionName)
    {
        services.AddScoped<INonceStore, NonceStore>();
        services.AddScoped<IAccountStore, AccountStore>();
        services.AddScoped<IOrderStore, OrderStore>();

        services.AddOptions<FileStoreOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations();

        return services;
    }
}
