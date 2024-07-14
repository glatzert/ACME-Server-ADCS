using System.Runtime.Serialization;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Extensions;

namespace Th11s.ACMEServer.Model
{
    [Serializable]
    public class Challenge : ISerializable
    {
        private static readonly Dictionary<ChallengeStatus, ChallengeStatus[]> _validStatusTransitions =
            new Dictionary<ChallengeStatus, ChallengeStatus[]>
            {
                { ChallengeStatus.Pending, new [] { ChallengeStatus.Processing } },
                { ChallengeStatus.Processing, new [] { ChallengeStatus.Processing, ChallengeStatus.Invalid, ChallengeStatus.Valid } }
            };

        private Authorization? _authorization;

        public Challenge(Authorization authorization, string type)
        {
            if (!ChallengeTypes.AllTypes.Contains(type))
                throw new InvalidOperationException($"Unknown ChallengeType {type}");

            ChallengeId = GuidString.NewValue();
            Status = ChallengeStatus.Pending;

            Type = type;
            Token = CryptoString.NewValue();

            Authorization = authorization;
            Authorization.Challenges.Add(this);
        }

        public string ChallengeId { get; }
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

        public AcmeError? Error { get; set; }


        public void SetStatus(ChallengeStatus nextStatus)
        {
            if (!_validStatusTransitions.ContainsKey(Status))
                throw new ConflictRequestException(nextStatus);
            if (!_validStatusTransitions[Status].Contains(nextStatus))
                throw new ConflictRequestException(nextStatus);

            Status = nextStatus;
        }



        // --- Serialization Methods --- //

        protected Challenge(SerializationInfo info, StreamingContext streamingContext)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            ChallengeId = info.GetRequiredString(nameof(ChallengeId));
            Status = info.GetEnumFromString<ChallengeStatus>(nameof(Status));

            Type = info.GetRequiredString(nameof(Type));
            Token = info.GetRequiredString(nameof(Token));

            Validated = info.TryGetValue<DateTimeOffset?>(nameof(Validated));
            Error = info.TryGetValue<AcmeError?>(nameof(Error));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("SerializationVersion", 1);

            info.AddValue(nameof(ChallengeId), ChallengeId);
            info.AddValue(nameof(Status), Status.ToString());

            info.AddValue(nameof(Type), Type);
            info.AddValue(nameof(Token), Token);

            info.AddValue(nameof(Validated), Validated);
            info.AddValue(nameof(Error), Error);
        }
    }
}
