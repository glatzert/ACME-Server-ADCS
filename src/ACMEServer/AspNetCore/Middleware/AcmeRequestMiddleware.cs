using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Th11s.ACMEServer.AspNetCore.Endpoints;
using Th11s.ACMEServer.AspNetCore.Endpoints.Metadata;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Features;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.RequestServices;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.AspNetCore.Middleware;

public class AcmeRequestMiddleware
{
    private readonly RequestDelegate _next;

    public AcmeRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context,
        LinkGenerator linkGenerator,
        INonceService nonceService,
        IRequestValidationService requestValidationService,
        ILogger<AcmeRequestMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(context);

        var endpoint = context.GetEndpoint();
        AddNonceResponseHeader(context, endpoint, nonceService, logger);
        AddIndexLinkResponseHeader(context, linkGenerator, logger);


        if (HttpMethods.IsPost(context.Request.Method))
        {
            AcmeJwsToken? acmeRequest = null;
            try
            {
                acmeRequest = await JsonSerializer.DeserializeAsync<AcmeJwsToken>(context.Request.Body);
            }
            catch (Exception)
            {
                throw AcmeErrors.MalformedRequest("Could not read JWS token from body").AsException();
            }

            if (acmeRequest is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            context.Features.Set<AcmeRequestFeature>(new(acmeRequest));

            await requestValidationService.ValidateRequestAsync(
                acmeRequest, 
                context.Request.GetDisplayUrl(), 
                context.RequestAborted);
        }

        await _next(context);
    }

    private void AddIndexLinkResponseHeader(HttpContext httpContext, LinkGenerator linkGenerator, ILogger<AcmeRequestMiddleware> logger)
    {
        httpContext.AddLinkResponseHeader(linkGenerator, "index", EndpointNames.Directory, null);
    }

    private void AddNonceResponseHeader(HttpContext httpContext, Endpoint? endpoint, INonceService nonceService, ILogger logger)
    {
        var nonceBlockers = endpoint?.Metadata.GetOrderedMetadata<SkipNonceGeneration>();
        if (nonceBlockers?.Any() == true)
        {
            return;
        }

        httpContext.Response.OnStarting(async () =>
        {
            var newNonce = await nonceService.CreateNonceAsync(httpContext.RequestAborted);
            httpContext.Response.Headers["Replay-Nonce"] = newNonce.Token;

            logger.LogDebug($"Added Replay-Nonce: {newNonce.Token}");
        });
    }
}