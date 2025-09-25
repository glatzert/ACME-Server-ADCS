using System.Diagnostics;

namespace Th11s.ACMEServer.Model.Primitives;

[DebuggerDisplay("Account: {Value}")]
[DebuggerStepThrough]
public record class AccountId : ResourceIdentifier
{
    public AccountId() : base()
    { }

    public AccountId(string value) : base(value)
    { }
}
