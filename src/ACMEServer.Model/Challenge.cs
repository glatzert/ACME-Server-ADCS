using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

[Serializable]
// TODO: split into multiple challenge type classes
public class Challenge
{
    private static readonly Dictionary<ChallengeStatus, ChallengeStatus[]> _validStatusTransitions =
        new()
        {
            { ChallengeStatus.Pending, new [] { ChallengeStatus.Processing } },
            { ChallengeStatus.Processing, new [] { ChallengeStatus.Processing, ChallengeStatus.Invalid, ChallengeStatus.Valid } }
        };

    private Authorization? _authorization;

    
    public Challenge(Authorization authorization, string type)
    {
        if (!ChallengeTypes.AllTypes.Contains(type))
            throw new InvalidOperationException($"Unknown ChallengeType {type}");

        ChallengeId = new();
        Status = ChallengeStatus.Pending;

        Type = type;
        Token = CryptoString.NewValue();

        Authorization = authorization;
        Authorization.Challenges.Add(this);
    }

    public ChallengeId ChallengeId { get; }
    public ChallengeStatus Status { get; set; }

    public string Type { get; }
    public string Token { get; }

    public Authorization Authorization
    {
        get => _authorization ?? throw new NotInitializedException();
        internal set => _authorization = value;
    }

    public DateTimeOffset? Validated { get; set; }
    public bool IsValid => Status == ChallengeStatus.Valid;

    public string? Payload { get; set; }
    public AcmeError? Error { get; set; }


    public void SetStatus(ChallengeStatus nextStatus)
    {
        if (!_validStatusTransitions.TryGetValue(Status, out var value))
            throw new ConflictRequestException(nextStatus);
        if (!value.Contains(nextStatus))
            throw new ConflictRequestException(nextStatus);

        Status = nextStatus;
    }



    // --- Serialization Methods --- //
    public Challenge(
        ChallengeId challengeId,
        ChallengeStatus status,
        string type,
        string token,
        string? payload,
        DateTimeOffset? validated,
        AcmeError? error
    ) {
        ChallengeId = challengeId;
        Status = status;
        Type = type;
        Token = token;
        Payload = payload;
        Validated = validated;
        Error = error;
    }
}
