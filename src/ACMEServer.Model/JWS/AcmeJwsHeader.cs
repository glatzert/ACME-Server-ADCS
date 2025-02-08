using System.Text.Json.Serialization;
using Th11s.ACMEServer.HttpModel.Converters;

namespace Th11s.ACMEServer.Model.JWS;

public class AcmeJwsHeader
{
    public string? Alg { get; set; }

    public string? Kid { get; set; }

    [JsonConverter(typeof(JwkConverter))]
    public Jwk? Jwk { get; set; }


    public string? Nonce { get; set; }
    public string? Url { get; set; }
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