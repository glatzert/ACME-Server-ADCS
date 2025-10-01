using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Th11s.ACMEServer.Json;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.AspNetCore.Middleware;

public class AcmeUnauthorizedResponseHandler(RequestDelegate next, ILogger<AcmeUnauthorizedResponseHandler> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<AcmeUnauthorizedResponseHandler> _logger = logger;

    private readonly HashSet<int> _watchedStatusCodes =
    [
        (int)HttpStatusCode.Unauthorized,
        (int)HttpStatusCode.Forbidden
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if(_watchedStatusCodes.Contains(context.Response.StatusCode))
        {
            _logger.LogDebug("Handling response with status code {statusCode}", context.Response.StatusCode);

            context.Response.ContentType = "application/problem+json";

            // Especially TOS agreement uses this context item element to pass the error to be returned.
            if (context.Items.TryGetValue("acme-error", out var acmeErrorObject) && acmeErrorObject is AcmeError acmeError)
            {
                if(acmeError.HttpStatusCode.HasValue)
                {
                    context.Response.StatusCode = acmeError.HttpStatusCode.Value;
                }

                _logger.LogInformation("Rewrote unauthorized response to ACME error: {acmeError}", acmeError.Type);
                await context.Response.WriteAsJsonAsync(new HttpModel.AcmeError(acmeError), AcmeJsonDefaults.DefaultJsonSerializerOptions, contentType: "application/problem+json");
            }
            else
            {
                _logger.LogInformation("Rewrote response to generic ACME unauthorized error.");
                await context.Response.WriteAsJsonAsync(new HttpModel.AcmeError(AcmeErrors.Unauthorized()), AcmeJsonDefaults.DefaultJsonSerializerOptions, contentType: "application/problem+json");
            }
        }

    }
}
