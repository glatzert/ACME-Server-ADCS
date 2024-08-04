using Microsoft.AspNetCore.Builder;

namespace Th11s.ACMEServer.AspNetCore.Middleware
{
    public static class AcmeMiddlewareExtensions
    {
        public static IApplicationBuilder UseAcmeServer(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AcmeMiddleware>();
        }
    }
}
