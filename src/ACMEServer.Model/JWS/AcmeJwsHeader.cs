using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text.Json.Serialization;
using Th11s.ACMEServer.HttpModel.Converters;
using Th11s.ACMEServer.Model.Features;

namespace Th11s.ACMEServer.Model.JWS;

public class AcmeJwsHeader
{
    public required string Alg { get; set; }

    public string? Kid { get; set; }

    [JsonConverter(typeof(JwkConverter))]
    public Jwk? Jwk { get; set; }


    public string? Nonce { get; set; }
    public string? Url { get; set; }


    public static ValueTask<AcmeJwsHeader> BindAsync(HttpContext httpContext, ParameterInfo parameterInfo)
    {
        var header = httpContext.Features.Get<AcmeRequestFeature>()?.Request.AcmeHeader ?? throw new InvalidOperationException();
        return ValueTask.FromResult(header);
    }
}

public static class AcmeJwsHeaderExtensions
{
    public static string GetAccountId(this AcmeJwsHeader header)
    {
        if (header.Kid == null)
        {
            throw new InvalidOperationException();
        }

        return header.Kid.Split('/').Last();
    }
}