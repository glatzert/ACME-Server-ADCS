using Microsoft.AspNetCore.Http;
using System.Net;
using Th11s.ACMEServer.Json;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.AspNetCore.Middleware;

public class AcmeUnauthorizedResponseHandler
{
    private readonly RequestDelegate _next;

    private readonly HashSet<int> _watchedStatusCodes = new()
    {
        (int)HttpStatusCode.Unauthorized,
        (int)HttpStatusCode.Forbidden
    };

    public AcmeUnauthorizedResponseHandler(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if(_watchedStatusCodes.Contains(context.Response.StatusCode))
        {
            context.Response.ContentType = "application/problem+json";

            if(context.Items.TryGetValue("acme-error", out var acmeErrorObject) && acmeErrorObject is AcmeError acmeError)
            {
                if(acmeError.HttpStatusCode.HasValue)
                {
                    context.Response.StatusCode = acmeError.HttpStatusCode.Value;
                }

                await context.Response.WriteAsJsonAsync(new HttpModel.AcmeError(acmeError), AcmeJsonDefaults.DefaultJsonSerializerOptions, contentType: "application/problem+json");
            }
            else
            {
                await context.Response.WriteAsJsonAsync(new HttpModel.AcmeError(AcmeErrors.Unauthorized()), AcmeJsonDefaults.DefaultJsonSerializerOptions, contentType: "application/problem+json");
            }
        }

    }
}
