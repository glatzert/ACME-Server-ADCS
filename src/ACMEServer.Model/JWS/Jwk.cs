using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.Model.JWS;

public class Jwk
{
    private JsonWebKey? _jsonWebKey;

    private string? _jsonKeyHash;
    private string? _json;

    private Jwk() { }

    public Jwk(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentNullException(nameof(json));

        Json = json;
    }

    public string Json
    {
        get => _json ?? throw new NotInitializedException();
        set => _json = value;
    }

    public JsonWebKey SecurityKey
    {
        get
        {
            _jsonWebKey ??= JsonWebKey.Create(Json);

            if (_jsonWebKey.KeySize == 0)
            {
                throw new MalformedRequestException(
                    "JWK does not contain a valid key size."
                );
            }

            return _jsonWebKey;
        }
    } 

    public string KeyHash
        => _jsonKeyHash ??= Base64UrlEncoder.Encode(
            SecurityKey.ComputeJwkThumbprint()
        );
}
