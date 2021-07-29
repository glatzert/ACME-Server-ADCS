using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TGIT.ACME.Protocol.Services;
using TGIT.ACME.Protocol.RequestServices;
using TGIT.ACME.Protocol.Workers;
using TGIT.ACME.Server.BackgroundServices;
using TGIT.ACME.Server.Configuration;
using TGIT.ACME.Server.Filters;
using TGIT.ACME.Server.Middleware;
using TGIT.ACME.Server.ModelBinding;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddACMEServer(this IServiceCollection services, IConfiguration configuration, 
            string sectionName = "AcmeServer")
        {
            services.AddControllers();

            services.AddTransient<AcmeRequestReader>();

            services.AddScoped<IAcmeRequestProvider, DefaultRequestProvider>();

            services.AddScoped<IRequestValidationService, DefaultRequestValidationService>();
            services.AddScoped<INonceService, DefaultNonceService>();
            services.AddScoped<IAccountService, DefaultAccountService>();
            services.AddScoped<IOrderService, DefaultOrderService>();

            services.AddScoped<IAuthorizationFactory, DefaultAuthorizationFactory>();

            services.AddScoped<IIssuanceWorker, IssuanceWorker>();
            services.AddScoped<IValidationWorker, ValidationWorker>();

            services.AddHttpClient<Http01ChallangeValidator>();
            services.AddScoped<Dns01ChallangeValidator>();
            services.AddScoped<IChallangeValidatorFactory, DefaultChallangeValidatorFactory>();

            services.AddScoped<AddNextNonceFilter>();

            services.AddHostedService<HostedValidationService>();
            services.AddHostedService<HostedIssuanceService>();

            services.Configure<MvcOptions>(opt =>
            {
                opt.Filters.Add(typeof(AcmeExceptionFilter));
                opt.Filters.Add(typeof(ValidateAcmeRequestFilter));
                opt.Filters.Add(typeof(AcmeIndexLinkFilter));

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
