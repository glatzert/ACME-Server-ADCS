using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

public class Account : IVersioned
{
    public Account(Jwk jwk,
        IEnumerable<string>? contacts,
        DateTimeOffset? tosAccepted,
        AcmeJwsToken? externalAccountBinding)
    {
        AccountId = new AccountId();

        Jwk = jwk;
        Contacts = contacts?.ToList();
        TOSAccepted = tosAccepted;
        ExternalAccountBinding = externalAccountBinding;
    }

    public AccountId AccountId { get; }
    public AccountStatus Status { get; set; }

    public Jwk Jwk { get; set; }

    public List<string>? Contacts { get; set; }
    public DateTimeOffset? TOSAccepted { get; set; }

    public AcmeJwsToken? ExternalAccountBinding { get; set; }
    public bool HasExternalAccountBinding => ExternalAccountBinding is not null;


    /// <summary>
    /// Concurrency Token
    /// </summary>
    public long Version { get; set; }



    // --- Serialization Methods --- //
    public Account(
        AccountId accountId,
        AccountStatus accountStatus,
        Jwk jwk,

        List<string> contacts,
        DateTimeOffset? tosAccepted,
        AcmeJwsToken? externalAccountBinding,

        long version
    )
    {
        AccountId = accountId;
        Status = accountStatus;
        Jwk = jwk;

        Contacts = [.. contacts];

        TOSAccepted = tosAccepted;
        ExternalAccountBinding = externalAccountBinding;

        Version = version;
    }
}
