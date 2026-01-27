using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Extensions;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

public class Authorization
{
    private static readonly Dictionary<AuthorizationStatus, AuthorizationStatus[]> _validStatusTransitions =
        new()
        {
            { AuthorizationStatus.Pending, new [] { AuthorizationStatus.Invalid, AuthorizationStatus.Expired, AuthorizationStatus.Valid } },
            { AuthorizationStatus.Valid, new [] { AuthorizationStatus.Revoked, AuthorizationStatus.Deactivated, AuthorizationStatus.Expired } }
        };

    private Order? _order;

    public Authorization(Order order, Identifier identifier, DateTimeOffset expires)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));
        ArgumentNullException.ThrowIfNull(identifier, nameof(identifier));

        AuthorizationId = new();
        Challenges = new List<Challenge>();

        Order = order;
        Order.Authorizations.Add(this);

        if (identifier.IsWildcard())
        {
            IsWildcard = true;
            // Remove the leading "*." from the identifier value for wildcard DNS identifiers
            Identifier = new Identifier(identifier.Type, identifier.Value[2..]);
        }
        else
        {
            Identifier = identifier;
        }

        Expires = expires;
    }

    public AuthorizationId AuthorizationId { get; }
    public AuthorizationStatus Status { get; set; }

    public Order Order
    {
        get => _order ?? throw new NotInitializedException();
        internal set => _order = value;
    }

    public Identifier Identifier { get; }
    public bool IsWildcard { get; }

    public DateTimeOffset Expires { get; set; }

    public List<Challenge> Challenges { get; private set; }


    public Challenge? GetChallenge(ChallengeId challengeId)
        => Challenges.FirstOrDefault(x => x.ChallengeId == challengeId);

    public void SelectChallenge(Challenge challenge)
        => Challenges = [challenge];

    public void ClearChallenges()
        => Challenges.Clear();


    public void SetStatus(AuthorizationStatus nextStatus)
    {
        if (!_validStatusTransitions.TryGetValue(Status, out var value))
            throw new InvalidOperationException($"Cannot do challenge status transition from '{Status}'.");

        if (!value.Contains(nextStatus))
            throw new InvalidOperationException($"Cannot do challenge status transition from '{Status}' to {nextStatus}.");

        Status = nextStatus;
    }



    // --- Serialization Methods --- //
    public Authorization(
        AuthorizationId authorizationId,
        AuthorizationStatus status,
        Identifier identifier,
        bool isWildcard,
        DateTimeOffset expires,
        List<Challenge> challenges
    )
    {
        AuthorizationId = authorizationId;
        Status = status;
        Identifier = identifier;
        IsWildcard = isWildcard;
        Expires = expires;
        Challenges = challenges;

        foreach (var challenge in Challenges)
            challenge.Authorization = this;
    }
}
