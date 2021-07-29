using Microsoft.Extensions.Configuration;
using TGIT.ACME.Protocol.Storage;
using TGIT.ACME.Storage.FileStore;
using TGIT.ACME.Storage.FileStore.Configuration;

namespace Microsoft.Extensions.DependencyInjection
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
                .ValidateDataAnnotations();

            return services;
        }
    }
}
