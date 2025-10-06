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
        builder.MapPost("/revoke-cert", (Delegate)RevokeCertificate)
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
