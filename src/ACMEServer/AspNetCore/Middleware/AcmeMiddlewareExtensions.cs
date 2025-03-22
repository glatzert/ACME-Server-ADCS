using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Th11s.ACMEServer.AspNetCore.Endpoints;

namespace Th11s.ACMEServer.AspNetCore.Middleware
{
    public static class AcmeMiddlewareExtensions
    {
        public static WebApplication MapAcmeServer(this WebApplication app, string routePrefix = null)
        {
            var builder = app.UseMiddleware<AcmeRequestMiddleware>();

            IEndpointRouteBuilder routeBuilder = app;
            if(!string.IsNullOrWhiteSpace(routePrefix))
            {
                routeBuilder = string.IsNullOrWhiteSpace(routePrefix)
                    ? app.MapGroup("/")
                    : app.MapGroup(routePrefix);
            }
            
            routeBuilder.MapDirectoryEndpoints();

            return app;
        }
    }
}
