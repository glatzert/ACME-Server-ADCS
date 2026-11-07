using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

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
