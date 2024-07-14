using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

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
