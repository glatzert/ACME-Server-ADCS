using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Th11s.ACMEServer.HttpModel.Services;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.AspNetCore.Middleware;

public class AcmeRequestMiddleware
{
    private readonly RequestDelegate _next;

    public AcmeRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAcmeRequestProvider requestProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestProvider);

        // Handle nonce?

        if (HttpMethods.IsPost(context.Request.Method))
        {
            var acmeRequest = await JsonSerializer.DeserializeAsync<AcmeJwsToken>(context.Request.Body);

            if (acmeRequest is not null)
            {
                context.Features.Set<AcmeRequest>(new(acmeRequest));
            }

            // TODO: Authorize and validate the request here?
        }

        await _next(context);
    }
}

public record AcmeRequest(AcmeJwsToken Request);

