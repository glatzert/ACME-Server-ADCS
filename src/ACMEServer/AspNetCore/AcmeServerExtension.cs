using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Th11s.ACMEServer.AspNetCore.Authentication;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.AspNetCore.Endpoints;
using Th11s.ACMEServer.AspNetCore.Middleware;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.HostedServices;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.RequestServices;
using Th11s.ACMEServer.Services.ChallengeValidation;
using Th11s.ACMEServer.Services.Processors;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Th11s.ACMEServer.AspNetCore;

public static class AcmeServerExtension
{
    public static IServiceCollection AddACMEServer(this IServiceCollection services, IConfiguration configuration,
        string sectionName = "AcmeServer")
    {
        using var serviceProvider = services.BuildServiceProvider();
        using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(AcmeServerExtension).FullName!);

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
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, DeviceAttest01ChallengeValidator>());

        services.AddScoped<IChallengeValidatorFactory, DefaultChallengeValidatorFactory>();
        services.AddHttpClient<IDeviceAttest01RemoteValidator, DeviceAttest01RemoteValidator>();

        services.AddSingleton<OrderValidationQueue>();
        services.AddSingleton<OrderValidationProcessor>();

        services.AddHostedService<HostedOrderValidationService>();
        services.AddHostedService<OrderValidationRetryService>();


        services.AddSingleton<CertificateIssuanceQueue>();
        services.AddSingleton<CertificateIssuanceProcessor>();

        services.AddHostedService<HostedCertificateIssuanceService>();
        services.AddHostedService<CertificateIssuanceRetryService>();


        var acmeServerConfig = configuration.GetSection(sectionName);
        services.AddOptions<ACMEServerOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var acmeServerOptions = new ACMEServerOptions();
        acmeServerConfig.Bind(acmeServerOptions);

        services.Configure<ACMEServerOptions>(acmeServerConfig);
        services.ConfigureProfiles(configuration, logger);

        if (configuration.GetSection($"{sectionName}:ExternalAccountBinding").Exists())
        {
            logger.LogInformation("External account binding is enabled.");
            services.AddScoped<IExternalAccountBindingValidator, DefaultExternalAccountBindingValidator>();
            services.AddHttpClient<IExternalAccountBindingClient, DefaultExternalAccountBindingClient>();
        }
        else
        {
            logger.LogInformation("External account binding is not enabled.");
            services.AddSingleton<IExternalAccountBindingValidator, NullExternalAccountBindingValidator>();
        }

        services.ConfigureHttpJsonOptions(opt => opt.SerializerOptions.ApplyDefaultJsonSerializerOptions());

        return services;
    }


    public static IServiceCollection ConfigureProfiles(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var profileSection = configuration.GetSection("Profiles")?.GetChildren();
        if (profileSection?.Any() != true)
        {
            throw new ApplicationException("Could not find any profiles in configuration. Without profiles the server cannot provide any service.");
        }

        var profiles = new HashSet<ProfileName>();
        foreach (var profile in profileSection)
        {
            profiles.Add(new ProfileName(profile.Key));
            services.AddOptions<ProfileConfiguration>(profile.Key)
                .BindConfiguration(profile.Path)
                .Configure(p => p.Name = profile.Key)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            logger.LogInformation("Added Profile {profileName}.", profile.Key);
        }

        // TODO: probably it's advisable to encapsulate this in a class
        services.AddOptions<HashSet<ProfileName>>()
            .Configure(p => p.UnionWith(profiles));

        return services;
    }

    public static WebApplication UseAcmeServer(this WebApplication app)
    {
        app.UseRouting();

        app.UseMiddleware<AcmeExceptionHandlerMiddlerware>();
        app.UseMiddleware<AcmeUnauthorizedResponseHandler>();
        app.UseMiddleware<AcmeRequestMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapDirectoryEndpoints();
        app.MapNonceEndpoints();
        app.MapAccountEndpoints();
        app.MapOrderEndpoints();

        // Add this endpoint to be availble to tests. It enables us to test middlewares without influence of the rest of the application.
        if(app.Environment.IsEnvironment("Test"))
        {
            app.MapPost("/test", () => Results.Ok("{}"))
                .RequireAuthorization();
        }

        return app;
    }
}
