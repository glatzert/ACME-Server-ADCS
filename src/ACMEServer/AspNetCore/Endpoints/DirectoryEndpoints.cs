using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.AspNetCore.Endpoints.Metadata;
using Th11s.ACMEServer.Configuration;

namespace Th11s.ACMEServer.AspNetCore.Endpoints
{
    public static class DirectoryEndpoints
    {
        public static IEndpointRouteBuilder MapDirectoryEndpoints(this IEndpointRouteBuilder builder)
        {
            builder.MapGet("/", GetDirectory)
                .WithName(EndpointNames.Directory)
                .WithMetadata(new SkipNonceGeneration());
            builder.MapGet("/directory", GetDirectory)
                .WithMetadata(new SkipNonceGeneration());

            return builder;
        }

        public static IResult GetDirectory(
            IOptions<ACMEServerOptions> options,
            LinkGenerator linkGenerator, 
            HttpContext httpContext)
        {
            return Results.Ok(new HttpModel.Directory
            {
                NewNonce = linkGenerator.GetUriByName(httpContext, EndpointNames.NewNonce, null),
                NewAccount = linkGenerator.GetUriByName(httpContext, EndpointNames.NewAccount, null),
                NewOrder = linkGenerator.GetUriByName(httpContext, EndpointNames.NewOrder, null),
                NewAuthz = linkGenerator.GetUriByName(httpContext, EndpointNames.NewAuthz, null),
                RevokeCert = linkGenerator.GetUriByName(httpContext, EndpointNames.RevokeCert, null),
                KeyChange = linkGenerator.GetUriByName(httpContext, EndpointNames.KeyChange, null),
                Meta = new HttpModel.DirectoryMetadata
                {
                    ExternalAccountRequired = false,
                    CAAIdentities = null,
                    TermsOfService = options.Value.TOS.RequireAgreement ? options.Value.TOS.Url : null,
                    Website = options.Value.WebsiteUrl
                }
            });
        }
    }
}
