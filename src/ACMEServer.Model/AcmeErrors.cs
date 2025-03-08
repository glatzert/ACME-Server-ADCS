namespace Th11s.ACMEServer.Model;

public static class AcmeErrors
{
    public static AcmeError AccountDoesNotExist()
        => new(
            "accountDoesNotExist", 
            "The request specified an account that does not exist"
        );

    public static AcmeError AlreadyRevoked()
        => new(
            "alreadyRevoked", 
            "Certificate has already been revoked."
        );

    public static AcmeError BadCSR(string detail)
        => new(
            "badCSR", 
            $"The CSR is unacceptable: {detail}"
        );

    public static AcmeError BadNonce()
        => new(
            "badNonce", 
            "The client sent an unaccaptable anti-replay nonce."
        );

    public static AcmeError BadPublicKey(string detail)
        => new(
            "badPublicKey", 
            $"The JWS was signed by a public key the server does not support: {detail}");

    public static AcmeError BadRevocationReason(string detail)
        => new(
            "badRevocationReason", 
            $"The revocation reason provided is not allowed by the server: {detail}"
        );

    public static AcmeError BadSignatureAlgorithm(string detail)
        => new(
            "badSignatureAlgorithm",
            $"The JWS was signed with an algorithm the server does not support: {detail}"
        );

    public static AcmeError CAA()
        => new(
            "caa",
            "Certification Authority Authorization (CAA) records forbid the CA from issuing a certificate."
        );


    public static AcmeError Compound(string detail, IEnumerable<AcmeError> subErrors)
        => new(
            "compound", 
            "Multiple errors occured.", 
            subErrors: subErrors);


    public static AcmeError Connection(Identifier identifier)
        => new(
            "connection",
            "The server could not connect to the validation target.",
            identifier: identifier
            );

    public static AcmeError Dns(Identifier identifier)
        => new(
            "dns",
            "There was a problem with a DNS query during identifier validation.",
            identifier: identifier
            );

    public static AcmeError ExternalAccountRequired()
        => new(
            "externalAccountRequired",
            "The request must include a value for the \"externalAccountBinding\" field."
            );

    public static AcmeError IncorrectResponse(Identifier identifier)
        => new(
            "incorrectResponse",
            "Response received didn't match the challenge's requirements",
            identifier: identifier
            );

    public static AcmeError InvalidContact(string contact)
        => new(
            "invalidContact",
            $"A contact URL for an account was invalid: {contact}"
            );

    public static AcmeError MalformedRequest(string detail)
        => new(
            "malformedRequest", 
            $"The request message was malformed: {detail}"
            );

    public static AcmeError OrderNotReady()
        => new(
            "orderNotReady",
            "The request attempted to finalize an order that is not ready."
            );

    public static AcmeError RateLimited()
        => new(
            "rateLimited",
            "The request exceeds a rate limit."
            );

    public static AcmeError RejectedIdentifier(Identifier identifier)
        => new(
            "rejectedIdentifier",
            "The server will not issue certificates for the identifier.", 
            identifier);

    public static AcmeError ServerInternal()
        => new(
            "serverInternal",
            "The server experienced an internal error."
            );

    public static AcmeError Tls(Identifier identifier)
        => new(
            "tls",
            "The server received a TLS error during validation.",
            identifier: identifier
            );

    public static AcmeError Unauthorized()
        => new(
            "unauthorized",
            "The client lacks sufficient authorization."
            );
    public static AcmeError UnsupportedContact(string contact)
        => new(
            "unsupportedContact",
            $"A contact URL for an account used an unsupported protocol scheme: {contact}"
            );
    public static AcmeError UnsupportedIdentifier(string detail, Identifier identifier)
        => new(
            "unsupportedIdentifier",
            "An identifier is of an unsupported type.", 
            identifier
            );
    public static AcmeError UserActionRequired(string detail)
        => new(
            "userActionRequired", 
            detail
            );
}