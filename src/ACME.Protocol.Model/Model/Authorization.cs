using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Model.Extensions;

namespace TGIT.ACME.Protocol.Model
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
            AuthorizationId = GuidString.NewValue();
            Challenges = new List<Challenge>();

            Order = order ?? throw new ArgumentNullException(nameof(order));
            Order.Authorizations.Add(this);

            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            Expires = expires;
        }

        public string AuthorizationId { get; }
        public AuthorizationStatus Status { get; set; }

        public Order Order {
            get => _order ?? throw new NotInitializedException();
            internal set => _order = value;
        }
        
        public Identifier Identifier { get; }
        public bool IsWildcard => Identifier.IsWildcard;

        public DateTimeOffset Expires { get; set; }

        public List<Challenge> Challenges { get; private set; }
        

        public Challenge? GetChallenge(string challengeId)
            => Challenges.FirstOrDefault(x => x.ChallengeId == challengeId);

        public void SelectChallenge(Challenge challenge)
            => Challenges.RemoveAll(c => c != challenge);

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
            Status = (AuthorizationStatus)info.GetInt32(nameof(Status));

            Identifier = info.GetRequiredValue<Identifier>(nameof(Identifier));
            Expires = info.GetValue<DateTimeOffset>(nameof(Expires));

            Challenges = info.GetRequiredValue<List<Challenge>>(nameof(Challenges));
            foreach (var challenge in Challenges)
                challenge.Authorization = this;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("SerializationVersion", 1);

            info.AddValue(nameof(AuthorizationId), AuthorizationId);
            info.AddValue(nameof(Status), Status);
            
            info.AddValue(nameof(Identifier), Identifier);
            info.AddValue(nameof(Expires), Expires);
            
            info.AddValue(nameof(Challenges), Challenges);
        }
    }
}
