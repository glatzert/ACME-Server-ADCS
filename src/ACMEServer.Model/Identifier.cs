using System.Diagnostics.CodeAnalysis;
using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.Model;

public class Identifier
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
            // TODO: This should probably happen in the minimal API endpoint.
            var normalizedType = value?.Trim();
            _type = normalizedType;
        }
    }

    public required string Value
    {
        get => _value ?? throw new NotInitializedException();
        set => _value = !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    public override string ToString()
    {
        return $"{Type}:{Value}";
    }

    
    public static class MetadataKeys
    {
        public const string PublicKey = "expected-public-key";
        public const string CAAValidationMehods = "caa-validation-methods";
    }
}
