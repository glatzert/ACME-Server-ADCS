using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading.Channels;
using Th11s.ACMEServer.AspNetCore.Authentication;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.HostedServices;
using Th11s.ACMEServer.Json;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.RequestServices;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Services.ChallengeValidation;
using Th11s.ACMEServer.Services.Processors;

namespace Th11s.ACMEServer.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddACMEServer(this IServiceCollection services, IConfiguration configuration,
        string sectionName = "AcmeServer")
    {
        services.AddControllers();

        services.AddTransient((_) => TimeProvider.System);

        services.AddAuthentication(JWSAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<JWSAuthenticationOptions, JWSAuthenticationHandler>(JWSAuthenticationDefaults.AuthenticationScheme, null);
        services.AddAuthorization(authz =>
        {
            authz.AddPolicy(Policies.ValidAccount, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(AcmeClaimTypes.AccountId);
                policy.Requirements.Add(new TOSRequirement());
            });
        });

        services.AddScoped<IAuthorizationHandler, TOSRequirementHandler>();
        services.AddHttpContextAccessor();

        services.AddScoped<IRequestValidationService, DefaultRequestValidationService>();
        services.AddScoped<INonceService, DefaultNonceService>();
        services.AddScoped<IAccountService, DefaultAccountService>();
        services.AddScoped<IOrderService, DefaultOrderService>();

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


        var acmeServerConfig = configuration.GetSection(sectionName);
        var acmeServerOptions = new ACMEServerOptions();
        acmeServerConfig.Bind(acmeServerOptions);

        services.Configure<ACMEServerOptions>(acmeServerConfig);

        if (configuration.GetSection($"{sectionName}:ExternalAccountBinding").Exists())
        {
            services.AddScoped<IExternalAccountBindingValidator, DefaultExternalAccountBindingValidator>();
            services.AddHttpClient<IExternalAccountBindingClient, DefaultExternalAccountBindingClient>();
        }
        else
        {
            services.AddSingleton<IExternalAccountBindingValidator, NullExternalAccountBindingValidator>();
        }

        services.ConfigureHttpJsonOptions(opt => opt.SerializerOptions.ApplyDefaultJsonSerializerOptions());

        return services;
    }
}
