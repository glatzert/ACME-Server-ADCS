using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TGIT.ACME.Server.ModelBinding;
using Th11s.ACMEServer.Model.Workers;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.HttpModel.Services;
using Th11s.ACMEServer.AspNetCore.Filters;
using Th11s.ACMEServer.BackgroundServices;
using Th11s.ACMEServer.AspNetCore.Middleware;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.BackgroundServices.Workers;
using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.RequestServices;
using Th11s.ACMEServer.Services.ChallangeValidation;

namespace Th11s.ACMEServer.AspNetCore.Extensions
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
