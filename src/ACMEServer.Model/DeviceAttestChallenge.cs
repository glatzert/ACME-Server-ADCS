using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

public class DeviceAttestChallenge : TokenChallenge
{
    public DeviceAttestChallenge(Authorization authorization)
        : base(authorization, ChallengeTypes.DeviceAttest01)
    { }

    public DeviceAttestChallenge(
        ChallengeId challengeId,
        ChallengeStatus status,
        string type,
        string token,
        string? payload,
        DateTimeOffset? validated,
        AcmeError? error
    ) : base(challengeId, status, type, token, validated, error)
    {
        Payload = payload;
    }

    public string? Payload { get; set; }
}
