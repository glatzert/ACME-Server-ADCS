using Microsoft.IdentityModel.Tokens;
using System.Runtime.Serialization;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Extensions;

namespace Th11s.ACMEServer.Model
{
    [Serializable]
    public class Jwk : ISerializable
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


        // --- Serialization Methods --- //

        protected Jwk(SerializationInfo info, StreamingContext streamingContext)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            Json = info.GetRequiredString(nameof(Json));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue(nameof(Json), Json);
        }
    }
}
