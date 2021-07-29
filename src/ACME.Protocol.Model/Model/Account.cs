using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TGIT.ACME.Protocol.Model.Extensions;

namespace TGIT.ACME.Protocol.Model
{
    [Serializable]
    public class Account : IVersioned, ISerializable
    {
        public Account(Jwk jwk, IEnumerable<string>? contacts, DateTimeOffset? tosAccepted)
        {
            AccountId = GuidString.NewValue();

            Jwk = jwk;
            Contacts = contacts?.ToList();
            TOSAccepted = tosAccepted;
        }

        public string AccountId { get; }
        public AccountStatus Status { get; private set; }

        public Jwk Jwk { get; }

        public List<string>? Contacts { get; private set; }
        public DateTimeOffset? TOSAccepted { get; private set; }

        /// <summary>
        /// Concurrency Token
        /// </summary>
        public long Version { get; set; }



        // --- Serialization Methods --- //

        protected Account(SerializationInfo info, StreamingContext streamingContext)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            AccountId = info.GetRequiredString(nameof(AccountId));
            Status = (AccountStatus)info.GetInt32(nameof(Status));
            Jwk = info.GetRequiredValue<Jwk>(nameof(Jwk));

            Contacts = info.GetValue<List<string>>(nameof(Contacts));
            TOSAccepted = info.TryGetValue<DateTimeOffset?>(nameof(TOSAccepted));

            Version = info.GetInt64(nameof(Version));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("SerializationVersion", 1);

            info.AddValue(nameof(AccountId), AccountId);
            info.AddValue(nameof(Status), Status);
            info.AddValue(nameof(Jwk), Jwk);

            info.AddValue(nameof(Contacts), Contacts);
            info.AddValue(nameof(TOSAccepted), TOSAccepted);

            info.AddValue(nameof(Version), Version);
        }
    }
}
