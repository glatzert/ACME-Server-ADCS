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
using Th11s.ACMEServer.Services.CsrValidation;
using DnsClient;
using System.Net;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.AspNetCore;

public static class AcmeServerExtension
{
    public static class SectionNames
    {
        public const string AcmeServer = "AcmeServer";
        public const string ExternalAccountBinding = "AcmeServer:ExternalAccountBinding";
        public const string DNSOverrides = "DNS";
        public const string Profiles = "Profiles";
    }

    public static IServiceCollection AddACMEServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Setup a logger for Startup logging
        using var serviceProvider = services.BuildServiceProvider();
        using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(AcmeServerExtension).FullName!);

        services.AddTransient((_) => TimeProvider.System);

        services.AddSingleton(CreateDnsClient);
        services.AddKeyedSingleton(nameof(CAAQueryHandler), (sp, _) => sp.GetRequiredService<ILookupClient>());
        services.AddKeyedSingleton(nameof(Dns01ChallengeValidator), (sp, _) => sp.GetRequiredService<ILookupClient>());
        services.AddKeyedSingleton(nameof(DnsPersist01ChallengeValidator), (sp, _) => sp.GetRequiredService<ILookupClient>());

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
        services.AddScoped<INonceFactory, DefaultNonceFactory>();
        services.AddScoped<IAccountService, DefaultAccountService>();
        services.AddScoped<IOrderService, DefaultOrderService>();
        services.AddScoped<IRevokationService, DefaultRevokationService>();

        services.AddScoped<IIdentifierValidator, DefaultIdentifierValidator>();
        services.AddScoped<ICAAEvaluator, DefaultCAAEvaluator>();
        services.AddScoped<ICAAQueryHandler, CAAQueryHandler>();
        services.AddScoped<IIssuanceProfileSelector, DefaultIssuanceProfileSelector>();

        services.AddScoped<IAuthorizationFactory, DefaultAuthorizationFactory>();

        services.AddHttpClient(nameof(Http01ChallengeValidator));
        services.AddHttpClient(nameof(Http01ChallengeValidator) + Http01ChallengeValidator.IgnoreCertHttpClientSuffix)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }
        );

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, Http01ChallengeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, Dns01ChallengeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, TlsAlpn01ChallengeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, DeviceAttest01ChallengeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, DnsPersist01ChallengeValidator>());

        services.AddScoped<IChallengeValidatorFactory, DefaultChallengeValidatorFactory>();
        services.AddHttpClient<IDeviceAttest01RemoteValidator, DeviceAttest01RemoteValidator>();

        services.AddScoped<ICsrValidator, CsrValidator>();
        services.AddScoped<IPublicKeyAnalyzer, DefaultPublicKeyAnalyzer>();

        services.AddSingleton<OrderValidationQueue>();
        services.AddSingleton<OrderValidationProcessor>();

        services.AddHostedService<HostedOrderValidationService>();
        services.AddHostedService<OrderValidationRetryService>();


        services.AddSingleton<CertificateIssuanceQueue>();
        services.AddSingleton<CertificateIssuanceProcessor>();

        services.AddHostedService<HostedCertificateIssuanceService>();
        services.AddHostedService<CertificateIssuanceRetryService>();

        services.AddOptions<ACMEServerOptions>()
            .BindConfiguration(SectionNames.AcmeServer)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddExternalAccountBindingIfConfigured(configuration, SectionNames.ExternalAccountBinding, logger);

        services.AddOptions<DNSOverrideOptions>()
            .BindConfiguration(SectionNames.DNSOverrides)
            .ValidateDataAnnotations()
            .ValidateOnStart();


        services.ConfigureProfiles(configuration, logger);

        services.AddScoped<IProfileProvider, DefaultProfileProvider>();


        services.ConfigureHttpJsonOptions(opt => opt.SerializerOptions.ApplyDefaultJsonSerializerOptions());

        return services;
    }

    private static ILookupClient CreateDnsClient(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<ILookupClient>();

        var options = serviceProvider.GetRequiredService<IOptions<DNSOverrideOptions>>();

        if (options.Value.NameServers.Length == 0)
        {
            logger?.UsingSystemDefaultDns();
            return new LookupClient();
        }

        List<NameServer> nameServers = [];
        foreach (string endPoint in options.Value.NameServers)
        {
            if (IPEndPoint.TryParse(endPoint, out var dnsEndPoint))
            {
                if (dnsEndPoint.Port == 0)
                {
                    dnsEndPoint.Port = NameServer.DefaultPort;
                }

                nameServers.Add(new NameServer(dnsEndPoint));
            }
            else
            {
                logger?.CouldNotParseDnsEndpoint(endPoint);
            }
        }

        if (nameServers.Count == 0)
        {
            logger?.FallingBackToSystemDns();
            return new LookupClient();
        }

        return new LookupClient(nameServers.ToArray());
    }

    public static IServiceCollection ConfigureProfiles(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var profileNames = configuration.GetProfileNames(SectionNames.Profiles, logger);
        if (profileNames.Count == 0)
        {
            throw new ApplicationException("Could not find any profiles in configuration. Without profiles the server cannot provide any service.");
        }

        foreach (var profile in profileNames)
        {
            var profileSection = configuration.GetSection($"{SectionNames.Profiles}:{profile.Value}");

            if (profileSection.GetSection("ADCSOptions").Exists())
            {
                logger.ProfileADCSOptionsSectionIsDeprectated(profileSection.Key);

                if (profileSection.GetSection(nameof(ProfileConfiguration.CertificateServices)).Exists())
                {
                    logger.ProfileADCSOptionsAndCertificateServicesSectionBothExist(profileSection.Key);
                }
            }

            services.AddOptions<ProfileConfiguration>(profile.Value)
                .BindConfiguration(profileSection.Path)
                .Configure(p => p.Name = profile.Value)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            logger.ProfileConfigured(profile.Value);
        }

        services.AddSingleton(profileNames);
        services.AddSingleton<IPostConfigureOptions<ProfileConfiguration>, DefaultProfileConfiguration>();

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
        app.MapRevokationEndpoints();

        // Add this endpoint to be availble to tests. It enables us to test middlewares without influence of the rest of the application.
        if (app.Environment.IsEnvironment("Test"))
        {
            app.MapPost("/test", () => Results.Ok("{}"))
                .RequireAuthorization();
        }

        return app;
    }
}
