using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Th11s.ACMEServer.AspNetCore.Endpoints;
using Th11s.ACMEServer.AspNetCore.Middleware;

namespace Th11s.ACMEServer.AspNetCore
{
    public static class AcmeServerExtension
    {
        public static WebApplication MapAcmeServer(this WebApplication app, string routePrefix = null)
        {
            app.UseMiddleware<AcmeExceptionHandlerMiddlerware>();
            app.UseMiddleware<AcmeRequestMiddleware>();

            IEndpointRouteBuilder routeBuilder = app;
            if (!string.IsNullOrWhiteSpace(routePrefix))
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
