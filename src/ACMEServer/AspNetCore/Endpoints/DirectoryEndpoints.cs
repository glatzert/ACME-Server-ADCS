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
            var directoryGroup = builder.MapGroup("/")
                .WithMetadata(new SkipNonceGeneration());
            
            builder.MapGet("/", GetDirectory)
                .WithName(EndpointNames.Directory);
            builder.MapGet("/directory", GetDirectory);

            return builder;
        }

        public static IResult GetDirectory(
            IOptions<ACMEServerOptions> options,
            LinkGenerator linkGenerator, 
            HttpContext httpContext)
        {
            return Results.Ok(new HttpModel.Directory
            {
                NewNonce = linkGenerator.GetUriByName(httpContext, "NewNonce", null),
                NewAccount = linkGenerator.GetUriByName(httpContext, "NewAccount", null),
                NewOrder = linkGenerator.GetUriByName(httpContext, "NewOrder", null),
                NewAuthz = linkGenerator.GetUriByName(httpContext, "NewAuthz", null),
                RevokeCert = linkGenerator.GetUriByName(httpContext, "RevokeCert", null),
                KeyChange = linkGenerator.GetUriByName(httpContext, "KeyChange", null),
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
