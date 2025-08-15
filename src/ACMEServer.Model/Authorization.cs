using System.Runtime.Serialization;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Extensions;

namespace Th11s.ACMEServer.Model
{
    [Serializable]
    public class Authorization : ISerializable
    {
        private static readonly Dictionary<AuthorizationStatus, AuthorizationStatus[]> _validStatusTransitions =
            new Dictionary<AuthorizationStatus, AuthorizationStatus[]>
            {
                { AuthorizationStatus.Pending, new [] { AuthorizationStatus.Invalid, AuthorizationStatus.Expired, AuthorizationStatus.Valid } },
                { AuthorizationStatus.Valid, new [] { AuthorizationStatus.Revoked, AuthorizationStatus.Deactivated, AuthorizationStatus.Expired } }
            };

        private Order? _order;

        public Authorization(Order order, Identifier identifier, DateTimeOffset expires)
        {
            ArgumentNullException.ThrowIfNull(order, nameof(order));
            ArgumentNullException.ThrowIfNull(identifier, nameof(identifier));

            AuthorizationId = GuidString.NewValue();
            Challenges = new List<Challenge>();

            Order = order;
            Order.Authorizations.Add(this);

            if(identifier.Type == "dns" && identifier.Value.StartsWith("*."))
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

        public string AuthorizationId { get; }
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


        public Challenge? GetChallenge(string challengeId)
            => Challenges.FirstOrDefault(x => x.ChallengeId == challengeId);

        public void SelectChallenge(Challenge challenge)
            => Challenges = new() { challenge };

        public void ClearChallenges()
            => Challenges.Clear();


        public void SetStatus(AuthorizationStatus nextStatus)
        {
            if (!_validStatusTransitions.ContainsKey(Status))
                throw new InvalidOperationException($"Cannot do challenge status transition from '{Status}'.");

            if (!_validStatusTransitions[Status].Contains(nextStatus))
                throw new InvalidOperationException($"Cannot do challenge status transition from '{Status}' to {nextStatus}.");

            Status = nextStatus;
        }



        // --- Serialization Methods --- //

        protected Authorization(SerializationInfo info, StreamingContext streamingContext)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            AuthorizationId = info.GetRequiredString(nameof(AuthorizationId));
            Status = info.GetEnumFromString<AuthorizationStatus>(nameof(Status));

            Identifier = info.GetRequiredValue<Identifier>(nameof(Identifier));
            IsWildcard = info.GetValue<bool>(nameof(IsWildcard));
            Expires = info.GetValue<DateTimeOffset>(nameof(Expires));

            Challenges = info.GetRequiredValue<List<Challenge>>(nameof(Challenges));
            foreach (var challenge in Challenges)
                challenge.Authorization = this;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info, nameof(info));

            info.AddValue("SerializationVersion", 1);

            info.AddValue(nameof(AuthorizationId), AuthorizationId);
            info.AddValue(nameof(Status), Status.ToString());

            info.AddValue(nameof(Identifier), Identifier);
            info.AddValue(nameof(IsWildcard), IsWildcard);
            info.AddValue(nameof(Expires), Expires);

            info.AddValue(nameof(Challenges), Challenges);
        }
    }
}
