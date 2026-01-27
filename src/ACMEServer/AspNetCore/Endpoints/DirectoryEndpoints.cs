using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.AspNetCore.Endpoints.Metadata;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;

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
        IOptions<HashSet<ProfileName>> profileNames,
        LinkGenerator linkGenerator, 
        HttpContext httpContext)
    {
        return Results.Ok(new HttpModel.Directory
        {
            NewNonce = linkGenerator.GetUriByName(httpContext, EndpointNames.NewNonce, null),
            NewAccount = linkGenerator.GetUriByName(httpContext, EndpointNames.NewAccount, null),
            NewOrder = linkGenerator.GetUriByName(httpContext, EndpointNames.NewOrder, null),
            NewAuthz = linkGenerator.GetUriByName(httpContext, EndpointNames.NewAuthz, null),
            RevokeCert = options.Value.SupportsRevokation 
                ? linkGenerator.GetUriByName(httpContext, EndpointNames.RevokeCert, null)
                : null,
            KeyChange = linkGenerator.GetUriByName(httpContext, EndpointNames.KeyChange, null),
            Meta = new HttpModel.DirectoryMetadata
            {
                ExternalAccountRequired = options.Value.ExternalAccountBinding?.Required == true,
                // TODO: For some reason, this can contain duplicates, in another occasion this was a problem with loading from config and default arrays
                CAAIdentities = options.Value.CAAIdentities.Distinct().ToArray(),
                TermsOfService = options.Value.TOS.RequireAgreement ? options.Value.TOS.Url : null,
                Website = options.Value.WebsiteUrl,
                Profiles = profileNames.Value.ToDictionary(
                    profileName => profileName.ToString(),
                    profileName => linkGenerator.GetUriByName(httpContext, EndpointNames.Profile, new { profile = profileName.ToString() }))
            }
        });
    }


    public static IResult GetProfile(
        string profile,
        IOptionsSnapshot<ProfileConfiguration> profiles)
    {
        if(profiles.Get(profile) is { } profileConfiguration)
        {
            return Results.Ok(new HttpModel.ProfileMetadata
            {
                ProfileName = profileConfiguration.Name,
                ExternalAccountRequired = profileConfiguration.RequireExternalAccountBinding,
                SupportedIdentifierTypes = profileConfiguration.SupportedIdentifiers
            });
        }
        else
        {
            return Results.NotFound();
        }
    }
}
