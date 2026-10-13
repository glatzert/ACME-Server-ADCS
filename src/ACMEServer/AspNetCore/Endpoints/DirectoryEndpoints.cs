using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.AspNetCore.Endpoints.Metadata;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.AspNetCore.Endpoints;

public static class DirectoryEndpoints
{
    public static IEndpointRouteBuilder MapDirectoryEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", GetDirectory)
            .WithName(EndpointNames.Directory)
            .WithMetadata(new SkipNonceGeneration());
        builder.MapGet("/directory", GetDirectory)
            .WithName(EndpointNames.DirectoryAlt)
            .WithMetadata(new SkipNonceGeneration());

        builder.MapGet("/profile/{profile}", GetProfile)
            .WithName(EndpointNames.ProfileMetadata)
            .WithMetadata(new SkipNonceGeneration());

        return builder;
    }

    public static IResult GetDirectory(
        IOptions<ACMEServerOptions> options,
        IProfileProvider profileProvider,
        ILinkGenerator linkGenerator,
        HttpContext httpContext)
    {
        var response = new HttpModel.Directory
        {
            NewNonce = linkGenerator.NewNonce(),
            NewAccount = linkGenerator.NewAccount(),
            NewOrder = linkGenerator.NewOrder(),
            NewAuthz = null, //TODO: linkGenerator.NewAuthz(),
            RevokeCert = options.Value.SupportsRevokation
                ? linkGenerator.RevokeCert()
                : null,
            KeyChange = linkGenerator.KeyChange(),
            Meta = new HttpModel.DirectoryMetadata
            {
                // TODO: For some reason, this can contain duplicates, in another occasion this was a problem with loading from config and default arrays
                CAAIdentities = [.. options.Value.CAAIdentities.Distinct()], // TODO: For some reason, this can contain duplicates
                ExternalAccountRequired = options.Value.ExternalAccountBinding?.Required == true,
                TermsOfService = options.Value.TOS.Url,
                Website = options.Value.WebsiteUrl,

                Profiles = profileProvider.GetProfileNames().ToDictionary(
                    profileName => profileName.ToString(),
                    linkGenerator.ProfileMetadata),
            }
        };


        // dns-persist-01 support
        var supportsDnsPersist01 = profileProvider.GetProfileNames().Select(profileProvider.GetProfileConfiguration)
            .SelectMany(x => x.AllowedChallengeTypes.Values)
            .Any(x => x.Contains(ChallengeTypes.DnsPersist01));

        if (supportsDnsPersist01)
        {
            response.Meta.IssuerDomainNames = [.. options.Value.DnsPersistPreProvisioning?.IssuerDomainNames ?? options.Value.CAAIdentities.Take(10)];
            response.Meta.AccountHashPrefix = options.Value.DnsPersistPreProvisioning?.AccountHashPrefix ?? linkGenerator.AccountHash();
        }

        return Results.Ok(response);
    }


    public static IResult GetProfile(
        string profile,
        IProfileProvider profileProvider)
    {
        if(profileProvider.TryGetProfileConfiguration(new(profile), out var profileConfiguration))
        {
            return Results.Ok(new HttpModel.ProfileMetadata
            {
                ProfileName = profileConfiguration.Name,
                ExternalAccountRequired = profileConfiguration.RequireExternalAccountBinding,
                SupportedIdentifierTypes = [..profileConfiguration.SupportedIdentifiers],
                Priority = profileConfiguration.Priority,
                ChallengeTypes = profileConfiguration.AllowedChallengeTypes.ToDictionary(x => x.Key, x => x.Value.ToArray())
            });
        }
        else
        {
            return Results.NotFound();
        }
    }
}
