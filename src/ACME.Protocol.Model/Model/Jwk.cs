using Microsoft.IdentityModel.Tokens;
using TGIT.ACME.Protocol.Model.Exceptions;

namespace TGIT.ACME.Protocol.Model
{
    public class Jwk
    {
        private JsonWebKey? _jsonWebKey;
        
        private string? _jsonKeyHash;
        private string? _json;

        private Jwk() { }

        public Jwk(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new System.ArgumentNullException(nameof(json));

            Json = json;
        }

        public string Json {
            get => _json ?? throw new NotInitializedException(); 
            set => _json = value; 
        }

        public JsonWebKey SecurityKey
            => _jsonWebKey ??= JsonWebKey.Create(Json);

        public string KeyHash
            => _jsonKeyHash ??= Base64UrlEncoder.Encode(
                SecurityKey.ComputeJwkThumbprint()
            );
    }
}
