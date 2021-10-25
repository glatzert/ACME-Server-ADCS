using Microsoft.IdentityModel.Tokens;
using System;
using System.Runtime.Serialization;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Model.Extensions;

namespace TGIT.ACME.Protocol.Model
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
