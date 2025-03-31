using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Th11s.ACMEServer.AspNetCore.Extensions
{
    internal static class HttpContextExtensions
    {
        public static void AddLocationResponseHeader(this HttpContext httpContext, LinkGenerator linkGenerator, string endpointName, object? values)
        {
            httpContext.Response.OnStarting(() =>
            {
                var locationUrl = linkGenerator.GetUriByName(httpContext, endpointName, values);
                httpContext.Response.Headers.Append("Location", locationUrl);

                return Task.CompletedTask;
            });
        }

        public static void AddLinkResponseHeader(this HttpContext httpContext, LinkGenerator linkGenerator, string relation, string endpointName, object? values)
        {
            httpContext.Response.OnStarting(() =>
            {
                var linkHeaderUrl = linkGenerator.GetUriByName(httpContext, endpointName, values);
                var linkHeader = $"<{linkHeaderUrl}>;rel=\"{relation}\"";
                httpContext.Response.Headers.Append("Link", linkHeader);
                return Task.CompletedTask;
            });
        }
    }
}
