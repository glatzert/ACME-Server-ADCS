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
        builder.MapPost("/renewal-info/{certificateId}", GetRenewalInformation)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.RenewalInfo);

        return builder;
    }

    public static async Task<IResult> GetRenewalInformation(
        string certificateId,
        HttpContext httpContext,
        IRenewalInfoService renewalInfoService)
    {
        var acmeRequest = httpContext.GetAcmeRequest();
        var renewalInfo = await renewalInfoService.GetRenewalInformationAsync(
            httpContext.User.GetAccountId(),
            new(certificateId),
            httpContext.RequestAborted);

        // TODO: Retry-After should be definable by the administrator.
        return Results.Ok(renewalInfo);
    }
}
