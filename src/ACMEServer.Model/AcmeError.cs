using System.Runtime.Serialization;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Extensions;

namespace Th11s.ACMEServer.Model;

[Serializable]
public class AcmeError : ISerializable
{
    private string? _type;
    private string? _detail;

    private AcmeError() { }

    public AcmeError(string type, string detail, Identifier? identifier = null, IEnumerable<AcmeError>? subErrors = null)
    {
        Type = type;

        if (!type.Contains(":"))
            Type = "urn:ietf:params:acme:error:" + type;

        Detail = detail;
        Identifier = identifier;
        SubErrors = subErrors?.ToList();
    }

    public string Type
    {
        get => _type ?? throw new NotInitializedException();
        private set => _type = value;
    }

    public string Detail
    {
        get => _detail ?? throw new NotInitializedException();
        set => _detail = value;
    }

    public Identifier? Identifier { get; }

    public List<AcmeError>? SubErrors { get; }



    // --- Serialization Methods --- //

    protected AcmeError(SerializationInfo info, StreamingContext streamingContext)
    {
        if (info is null)
            throw new ArgumentNullException(nameof(info));

        Type = info.GetRequiredString(nameof(Type));
        Detail = info.GetRequiredString(nameof(Detail));

        Identifier = info.TryGetValue<Identifier>(nameof(Identifier));
        SubErrors = info.TryGetValue<List<AcmeError>>(nameof(SubErrors));
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info is null)
            throw new ArgumentNullException(nameof(info));

        info.AddValue("SerializationVersion", 1);

        info.AddValue(nameof(Type), Type);
        info.AddValue(nameof(Detail), Detail);

        if (Identifier != null)
            info.AddValue(nameof(Identifier), Identifier);

        if (SubErrors != null)
            info.AddValue(nameof(SubErrors), SubErrors);
    }
}

public static class AcmeErrors
{
    public static AcmeError AccountDoesNotExist()
        => new AcmeError(
            "accountDoesNotExist", 
            "The request specified an account that does not exist"
        );

    public static AcmeError AlreadyRevoked()
        => new AcmeError(
            "alreadyRevoked", 
            "Certificate has already been revoked."
        );

    public static AcmeError BadCSR(string detail)
        => new AcmeError(
            "badCSR", 
            $"The CSR is unacceptable: {detail}"
        );

    public static AcmeError BadNonce()
        => new AcmeError(
            "badNonce", 
            "The client sent an unaccaptable anti-replay nonce."
        );

    public static AcmeError BadPublicKey(string detail)
        => new AcmeError(
            "badPublicKey", 
            $"The JWS was signed by a public key the server does not support: {detail}");

    public static AcmeError BadRevocationReason(string detail)
        => new AcmeError(
            "badRevocationReason", 
            $"The revocation reason provided is not allowed by the server: {detail}"
        );

    public static AcmeError BadSignatureAlgorithm(string detail)
        => new AcmeError(
            "badSignatureAlgorithm",
            $"The JWS was signed with an algorithm the server does not support: {detail}"
        );

    public static AcmeError CAA()
        => new AcmeError(
            "caa",
            "Certification Authority Authorization (CAA) records forbid the CA from issuing a certificate."
        );


    public static AcmeError Compound(string detail, IEnumerable<AcmeError> subErrors)
        => new AcmeError(
            "compound", 
            "Multiple errors occured.", 
            subErrors: subErrors);


    public static AcmeError Connection(Identifier identifier)
        => new AcmeError(
            "connection",
            "The server could not connect to the validation target.",
            identifier: identifier
            );

    public static AcmeError Dns(Identifier identifier)
        => new AcmeError(
            "dns",
            "There was a problem with a DNS query during identifier validation.",
            identifier: identifier
            );

    public static AcmeError ExternalAccountRequired()
        => new AcmeError(
            "externalAccountRequired",
            "The request must include a value for the \"externalAccountBinding\" field."
            );

    public static AcmeError IncorrectResponse(Identifier identifier)
        => new AcmeError(
            "incorrectResponse",
            "Response received didn't match the challenge's requirements",
            identifier: identifier
            );

    public static AcmeError InvalidContact(string contact)
        => new AcmeError(
            "invalidContact",
            $"A contact URL for an account was invalid: {contact}"
            );

    public static AcmeError MalformedRequest(string detail)
        => new AcmeError(
            "malformedRequest", 
            $"The request message was malformed: {detail}"
            );

    public static AcmeError OrderNotReady()
        => new AcmeError(
            "orderNotReady",
            "The request attempted to finalize an order that is not ready."
            );

    public static AcmeError RateLimited()
        => new AcmeError(
            "rateLimited",
            "The request exceeds a rate limit."
            );

    public static AcmeError RejectedIdentifier(Identifier identifier)
        => new AcmeError(
            "rejectedIdentifier",
            "The server will not issue certificates for the identifier.", 
            identifier);

    public static AcmeError ServerInternal()
        => new AcmeError(
            "serverInternal",
            "The server experienced an internal error."
            );

    public static AcmeError Tls(Identifier identifier)
        => new AcmeError(
            "tls",
            "The server received a TLS error during validation.",
            identifier: identifier
            );

    public static AcmeError Unauthorized()
        => new AcmeError(
            "unauthorized",
            "The client lacks sufficient authorization."
            );
    public static AcmeError UnsupportedContact(string contact)
        => new AcmeError(
            "unsupportedContact",
            $"A contact URL for an account used an unsupported protocol scheme: {contact}"
            );
    public static AcmeError UnsupportedIdentifier(string detail, Identifier identifier)
        => new AcmeError(
            "unsupportedIdentifier",
            "An identifier is of an unsupported type.", 
            identifier
            );
    public static AcmeError UserActionRequired(string detail)
        => new AcmeError(
            "userActionRequired", 
            detail
            );
}