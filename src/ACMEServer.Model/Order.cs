using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

public class Order : IVersioned
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> _validStatusTransitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            { OrderStatus.Pending, new [] { OrderStatus.Ready, OrderStatus.Invalid } },
            { OrderStatus.Ready, new [] { OrderStatus.Processing, OrderStatus.Invalid } },
            { OrderStatus.Processing, new [] { OrderStatus.Valid, OrderStatus.Invalid } },
        };

    public Order(AccountId accountId, IEnumerable<Identifier> identifiers)
    {
        OrderId = new();
        Status = OrderStatus.Pending;

        AccountId = accountId;

        Identifiers = [.. identifiers];
        Authorizations = [];
    }

    public OrderId OrderId { get; }
    public AccountId AccountId { get; }

    public OrderStatus Status { get; private set; }

    public List<Identifier> Identifiers { get; private set; }
    public List<Authorization> Authorizations { get; private set; }

    public DateTimeOffset? NotBefore { get; set; }
    public DateTimeOffset? NotAfter { get; set; }
    public DateTimeOffset? Expires { get; set; }

    public ProfileName Profile { get; set; }

    public AcmeError? Error { get; set; }

    public string? CertificateSigningRequest { get; set; }
    public CertificateId? CertificateId { get; set; }

    /// <summary>
    /// This contains the expected subject public key info for orders that use permanent-identifiers and device-attest-01.
    /// The value is a base64url-encoded SubjectPublicKeyInfo structure as per RFC 5280.
    /// </summary>
    public string? ExpectedPublicKey {
        get => field;
        set {
            if (field != null && field != value)
            {
                throw AcmeErrors.MalformedRequest("There were multiple conflicting sources for an expected public key").AsException();
            }

            field = value;
        } 
    }

    /// <summary>
    /// Concurrency Token
    /// </summary>
    public long Version { get; set; }

    public Authorization? GetAuthorization(AuthorizationId authId)
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
        if (Authorizations.Count != 0 && Authorizations.All(a => a.Status == AuthorizationStatus.Valid))
            SetStatus(OrderStatus.Ready);

        if (Authorizations.Any(a => a.Status.IsInvalid()))
            SetStatus(OrderStatus.Invalid);
    }



    // --- Serialization Methods --- //
    public Order(
        OrderId orderId, 
        AccountId accountId,
        OrderStatus orderStatus, 

        List<Identifier> identifiers,
        List<Authorization> authorizations,
        
        DateTimeOffset? notBefore, 
        DateTimeOffset? notAfter, 
        DateTimeOffset? expires, 
        
        ProfileName profileName, 
        
        string? certificateSigningRequest, 
        CertificateId? certificateId, 
        
        AcmeError? error, 
        
        long version)
    {
        OrderId = orderId;
        AccountId = accountId;
        Status = orderStatus;

        Identifiers = [..identifiers];
        Authorizations = [..authorizations];

        foreach (var auth in Authorizations)
        {
            auth.Order = this;
        }

        NotBefore = notBefore;
        NotAfter = notAfter;
        Expires = expires;

        Profile = profileName;

        CertificateSigningRequest = certificateSigningRequest;
        CertificateId = certificateId;

        Error = error;
        Version = version;
    }
}
