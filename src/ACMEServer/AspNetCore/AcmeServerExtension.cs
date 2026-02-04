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
    public static IServiceCollection AddACMEServer(
        this IServiceCollection services, 
        IConfiguration configuration,
        string sectionName = "AcmeServer")
    {
        // Setup a logger for Startup logging
        using var serviceProvider = services.BuildServiceProvider();
        using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(AcmeServerExtension).FullName!);

        services.AddTransient((_) => TimeProvider.System);

        services.AddSingleton(sp => CreateDnsClient(sp));
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

        services.AddHttpClient<Http01ChallengeValidator>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, Http01ChallengeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, Dns01ChallengeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, TlsAlpn01ChallengeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, DeviceAttest01ChallengeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IChallengeValidator, DnsPersist01ChallengeValidator>());

        services.AddScoped<IChallengeValidatorFactory, DefaultChallengeValidatorFactory>();
        services.AddHttpClient<IDeviceAttest01RemoteValidator, DeviceAttest01RemoteValidator>();

        services.AddScoped<ICsrValidator, CsrValidator>();

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

        services.AddOptions<DNSOverrideOptions>()
            .BindConfiguration("DNS")
            .ValidateOnStart();

        var acmeServerOptions = new ACMEServerOptions();
        acmeServerConfig.Bind(acmeServerOptions);

        services.Configure<ACMEServerOptions>(acmeServerConfig);
        services.ConfigureProfiles(configuration, logger);

        services.AddScoped<IProfileProvider, DefaultProfileProvider>();

        if (configuration.GetSection($"{sectionName}:ExternalAccountBinding").Exists())
        {
            logger.ExternalAccountBindingEnabled(sectionName);
            services.AddScoped<IExternalAccountBindingValidator, DefaultExternalAccountBindingValidator>();
            services.AddHttpClient<IExternalAccountBindingClient, DefaultExternalAccountBindingClient>();
        }
        else
        {
            logger.ExternalAccountBindingNotEnabled(sectionName);
            services.AddSingleton<IExternalAccountBindingValidator, NullExternalAccountBindingValidator>();
        }

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
        foreach(string endPoint in options.Value.NameServers)
        {
            if(IPEndPoint.TryParse(endPoint, out var dnsEndPoint))
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
        var profileSection = configuration.GetSection("Profiles")?.GetChildren();
        if (profileSection?.Any() != true)
        {
            throw new ApplicationException("Could not find any profiles in configuration. Without profiles the server cannot provide any service.");
        }

        var profiles = new ProfileNamesCollection();
        foreach (var profile in profileSection)
        {
            if(!profiles.Add(new ProfileName(profile.Key)))
            {
                logger.ProfileExistsMultipleTimes(profile.Key);
            }

            services.AddOptions<ProfileConfiguration>(profile.Key)
                .BindConfiguration(profile.Path)
                .Configure(p => p.Name = profile.Key)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            logger.ProfileConfigured(profile.Key);
        }

        services.AddSingleton(profiles);
        services.PostConfigureAll<ProfileConfiguration>(SetProfileDefaults);

        return services;
    }

    private static void SetProfileDefaults(ProfileConfiguration profile)
    {
        profile.SupportedIdentifiers ??= [];

        profile.AllowedChallengeTypes ??= [];
        if(!profile.AllowedChallengeTypes.ContainsKey(IdentifierTypes.DNS))
        {
            profile.AllowedChallengeTypes[IdentifierTypes.DNS] = [ChallengeTypes.Dns01, ChallengeTypes.Http01, ChallengeTypes.TlsAlpn01];
        }
        if(!profile.AllowedChallengeTypes.ContainsKey(IdentifierTypes.IP))
        {
            profile.AllowedChallengeTypes[IdentifierTypes.IP] = [ChallengeTypes.Http01, ChallengeTypes.TlsAlpn01];
        }
        if(!profile.AllowedChallengeTypes.ContainsKey(IdentifierTypes.Email))
        {
            profile.AllowedChallengeTypes[IdentifierTypes.Email] = [];
        }
        if(!profile.AllowedChallengeTypes.ContainsKey(IdentifierTypes.PermanentIdentifier))
        {
            profile.AllowedChallengeTypes[IdentifierTypes.PermanentIdentifier] = [ChallengeTypes.DeviceAttest01];
        }
        if(!profile.AllowedChallengeTypes.ContainsKey(IdentifierTypes.HardwareModule))
        {
            profile.AllowedChallengeTypes[IdentifierTypes.HardwareModule] = [ChallengeTypes.DeviceAttest01];
        }

        profile.IdentifierValidation.DNS.AllowedDNSNames ??= [""];
        profile.IdentifierValidation.IP.AllowedIPNetworks ??= ["::0/0", "0.0.0.0/0"];

        profile.ChallengeValidation.DeviceAttest01.Apple.RootCertificates ??= ["MIICJDCCAamgAwIBAgIUQsDCuyxyfFxeq/bxpm8frF15hzcwCgYIKoZIzj0EAwMwUTEtMCsGA1UEAwwkQXBwbGUgRW50ZXJwcmlzZSBBdHRlc3RhdGlvbiBSb290IENBMRMwEQYDVQQKDApBcHBsZSBJbmMuMQswCQYDVQQGEwJVUzAeFw0yMjAyMTYxOTAxMjRaFw00NzAyMjAwMDAwMDBaMFExLTArBgNVBAMMJEFwcGxlIEVudGVycHJpc2UgQXR0ZXN0YXRpb24gUm9vdCBDQTETMBEGA1UECgwKQXBwbGUgSW5jLjELMAkGA1UEBhMCVVMwdjAQBgcqhkjOPQIBBgUrgQQAIgNiAAT6Jigq+Ps9Q4CoT8t8q+UnOe2poT9nRaUfGhBTbgvqSGXPjVkbYlIWYO+1zPk2Sz9hQ5ozzmLrPmTBgEWRcHjA2/y77GEicps9wn2tj+G89l3INNDKETdxSPPIZpPj8VmjQjBAMA8GA1UdEwEB/wQFMAMBAf8wHQYDVR0OBBYEFPNqTQGd8muBpV5du+UIbVbi+d66MA4GA1UdDwEB/wQEAwIBBjAKBggqhkjOPQQDAwNpADBmAjEA1xpWmTLSpr1VH4f8Ypk8f3jMUKYz4QPG8mL58m9sX/b2+eXpTv2pH4RZgJjucnbcAjEA4ZSB6S45FlPuS/u4pTnzoz632rA+xW/TZwFEh9bhKjJ+5VQ9/Do1os0u3LEkgN/r"];
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
        if(app.Environment.IsEnvironment("Test"))
        {
            app.MapPost("/test", () => Results.Ok("{}"))
                .RequireAuthorization();
        }

        return app;
    }
}
