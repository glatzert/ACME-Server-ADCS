using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.AspNetCore.Endpoints.Metadata;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
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
            .WithMetadata(new SkipNonceGeneration());

        builder.MapGet("/profile/{profile}", GetProfile)
            .WithName(EndpointNames.Profile)
            .WithMetadata(new SkipNonceGeneration());

        return builder;
    }

    public static IResult GetDirectory(
        IOptions<ACMEServerOptions> options,
        IProfileProvider profileProvider,
        ILinkGenerator linkGenerator,
        HttpContext httpContext)
    {
        return Results.Ok(new HttpModel.Directory
        {
            NewNonce = linkGenerator.NewNonce(),
            NewAccount = linkGenerator.NewAccount(),
            NewOrder = linkGenerator.NewOrder(),
            NewAuthz = linkGenerator.NewAuthz(),
            RevokeCert = options.Value.SupportsRevokation 
                ? linkGenerator.RevokeCert()
                : null,
            KeyChange = linkGenerator.KeyChange(),
            Meta = new HttpModel.DirectoryMetadata
            {
                ExternalAccountRequired = options.Value.ExternalAccountBinding?.Required == true,
                // TODO: For some reason, this can contain duplicates, in another occasion this was a problem with loading from config and default arrays
                CAAIdentities = options.Value.CAAIdentities.Distinct().ToArray(), // TODO: For some reason, this can contain duplicates
                TermsOfService = options.Value.TOS.Url,
                Website = options.Value.WebsiteUrl,
                Profiles = profileProvider.GetProfileNames().ToDictionary(
                    profileName => profileName.ToString(),
                    profileName => linkGenerator.ProfileMetadata(profileName))
            }
        });
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
                SupportedIdentifierTypes = profileConfiguration.SupportedIdentifiers,
                ChallengeTypes = profileConfiguration.AllowedChallengeTypes
            });
        }
        else
        {
            return Results.NotFound();
        }
    }
}
