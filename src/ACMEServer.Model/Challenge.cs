using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

// TODO: split into multiple challenge type classes
public abstract class Challenge
{
    private static readonly Dictionary<ChallengeStatus, ChallengeStatus[]> _validStatusTransitions =
        new()
        {
            { ChallengeStatus.Pending, new [] { ChallengeStatus.Processing } },
            { ChallengeStatus.Processing, new [] { ChallengeStatus.Processing, ChallengeStatus.Invalid, ChallengeStatus.Valid } }
        };

    private Authorization? _authorization;

    
    protected Challenge(Authorization authorization, string type)
    {
        if (!ChallengeTypes.AllTypes.Contains(type))
            throw new InvalidOperationException($"Unknown ChallengeType {type}");

        ChallengeId = new();
        Status = ChallengeStatus.Pending;

        Type = type;

        Authorization = authorization;
        Authorization.Challenges.Add(this);
    }

    public ChallengeId ChallengeId { get; }
    public ChallengeStatus Status { get; set; }

    public string Type { get; }

    public Authorization Authorization
    {
        get => _authorization ?? throw new NotInitializedException();
        internal set => _authorization = value;
    }

    public DateTimeOffset? Validated { get; set; }
    public bool IsValid => Status == ChallengeStatus.Valid;

    public AcmeError? Error { get; set; }


    public void SetStatus(ChallengeStatus nextStatus)
    {
        if (!_validStatusTransitions.TryGetValue(Status, out var value))
            throw new ConflictRequestException(nextStatus);
        if (!value.Contains(nextStatus))
            throw new ConflictRequestException(nextStatus);

        Status = nextStatus;
    }


    protected Challenge(
        ChallengeId challengeId,
        ChallengeStatus status,
        string type,
        DateTimeOffset? validated,
        AcmeError? error
    ) {
        ChallengeId = challengeId;
        Status = status;
        Type = type;
        Validated = validated;
        Error = error;
    }
}

public class TokenChallenge : Challenge
{
    public TokenChallenge(Authorization authorization, string type)
        : base(authorization, type)
    {
        if (!ChallengeTypes.TokenChallenges.Contains(type))
            throw new InvalidOperationException($"Unknown TokenChallengeType {type}");

        Token = CryptoString.NewValue();
    }

    public string Token { get; }

    public TokenChallenge(
        ChallengeId challengeId,
        ChallengeStatus status,
        string type,
        string token,
        DateTimeOffset? validated,
        AcmeError? error
    ) : base(challengeId, status, type, validated, error)
    {
        Token = token;
    }
}

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