using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.FileSystem.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddACMEFileStore(this IServiceCollection services, IConfiguration configuration, string sectionName)
        {
            services.AddScoped<INonceStore, NonceStore>();
            services.AddScoped<IAccountStore, AccountStore>();
            services.AddScoped<IOrderStore, OrderStore>();

            services.AddOptions<FileStoreOptions>()
                .Bind(configuration.GetSection(sectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}
