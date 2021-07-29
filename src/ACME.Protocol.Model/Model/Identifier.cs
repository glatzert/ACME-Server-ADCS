using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Model.Extensions;

namespace TGIT.ACME.Protocol.Model
{
    [Serializable]
    public class Identifier : ISerializable
    {
        private static readonly string[] _supportedTypes = new[] { "dns" };

        private string? _type;
        private string? _value;

        public Identifier(string type, string value)
        {
            Type = type;
            Value = value;
        } 

        public string Type
        {
            get => _type ?? throw new NotInitializedException();
            set
            {
                var normalizedType = value?.Trim().ToLowerInvariant();
                if (!_supportedTypes.Contains(normalizedType))
                    throw new MalformedRequestException($"Unsupported identifier type: {normalizedType}");

                _type = normalizedType;
            }
        }

        public string Value
        {
            get => _value ?? throw new NotInitializedException();
            set => _value = value?.Trim().ToLowerInvariant();
        }

        public bool IsWildcard
            => Value.StartsWith("*", StringComparison.InvariantCulture);




        // --- Serialization Methods --- //

        protected Identifier(SerializationInfo info, StreamingContext streamingContext)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            Type = info.GetRequiredString(nameof(Type));
            Value = info.GetRequiredString(nameof(Value));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("SerializationVersion", 1);

            info.AddValue(nameof(Type), Type);
            info.AddValue(nameof(Value), Value);
        }
    }
}
