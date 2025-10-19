using System.Net;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

public static class AcmeErrors
{
    public const string AcmeUrn = "urn:ietf:params:acme:error";
    public const string CustomUrn = "urn:th11s:acme:error";

    public static AcmeError AccountDoesNotExist()
        => new(
            $"{AcmeUrn}:accountDoesNotExist", 
            "The request specified an account that does not exist"
        );

    public static AcmeError JwkAlreadyInUse()
        => new AcmeError(
            $"{CustomUrn}:jwkAlreadyInUse",
            "The JWK used to sign the inner payload is already in use"
        )
        {
            HttpStatusCode = (int)HttpStatusCode.Conflict
        };

    public static AcmeError AlreadyRevoked()
        => new(
            $"{AcmeUrn}:alreadyRevoked", 
            "Certificate has already been revoked."
        );

    public static AcmeError BadCSR(string detail)
        => new(
            $"{AcmeUrn}:badCSR", 
            $"The CSR is unacceptable: {detail}"
        );

    public static AcmeError BadNonce()
        => new(
            $"{AcmeUrn}:badNonce", 
            "The client sent an unaccaptable anti-replay nonce."
        );

    public static AcmeError BadPublicKey(string detail)
        => new(
            $"{AcmeUrn}:badPublicKey", 
            $"The JWS was signed by a public key the server does not support: {detail}");

    public static AcmeError BadRevocationReason(string detail)
        => new(
            $"{AcmeUrn}:badRevocationReason", 
            $"The revocation reason provided is not allowed by the server: {detail}"
        );

    public static AcmeError BadSignatureAlgorithm(string detail, string[] supportedAlgorithms)
        => new(
            $"{AcmeUrn}:badSignatureAlgorithm",
            $"The JWS was signed with an algorithm the server does not support: {detail}"
        )
        { 
            AdditionalFields = { ["algorithms"] = supportedAlgorithms } 
        };

    public static AcmeError CAA()
        => new(
            $"{AcmeUrn}:caa",
            "Certification Authority Authorization (CAA) records forbid the CA from issuing a certificate."
        );


    public static AcmeError Compound(IEnumerable<AcmeError> subErrors)
        => new(
            $"{AcmeUrn}:compound", 
            "Multiple errors occured.", 
            subErrors: subErrors);


    public static AcmeError Connection(Identifier identifier, string? detail = null)
        => new(
            $"{AcmeUrn}:connection",
            detail ?? "The server could not connect to the validation target.",
            identifier: identifier
            );

    public static AcmeError Dns(Identifier identifier)
        => new(
            $"{AcmeUrn}:dns",
            "There was a problem with a DNS query during identifier validation.",
            identifier: identifier
            );

    public static AcmeError ExternalAccountRequired()
        => new(
            $"{AcmeUrn}:externalAccountRequired",
            "The request must include a value for the \"externalAccountBinding\" field."
            );

    public static AcmeError IncorrectResponse(Identifier identifier, string? detail = null)
        => new(
            $"{AcmeUrn}:incorrectResponse",
            detail ?? "Response received didn't match the challenge's requirements",
            identifier: identifier
            );

    public static AcmeError InvalidContact(string contact)
        => new(
            $"{AcmeUrn}:invalidContact",
            $"A contact URL for an account was invalid: {contact}"
            );

    public static AcmeError MalformedRequest(string detail)
        => new(
            $"{AcmeUrn}:malformed", 
            $"The request message was malformed: {detail}"
            );

    public static AcmeError OrderNotReady()
        => new(
            $"{AcmeUrn}:orderNotReady",
            "The request attempted to finalize an order that is not ready."
            );

    public static AcmeError RateLimited()
        => new(
            $"{AcmeUrn}:rateLimited",
            "The request exceeds a rate limit."
            );

    public static AcmeError RejectedIdentifier(Identifier identifier)
        => new(
            $"{AcmeUrn}:rejectedIdentifier",
            "The server will not issue certificates for the identifier.", 
            identifier);

    public static AcmeError ServerInternal()
        => new(
            $"{AcmeUrn}:serverInternal",
            "The server experienced an internal error."
            );

    public static AcmeError Tls(Identifier identifier)
        => new(
            $"{AcmeUrn}:tls",
            "The server received a TLS error during validation.",
            identifier: identifier
            );

    public static AcmeError Unauthorized()
        => new(
            $"{AcmeUrn}:unauthorized",
            "The client lacks sufficient authorization."
            )
        { 
            HttpStatusCode = (int)HttpStatusCode.Unauthorized 
        };

    public static AcmeError InvalidSignature()
        => new(
            $"{CustomUrn}:badSignature",
            "The signature is invalid."
            )
        {
            HttpStatusCode = (int)HttpStatusCode.Unauthorized
        };

    public static AcmeError UnsupportedContact(string contact)
        => new(
            $"{AcmeUrn}:unsupportedContact",
            $"A contact URL for an account used an unsupported protocol scheme: {contact}"
            );
    public static AcmeError UnsupportedIdentifier(Identifier identifier)
        => new(
            $"{AcmeUrn}:unsupportedIdentifier",
            "An identifier is of an unsupported type.", 
            identifier
            );
    public static AcmeError UserActionRequired(string detail)
        => new(
            $"{AcmeUrn}:userActionRequired", 
            detail
            );


    public static AcmeError ExternalAccountBindingFailed(string detail)
        => new(
            $"{CustomUrn}:externalAccountBindingFailed",
            detail
            );


    public static AcmeError NoIssuanceProfile()
        => new(
            $"{CustomUrn}:noIssuanceProfile",
            "No issuance profile was found that supports the requested identifiers."
            );

    public static AcmeError UnsupportedProfile(ProfileName profileName)
        => new AcmeError(
            //TODO: Check if there is an official URN for this
            $"{CustomUrn}:unsupportedIssuanceProfile",
            $"The requested issuance profile {profileName} is not supported."
            );

    public static AcmeError InvalidProfile(ProfileName profileName)
        => new(
            //TODO: Check if there is an official URN for this
            $"{AcmeUrn}:invalidProfile",
            $"The requested issuance profile {profileName} is does not support all requested identifiers."
        );
}