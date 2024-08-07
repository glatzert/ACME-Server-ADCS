﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Th11s.ACMEServer.AspNetCore.Extensions
{
    internal static class HttpContextExtensions
    {
        public static string GetProtocol(this HttpContext context)
        {
            return context.Request.IsHttps ? "https" : "http";
        }

        public static void AddOrMerge(this IHeaderDictionary headers, string headerName, StringValues values)
        {
            if (!headers.TryGetValue(headerName, out var currentValues))
            {
                headers.Add(headerName, values);
                return;
            }

            headers[headerName] = new StringValues(currentValues.Union(values).ToArray());
        }
    }
}
