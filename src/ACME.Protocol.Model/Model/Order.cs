using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TGIT.ACME.Protocol.Model.Extensions;

namespace TGIT.ACME.Protocol.Model
{
    [Serializable]
    public class Order : IVersioned, ISerializable
    {
        private static readonly Dictionary<OrderStatus, OrderStatus[]> _validStatusTransitions =
            new Dictionary<OrderStatus, OrderStatus[]>
            {
                { OrderStatus.Pending, new [] { OrderStatus.Ready, OrderStatus.Invalid } },
                { OrderStatus.Ready, new [] { OrderStatus.Processing, OrderStatus.Invalid } },
                { OrderStatus.Processing, new [] { OrderStatus.Valid, OrderStatus.Invalid } },
            };

        public Order(Account account, IEnumerable<Identifier> identifiers)
        {
            OrderId = GuidString.NewValue();
            Status = OrderStatus.Pending;

            AccountId = account.AccountId;

            Identifiers = new List<Identifier>(identifiers);
            Authorizations = new List<Authorization>();
        }

        public string OrderId { get; }
        public string AccountId { get; }

        public OrderStatus Status { get; private set; }
        
        public List<Identifier> Identifiers { get; private set; }
        public List<Authorization> Authorizations { get; private set; }
        
        public DateTimeOffset? NotBefore { get; set; }
        public DateTimeOffset? NotAfter { get; set; }
        public DateTimeOffset? Expires { get; set; }

        public AcmeError? Error { get; set; }

        public string? CertificateSigningRequest { get; set; }
        public byte[]? Certificate { get; set; }


        /// <summary>
        /// Concurrency Token
        /// </summary>
        public long Version { get; set; }

        public Authorization? GetAuthorization(string authId)
            => Authorizations.FirstOrDefault(x => x.AuthorizationId == authId);

        public void SetStatus(OrderStatus nextStatus)
        {
            if (!_validStatusTransitions.ContainsKey(Status))
                throw new InvalidOperationException($"Cannot do challenge status transition from '{Status}'.");

            if (!_validStatusTransitions[Status].Contains(nextStatus))
                throw new InvalidOperationException($"Cannot do challenge status transition from '{Status}' to {nextStatus}.");

            Status = nextStatus;
        }

        public void SetStatusFromAuthorizations()
        {
            if (Authorizations.All(a => a.Status == AuthorizationStatus.Valid))
                SetStatus(OrderStatus.Ready);

            if (Authorizations.Any(a => a.Status.IsInvalid()))
                SetStatus(OrderStatus.Invalid);
        }



        // --- Serialization Methods --- //

        protected Order(SerializationInfo info, StreamingContext streamingContext)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            OrderId = info.GetRequiredString(nameof(OrderId));
            AccountId = info.GetRequiredString(nameof(AccountId));

            Status = (OrderStatus)info.GetInt32(nameof(Status));

            Identifiers = info.GetRequiredValue<List<Identifier>>(nameof(Identifiers));
            Authorizations = info.GetRequiredValue<List<Authorization>>(nameof(Authorizations));

            foreach (var auth in Authorizations)
                auth.Order = this;
            
            NotBefore = info.TryGetValue<DateTimeOffset?>(nameof(NotBefore));
            NotAfter = info.TryGetValue<DateTimeOffset?>(nameof(NotAfter));
            Expires = info.TryGetValue<DateTimeOffset?>(nameof(Expires));

            Error = info.TryGetValue<AcmeError?>(nameof(Error));
            Version = info.GetInt64(nameof(Version));

            CertificateSigningRequest = info.TryGetValue<string?>(nameof(CertificateSigningRequest));
            Certificate = info.TryGetValue<byte[]?>(nameof(Certificate));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("SerializationVersion", 1);

            info.AddValue(nameof(OrderId), OrderId);
            info.AddValue(nameof(AccountId), AccountId);

            info.AddValue(nameof(Status), Status);

            info.AddValue(nameof(Identifiers), Identifiers);
            info.AddValue(nameof(Authorizations), Authorizations);
            
            info.AddValue(nameof(NotBefore), NotBefore);
            info.AddValue(nameof(NotAfter), NotAfter);
            info.AddValue(nameof(Expires), Expires);

            info.AddValue(nameof(Error), Error);
            info.AddValue(nameof(Version), Version);

            if (CertificateSigningRequest != null)
                info.AddValue(nameof(CertificateSigningRequest), CertificateSigningRequest);
            if (Certificate != null)
                info.AddValue(nameof(Certificate), Certificate);
        }
    }
}
