using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Th11s.ACMEServer.AspNetCore.Endpoints;
using Th11s.ACMEServer.AspNetCore.Middleware;

namespace Th11s.ACMEServer.AspNetCore
{
    public static class AcmeServerExtension
    {
        public static WebApplication UseAcmeServer(this WebApplication app)
        {
            app.UseRouting();

            app.UseMiddleware<AcmeExceptionHandlerMiddlerware>();
            app.UseMiddleware<AcmeUnauthorizedResponseHandler>();
            app.UseMiddleware<AcmeRequestMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapDirectoryEndpoints();
            app.MapNonceEndpoints();
            app.MapAccountEndpoints();
            app.MapOrderEndpoints();

            // Add this endpoint to be availble to tests. It enables us to test middlewares without influence of the rest of the application.
            if(app.Environment.IsDevelopment())
            {
                app.MapPost("/test", () => Results.Ok("{}"))
                    .RequireAuthorization();
            }

            return app;
        }
    }
}
