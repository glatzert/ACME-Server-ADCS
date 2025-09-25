using System.Diagnostics;

namespace Th11s.ACMEServer.Model.Primitives;

[DebuggerDisplay("Order: {Value}")]
[DebuggerStepThrough]
public record class OrderId : ResourceIdentifier
{
    public OrderId() : base()
    { }

    public OrderId(string value) : base(value)
    { }
}