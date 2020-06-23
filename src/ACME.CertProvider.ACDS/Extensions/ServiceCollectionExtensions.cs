using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using System.Linq;
using TGIT.ACME.CertProvider.ACDS;
using TGIT.ACME.Protocol.HttpModel.Converters;
using TGIT.ACME.Protocol.Services;
using TGIT.ACME.Protocol.Storage;
using TGIT.ACME.Protocol.Workers;
using TGIT.ACME.Server.BackgroundServices;
using TGIT.ACME.Server.Configuration;
using TGIT.ACME.Server.Filters;
using TGIT.ACME.Storage.FileStore;
using TGIT.ACME.Storage.FileStore.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddACDSIssuer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICsrValidator, CsrValidator>();
            services.AddScoped<ICertificateIssuer, CertificateIssuer>();

            return services;
        }
    }
}
