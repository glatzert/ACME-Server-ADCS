using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.AspNetCore.Middleware;

public class AcmeExceptionHandlerMiddlerware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AcmeExceptionHandlerMiddlerware> _logger;

    public AcmeExceptionHandlerMiddlerware(RequestDelegate next, ILogger<AcmeExceptionHandlerMiddlerware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (exception is AcmeBaseException acmeBaseException)
            {
                _logger.LogDebug(exception, $"Detected {acmeBaseException.GetType()}. Converting to BadRequest.");
#if DEBUG
                _logger.LogError(exception, "AcmeException detected.");
#endif

                if (acmeBaseException is AcmeErrorException aee)
                {
                    context.Response.StatusCode = aee.Error.HttpStatusCode ?? (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsJsonAsync(new HttpModel.AcmeError(aee.Error), new JsonSerializerOptions(), contentType: "application/problem+json");
                }


                else if (acmeBaseException is AcmeException acmeException)
                {


                    if (acmeException is ConflictRequestException)
                        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    else if (acmeException is NotAllowedException)
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    else if (acmeException is NotFoundException)
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    else
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                    await context.Response.WriteAsJsonAsync(new HttpModel.AcmeError(acmeException), new JsonSerializerOptions(), contentType: "application/problem+json");
                }
            }

            else
            {
                _logger.LogError(exception, "Unhandled exception in request.");
                throw;
            }
        }
    }
}