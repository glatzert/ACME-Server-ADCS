using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using Th11s.ACMEServer.AspNetCore.Endpoints;
using Th11s.ACMEServer.AspNetCore.Endpoints.Metadata;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Features;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Services;

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
        ILogger<AcmeRequestMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(context);

        var endpoint = context.GetEndpoint();
        AddNonceResponseHeader(context, endpoint, nonceService, logger);
        AddIndexLinkResponseHeader(context, linkGenerator, logger);


        if (HttpMethods.IsPost(context.Request.Method))
        {
            var acmeRequest = await JsonSerializer.DeserializeAsync<AcmeJwsToken>(context.Request.Body);

            if (acmeRequest is null)
            {
                // TODO: Return a 400 Bad Request response?
            }

            context.Features.Set<AcmeRequestFeature>(new(acmeRequest));

            // TODO: Authorize and validate the request here?
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

            logger.LogInformation($"Added Replay-Nonce: {newNonce.Token}");
        });
    }
}