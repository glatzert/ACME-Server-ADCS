using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.HttpModel.Services;

namespace Th11s.ACMEServer.AspNetCore.Middleware
{
    public class AcmeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AcmeRequestReader _requestReader;

        public AcmeMiddleware(RequestDelegate next, AcmeRequestReader requestReader)
        {
            _next = next;
            _requestReader = requestReader;
        }

        public async Task InvokeAsync(HttpContext context, IAcmeRequestProvider requestProvider)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (requestProvider is null)
                throw new ArgumentNullException(nameof(requestProvider));

            if (HttpMethods.IsPost(context.Request.Method))
            {
                var result = await _requestReader.ReadAcmeRequest(context.Request);
                requestProvider.Initialize(result);
            }

            await _next(context);
        }
    }

    public class AcmeRequestReader
    {
        public async Task<AcmeRawPostRequest> ReadAcmeRequest(HttpRequest request)
        {
            var result = await JsonSerializer.DeserializeAsync<AcmeRawPostRequest>(request.Body);
            return result;
        }
    }
}
