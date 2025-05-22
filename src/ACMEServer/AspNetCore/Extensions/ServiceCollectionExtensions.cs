using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using Th11s.ACMEServer.AspNetCore.Filters;
using Th11s.ACMEServer.AspNetCore.Middleware;
using Th11s.ACMEServer.AspNetCore.ModelBinding;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.HostedServices;
using Th11s.ACMEServer.HttpModel.Services;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.RequestServices;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Services.ChallengeValidation;
using Th11s.ACMEServer.Services.Processors;

namespace Th11s.ACMEServer.AspNetCore.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddACMEServer(this IServiceCollection services, IConfiguration configuration, ILogger logger,
            string sectionName = "AcmeServer")
        {
            services.AddControllers();

            services.AddTransient((_) => TimeProvider.System);
            services.AddTransient<AcmeRequestReader>();

            services.AddScoped<IAcmeRequestProvider, DefaultRequestProvider>();

            services.AddScoped<IRequestValidationService, DefaultRequestValidationService>();
            services.AddScoped<INonceService, DefaultNonceService>();
            services.AddScoped<IAccountService, DefaultAccountService>();
            services.AddScoped<IOrderService, DefaultOrderService>();

            services.AddScoped<AddNextNonceFilter>();
            services.AddScoped<IAuthorizationFactory, DefaultAuthorizationFactory>();

            services.AddHttpClient<Http01ChallengeValidator>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, Http01ChallengeValidator>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, Dns01ChallengeValidator>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, TlsAlpn01ChallengeValidator>());

            services.AddScoped<IChallengeValidatorFactory, DefaultChallengeValidatorFactory>();


            services.AddKeyedSingleton(nameof(OrderValidationProcessor), (_, _) => Channel.CreateUnbounded<OrderId>());
            services.AddSingleton<OrderValidationProcessor>();

            services.AddHostedService<HostedOrderValidationService>();
            services.AddHostedService<OrderValidationRetryService>();


            services.AddKeyedSingleton(nameof(CertificateIssuanceProcessor), (_, _) => Channel.CreateUnbounded<OrderId>());
            services.AddSingleton<CertificateIssuanceProcessor>();

            services.AddHostedService<HostedCertificateIssuanceService>();
            services.AddHostedService<CertificateIssuanceRetryService>();


            services.Configure<MvcOptions>(opt =>
            {
                opt.Filters.Add(typeof(AcmeExceptionFilter));
                opt.Filters.Add(typeof(ValidateAcmeRequestFilter));
                opt.Filters.Add(typeof(AcmeIndexLinkFilter));
                opt.Filters.Add(typeof(AcmeLocationFilter));

                opt.ModelBinderProviders.Insert(0, new AcmeModelBindingProvider());
            });

            var acmeServerConfig = configuration.GetSection(sectionName);
            services.AddOptions<ACMEServerOptions>()
                .BindConfiguration(sectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            if (configuration.GetSection($"{sectionName}:ExternalAccountBinding").Exists())
            {
                logger.LogInformation("Found ExternalAccountBinding configuration. Registering ExternalAccountBinding services.");

                services.AddScoped<IExternalAccountBindingValidator, DefaultExternalAccountBindingValidator>();
                services.AddHttpClient<IExternalAccountBindingClient, DefaultExternalAccountBindingClient>();
            }
            else
            {
                logger.LogInformation("No ExternalAccountBinding configuration found.");

                services.AddSingleton<IExternalAccountBindingValidator, NullExternalAccountBindingValidator>();
            }

            return services;
        }
    }
}
