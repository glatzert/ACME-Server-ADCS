using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Extensions;

namespace Th11s.ACMEServer.Model;

[Serializable]
public class Identifier : ISerializable
{
    private string? _type;
    private string? _value;

    [SetsRequiredMembers]
    public Identifier(string type, string value)
    {
        Type = type;
        Value = value;
    }

    public required string Type
    {
        get => _type ?? throw new NotInitializedException();
        init
        {
            // TODO: This might not be true for all identifer types
            var normalizedType = value?.Trim().ToLowerInvariant();
            _type = normalizedType;
        }
    }

    public required string Value
    {
        get => _value ?? throw new NotInitializedException();
        set => _value = !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    public bool IsWildcard
        => Value.StartsWith("*", StringComparison.InvariantCulture);


    public Dictionary<string, string>? Metadata { get; set; }


    // --- Serialization Methods --- //

    protected Identifier(SerializationInfo info, StreamingContext streamingContext)
    {
        if (info is null)
            throw new ArgumentNullException(nameof(info));

        Type = info.GetRequiredString(nameof(Type));
        Value = info.GetRequiredString(nameof(Value));

        Metadata = info.TryGetValue<Dictionary<string, string>>(nameof(Metadata));
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info is null)
            throw new ArgumentNullException(nameof(info));

        info.AddValue("SerializationVersion", 1);

        info.AddValue(nameof(Type), Type);
        info.AddValue(nameof(Value), Value);

        if(Metadata is not null)
        {
            info.AddValue(nameof(Metadata), Metadata);
        }
    }

    public string? GetExpectedPublicKey(this Identifier identifier)
    {
        if (identifier.Metadata?.TryGetValue(MetadataKeys.PublicKey, out var publicKey) == true)
        {
            return publicKey;
        }

        return null;
    }

    public static class MetadataKeys
    {
        public const string PublicKey = "expected-public-key";
    }
}
