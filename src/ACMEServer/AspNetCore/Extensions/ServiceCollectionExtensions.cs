using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Th11s.ACMEServer.AspNetCore.ModelBinding;
using Th11s.ACMEServer.Model.Workers;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.HttpModel.Services;
using Th11s.ACMEServer.AspNetCore.Filters;
using Th11s.ACMEServer.HostedServices;
using Th11s.ACMEServer.AspNetCore.Middleware;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.HostedServices.Workers;
using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.RequestServices;
using Th11s.ACMEServer.Services.ChallengeValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Th11s.ACMEServer.Services.Processors;
using System.Threading.Channels;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.AspNetCore.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddACMEServer(this IServiceCollection services, IConfiguration configuration,
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
            var acmeServerOptions = new ACMEServerOptions();
            acmeServerConfig.Bind(acmeServerOptions);

            services.Configure<ACMEServerOptions>(acmeServerConfig);

            return services;
        }
    }
}
