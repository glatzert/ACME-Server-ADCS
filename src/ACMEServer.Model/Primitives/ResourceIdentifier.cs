using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Th11s.ACMEServer.Model.Primitives;

[DebuggerStepThrough]
public abstract record class ResourceIdentifier
{
    private static Regex IdentifierRegex { get; } = new("^[A-Za-z0-9_\\.-]{5,64}$", RegexOptions.Compiled);

    public string Value { get; init; }

    public ResourceIdentifier()
    { 
        Value = GuidString.NewValue();
    }

    public ResourceIdentifier(string value)
    {
        if(!IdentifierRegex.IsMatch(value))
        {
            throw new ArgumentException("Invalid identifier format", nameof(value));
        }

        Value = value;
    }

    public override string ToString() => Value;
    public override int GetHashCode() => HashCode.Combine(Value);
}