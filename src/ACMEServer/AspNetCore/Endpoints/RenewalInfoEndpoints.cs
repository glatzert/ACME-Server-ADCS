using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.AspNetCore.Endpoints;

public static class RenewalInfoEndpoints
{
    public static IEndpointRouteBuilder MapRenewalInfoEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/renewal-info/{certificateId?}", GetRenewalInformation)
            .WithName(EndpointNames.RenewalInfo)
            .CacheOutput(p =>
            {
                p.Cache()
                 .SetVaryByRouteValue("certificateId")
                 .Expire(TimeSpan.FromMinutes(15));
            });

        return builder;
    }

    public static async Task<IResult> GetRenewalInformation(
        string? certificateId,
        HttpContext httpContext,
        IRenewalInfoService renewalInfoService,
        IOptions<ACMEServerOptions> options)
    {
        if (string.IsNullOrWhiteSpace(certificateId))
        {
            return Results.BadRequest(new { error = "invalidRequest", detail = "Missing certificateId" });
        }

        var renewalInfo = await renewalInfoService.GetRenewalInformationAsync(
            new(certificateId),
            httpContext.RequestAborted);

        httpContext.Response.Headers.RetryAfter = ((int)options.Value.ARI.RetryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        return Results.Ok(renewalInfo);
    }
}
