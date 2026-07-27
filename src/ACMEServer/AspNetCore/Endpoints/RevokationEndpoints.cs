using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services;
using Payloads = Th11s.ACMEServer.HttpModel.Payloads;

namespace Th11s.ACMEServer.AspNetCore.Endpoints;

public static class RevokationEndpoints
{
    /// <summary>
    /// Maps the endpoints for certificate revokation.
    /// </summary>
    public static IEndpointRouteBuilder MapRevokationEndpoints(this IEndpointRouteBuilder builder)
    {
        // An revokation may be signed with an certificate private key or an account key, so we'll only call require authorization here.
        // This will make sure the kid or jwk are validated. Authorization will be checked in the DefaultRevokationService.
        builder.MapPost("/revoke-cert", RevokeCertificate)
            .RequireAuthorization()
            .WithName(EndpointNames.RevokeCert);

        return builder;
    }

    public static async Task<IResult> RevokeCertificate(
        HttpContext httpContext,
        IRevokationService revokationService)
    {
        var acmeRequest = httpContext.GetAcmeRequest();
        if (!acmeRequest.TryGetPayload<Payloads.RevokeCertificate>(out var payload) || payload is null)
        {
            throw AcmeErrors.MalformedRequest("Payload was empty or could not be read.").AsException();
        }

        await revokationService.RevokeCertificateAsync(acmeRequest, payload, httpContext.RequestAborted);
        return Results.Ok();
    }
}
