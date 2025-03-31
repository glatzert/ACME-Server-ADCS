using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Th11s.ACMEServer.AspNetCore.Endpoints
{
    public static class NonceEndpoints
    {
        /// <summary>
        /// Maps the endpoints for nonce generation.
        /// Generally the nonce endpoints do nothing by themselves, since nonce generation is handled via middleware (see <see cref="Middleware.AcmeRequestMiddleware"/>).
        /// </summary>
        public static IEndpointRouteBuilder MapNonceEndpoints(this IEndpointRouteBuilder builder)
        {
            builder.MapGet("/new-nonce", () => Results.NoContent())
                .WithName(EndpointNames.NewNonce);
            builder.MapMethods("/new-nonce", [HttpMethods.Head], () => Results.Ok());

            return builder;
        }
    }
}
