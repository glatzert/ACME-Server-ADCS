using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.Globalization;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.AspNetCore.Endpoints;

public static class RenewalInfoEndpoints
{
    public static IEndpointRouteBuilder MapRenewalInfoEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/renewal-info/{certificateId}", GetRenewalInformation)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.RenewalInfo);

        return builder;
    }

    public static async Task<IResult> GetRenewalInformation(
        string certificateId,
        HttpContext httpContext,
        IRenewalInfoService renewalInfoService,
        IOptions<ACMEServerOptions> options)
    {
        var acmeRequest = httpContext.GetAcmeRequest();
        var renewalInfo = await renewalInfoService.GetRenewalInformationAsync(
            httpContext.User.GetAccountId(),
            new(certificateId),
            httpContext.RequestAborted);

        httpContext.Response.Headers.RetryAfter = ((int)options.Value.ARI.RetryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        return Results.Ok(renewalInfo);
    }
}
