using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Th11s.ACMEServer.AspNetCore.Authentication;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.HostedServices;
using Th11s.ACMEServer.Json;
using Th11s.ACMEServer.Model.Configuration;
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
        
        services.AddScoped<IOrderValidator, DefaultOrderValidator>();
        services.AddScoped<IIssuanceProfileSelector, DefaultIssuanceProfileSelector>();

        services.AddScoped<IAuthorizationFactory, DefaultAuthorizationFactory>();

        services.AddHttpClient<Http01ChallengeValidator>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, Http01ChallengeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, Dns01ChallengeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, TlsAlpn01ChallengeValidator>());

        services.AddScoped<IChallengeValidatorFactory, DefaultChallengeValidatorFactory>();


        services.AddSingleton<OrderValidationQueue>();
        services.AddSingleton<OrderValidationProcessor>();

        services.AddHostedService<HostedOrderValidationService>();
        services.AddHostedService<OrderValidationRetryService>();


        services.AddSingleton<CertificateIssuanceQueue>();
        services.AddSingleton<CertificateIssuanceProcessor>();

        services.AddHostedService<HostedCertificateIssuanceService>();
        services.AddHostedService<CertificateIssuanceRetryService>();


        var acmeServerConfig = configuration.GetSection(sectionName);
        var acmeServerOptions = new ACMEServerOptions();
        acmeServerConfig.Bind(acmeServerOptions);

        services.Configure<ACMEServerOptions>(acmeServerConfig);
        services.ConfigureProfiles(configuration);

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


    public static IServiceCollection ConfigureProfiles(this IServiceCollection services, IConfiguration configuration)
    {
        var profileSection = configuration.GetSection("Profiles")?.GetChildren();
        if(profileSection?.Any() != true)
        {
            throw new ApplicationException("Could not find any profiles in configuration. Without profiles the server cannot provide any service.");
        }

        var profiles = new HashSet<ProfileName>();
        foreach (var profile in profileSection)
        {
            profiles.Add(new ProfileName(profile.Key));
            services.AddOptions<ProfileConfiguration>(profile.Key)
                .BindConfiguration(profile.Path)
                .Configure(p => p.Name = profile.Key);
        }

        // TODO: probably it's advisable to encapsulate this in a class
        services.AddOptions<HashSet<ProfileName>>()
            .Configure(p => p.UnionWith(profiles));

        return services;
    }
}
