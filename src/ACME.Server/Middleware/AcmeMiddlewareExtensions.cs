using Microsoft.Extensions.DependencyInjection;
using TGIT.ACME.Server.Middleware;

namespace Microsoft.AspNetCore.Builder
{
    public static class AcmeMiddlewareExtensions
    {
        public static IApplicationBuilder UseAcmeServer(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AcmeMiddleware>();
        }
    }
}
