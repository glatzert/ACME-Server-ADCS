using System.Diagnostics;

namespace Th11s.ACMEServer.Model.Primitives;

[DebuggerDisplay("Authorization: {Value}")]
[DebuggerStepThrough]
public record class AuthorizationId : ResourceIdentifier
{
    public AuthorizationId() : base()
    { }

    public AuthorizationId(string value) : base(value)
    { }
}