using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.AspNetCore.Endpoints;

public static class RenewalInfoEndpoints
{
    public static IEndpointRouteBuilder MapRenewalEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/renewal-info", GetRenewalInformation)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.RenewalInfo);

        return builder;
    }

    public static async Task<IResult> GetRenewalInformation(
        HttpContext httpContext,
        IRenewalInfoService renewalInfoService)
    {
        var acmeRequest = httpContext.GetAcmeRequest();
        var renewalInfo = await renewalInfoService.GetRenewalInformationAsync(acmeRequest, httpContext.RequestAborted);

        // TODO: Retry-After should be defined by the administrator.
        return Results.Ok(renewalInfo);
    }
}
