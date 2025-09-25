using System.Diagnostics;

namespace Th11s.ACMEServer.Model.Primitives;

[DebuggerDisplay("Challenge: {Value}")]
[DebuggerStepThrough]
public record class ChallengeId : ResourceIdentifier
{
    public ChallengeId() : base()
    { }

    public ChallengeId(string value) : base(value)
    { }
}