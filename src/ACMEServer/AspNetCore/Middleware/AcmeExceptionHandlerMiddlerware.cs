﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Th11s.ACMEServer.Json;
using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.AspNetCore.Middleware;

public class AcmeExceptionHandlerMiddlerware(RequestDelegate next, ILogger<AcmeExceptionHandlerMiddlerware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<AcmeExceptionHandlerMiddlerware> _logger = logger;

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
                
                HttpModel.AcmeError acmeError;

                if (acmeBaseException is AcmeErrorException aee)
                {
                    context.Response.StatusCode = aee.Error.HttpStatusCode ?? (int)HttpStatusCode.BadRequest;
                    acmeError = new HttpModel.AcmeError(aee.Error);
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

                    acmeError = new HttpModel.AcmeError(acmeException);
                }
                else // this case should not be reached - it will be handled, when the AcmeException is removed.
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    acmeError = new HttpModel.AcmeError("internal", "An internal server error occurred.");
                }

                _logger.LogDebug(exception, "ACME Error of type {exceptionType} and will be send with status code {statusCode}.", acmeBaseException.GetType(), context.Response.StatusCode);
                LogAcmeError(acmeError);

                await context.Response.WriteAsJsonAsync(acmeError, AcmeJsonDefaults.DefaultJsonSerializerOptions, contentType: "application/problem+json");
            }

            else
            {
                _logger.LogError(exception, "Unhandled exception in request.");
                throw;
            }
        }
    }

    private void LogAcmeError(HttpModel.AcmeError acmeError)
    {
        if (acmeError.Identifier is not null)
        {
            var identifier = $"{acmeError.Identifier.Type}:{acmeError.Identifier.Value}";
            _logger.LogDebug("ACME Error data for '{identifier}': '{type}', '{detail}'", identifier, acmeError.Type, acmeError.Detail);
        }
        else
        {
            _logger.LogDebug("ACME Error data: '{type}', '{detail}'", acmeError.Type, acmeError.Detail);
        }

        if (acmeError.Subproblems is not null)
        {
            _logger.LogDebug("ACME Error contains {count} subproblems.", acmeError.Subproblems.Count);
            foreach (var subproblem in acmeError.Subproblems)
            {
                LogAcmeError(subproblem);
            }
        }
    }
}