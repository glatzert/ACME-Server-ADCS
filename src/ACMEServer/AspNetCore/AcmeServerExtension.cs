using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Th11s.ACMEServer.AspNetCore.Endpoints;
using Th11s.ACMEServer.AspNetCore.Middleware;

namespace Th11s.ACMEServer.AspNetCore
{
    public static class AcmeServerExtension
    {
        public static WebApplication MapAcmeServer(this WebApplication app)
        {
            app.UseMiddleware<AcmeExceptionHandlerMiddlerware>();
            app.UseMiddleware<AcmeRequestMiddleware>();

            app.MapDirectoryEndpoints();
            app.MapNonceEndpoints();
            app.MapAccountEndpoints();

            return app;
        }
    }
}
