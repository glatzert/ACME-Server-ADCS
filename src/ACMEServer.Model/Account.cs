using System.Runtime.Serialization;
using Th11s.ACMEServer.Model.Extensions;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Model;

[Serializable]
public class Account : IVersioned, ISerializable
{
    public Account(Jwk jwk, 
        IEnumerable<string>? contacts, 
        DateTimeOffset? tosAccepted,
        AcmeJwsToken? externalAccountBinding)
    {
        AccountId = GuidString.NewValue();

        Jwk = jwk;
        Contacts = contacts?.ToList();
        TOSAccepted = tosAccepted;
        ExternalAccountBinding = externalAccountBinding;
    }

    public string AccountId { get; }
    public AccountStatus Status { get; set; }

    public Jwk Jwk { get; }

    public List<string>? Contacts { get; set; }
    public DateTimeOffset? TOSAccepted { get; set; }

    public AcmeJwsToken? ExternalAccountBinding { get; set; }
    public bool HasExternalAccountBinding => ExternalAccountBinding is not null;


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
        Status = info.GetEnumFromString<AccountStatus>(nameof(Status));
        Jwk = info.GetRequiredValue<Jwk>(nameof(Jwk));

        Contacts = info.GetValue<List<string>>(nameof(Contacts));
        TOSAccepted = info.TryGetValue<DateTimeOffset?>(nameof(TOSAccepted));
        ExternalAccountBinding = info.TryGetValue<AcmeJwsToken?>(nameof(ExternalAccountBinding));

        Version = info.GetInt64(nameof(Version));
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info is null)
            throw new ArgumentNullException(nameof(info));

        info.AddValue("SerializationVersion", 1);

        info.AddValue(nameof(AccountId), AccountId);
        info.AddValue(nameof(Status), Status.ToString());
        info.AddValue(nameof(Jwk), Jwk);

        info.AddValue(nameof(Contacts), Contacts);
        info.AddValue(nameof(TOSAccepted), TOSAccepted);
        info.AddValue(nameof(ExternalAccountBinding), ExternalAccountBinding);

        info.AddValue(nameof(Version), Version);
    }
}
