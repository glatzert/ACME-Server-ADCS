using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer;

/// <summary>
/// Centralized logging messages for ACMEServer project using source-generated logging.
/// </summary>
public static partial class LogMessages
{
    #region CommonErrors (0-1000)

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Error,
        Message = "Profile configuration for profile '{Profile}' not found.")]
    public static partial void ProfileConfigurationNotFound(this ILogger logger, string profile);

    #endregion

    #region DefaultAuthorizationFactory (1000-1019)

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "No challenge types available for identifier {Identifier} and its metadata restrictions {AllowedChallengeTypes}")]
    public static partial void NoChallengeTypesAvailable(this ILogger logger, Identifier identifier, string allowedChallengeTypes);

    #endregion

    #region CsrValidator (1020-1049)

    [LoggerMessage(
        EventId = 1021,
        Level = LogLevel.Warning,
        Message = "Certificate signing request was null or empty.")]
    public static partial void CsrNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 1022,
        Level = LogLevel.Warning,
        Message = "Certificate signing request could not be decoded.")]
    public static partial void CsrDecodeFailed(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 1023,
        Level = LogLevel.Warning,
        Message = "CSR validation failed: Public key did not match expected key.")]
    public static partial void CsrPublicKeyMismatch(this ILogger logger);

    [LoggerMessage(
        EventId = 1024,
        Level = LogLevel.Warning,
        Message = "CSR validation failed: Not all subject alternative names are valid. Invalid SANs: {InvalidAlternativeNames}")]
    public static partial void CsrInvalidSans(this ILogger logger, string invalidAlternativeNames);

    [LoggerMessage(
        EventId = 1025,
        Level = LogLevel.Warning,
        Message = "CSR validation failed: Not all common names are valid. Invalid CNs: {InvalidCommonNames}")]
    public static partial void CsrInvalidCommonNames(this ILogger logger, string invalidCommonNames);

    [LoggerMessage(
        EventId = 1026,
        Level = LogLevel.Warning,
        Message = "CSR validation failed: Not all identifiers were used in the CSR. Unused identifiers: {UnusedIdentifiers}")]
    public static partial void CsrUnusedIdentifiers(this ILogger logger, string unusedIdentifiers);

    [LoggerMessage(
        EventId = 1027,
        Level = LogLevel.Warning,
        Message = "Validation of CSR failed with exception.")]
    public static partial void CsrValidationException(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 1028,
        Level = LogLevel.Debug,
        Message = "CSR Validation succeeded.")]
    public static partial void CsrValidationSucceeded(this ILogger logger);

    #endregion

    #region DeviceAttest01ChallengeValidator (1050-1079)

    [LoggerMessage(
        EventId = 1051,
        Level = LogLevel.Error,
        Message = "Remote validation for device-attest-01:Apple failed.")]
    public static partial void DeviceAttestRemoteValidationFailed(this ILogger logger);

    [LoggerMessage(
        EventId = 1052,
        Level = LogLevel.Debug,
        Message = "No remote validation URL configured for device-attest-01, skipping remote validation.")]
    public static partial void DeviceAttestNoRemoteValidation(this ILogger logger);

    [LoggerMessage(
        EventId = 1053,
        Level = LogLevel.Error,
        Message = "ChallengeValidation parameters did not contain a root certificate for device-attest-01:Apple. Validation not possible.")]
    public static partial void DeviceAttestNoRootCertificate(this ILogger logger);

    #endregion

    #region DefaultOrderService (1080-1119)

    [LoggerMessage(
        EventId = 1080,
        Level = LogLevel.Debug,
        Message = "No identifiers submitted for order creation.")]
    public static partial void NoIdentifiersSubmitted(this ILogger logger);

    [LoggerMessage(
        EventId = 1081,
        Level = LogLevel.Debug,
        Message = "CAA evaluation for identifier {Identifier} did not allow issuance.")]
    public static partial void CAAEvaluationFailed(this ILogger logger, Identifier identifier);

    [LoggerMessage(
        EventId = 1082,
        Level = LogLevel.Information,
        Message = "Created order {OrderId} for account {AccountId} with identifiers {Identifiers} and profile {Profile}")]
    public static partial void OrderCreated(this ILogger logger, OrderId orderId, AccountId accountId, string identifiers, ProfileName profile);

    [LoggerMessage(
        EventId = 1083,
        Level = LogLevel.Debug,
        Message = "Order {OrderId} is not valid. Cannot return certificate.")]
    public static partial void OrderNotValidForCertificate(this ILogger logger, OrderId orderId);

    [LoggerMessage(
        EventId = 1084,
        Level = LogLevel.Debug,
        Message = "Order {OrderId} is not pending. Cannot process challenge.")]
    public static partial void OrderNotPendingForChallenge(this ILogger logger, OrderId orderId);

    [LoggerMessage(
        EventId = 1085,
        Level = LogLevel.Debug,
        Message = "Challenge {ChallengeId} for authorization {AuthId} is not pending. Cannot process challenge.")]
    public static partial void ChallengeNotPending(this ILogger logger, ChallengeId challengeId, AuthorizationId authId);

    [LoggerMessage(
        EventId = 1086,
        Level = LogLevel.Information,
        Message = "Processing challenge {ChallengeId} for order {OrderId}")]
    public static partial void ProcessingChallenge(this ILogger logger, ChallengeId challengeId, OrderId orderId);

    #endregion

    #region DefaultAccountService (1120-1159)

    [LoggerMessage(
        EventId = 1120,
        Level = LogLevel.Warning,
        Message = "JWK is required in the protected header to create a new account.")]
    public static partial void JwkRequiredForNewAccount(this ILogger logger);

    [LoggerMessage(
        EventId = 1121,
        Level = LogLevel.Information,
        Message = "Terms of service agreement is required, but client did not agree to the terms of service.")]
    public static partial void TermsOfServiceNotAgreed(this ILogger logger);

    [LoggerMessage(
        EventId = 1122,
        Level = LogLevel.Warning,
        Message = "External account binding is required, but payload did not contain externalAccountBinding.")]
    public static partial void ExternalAccountBindingRequired(this ILogger logger);

    [LoggerMessage(
        EventId = 1123,
        Level = LogLevel.Debug,
        Message = "Payload contains externalAccountBinding. Validating ...")]
    public static partial void ValidatingExternalAccountBinding(this ILogger logger);

    [LoggerMessage(
        EventId = 1124,
        Level = LogLevel.Warning,
        Message = "ExternalAccountBinding validation failed.")]
    public static partial void ExternalAccountBindingValidationFailed(this ILogger logger);

    [LoggerMessage(
        EventId = 1125,
        Level = LogLevel.Warning,
        Message = "ExternalAccountBinding could not be validated. EAB not required, so it's ignored.")]
    public static partial void ExternalAccountBindingIgnored(this ILogger logger);

    [LoggerMessage(
        EventId = 1126,
        Level = LogLevel.Information,
        Message = "Creating new account with id {AccountId}")]
    public static partial void CreatingNewAccount(this ILogger logger, AccountId accountId);

    [LoggerMessage(
        EventId = 1127,
        Level = LogLevel.Debug,
        Message = "Updating contact information for account {AccountId}")]
    public static partial void UpdatingAccountContact(this ILogger logger, AccountId accountId);

    [LoggerMessage(
        EventId = 1128,
        Level = LogLevel.Debug,
        Message = "Updating TOS acceptance for account {AccountId}")]
    public static partial void UpdatingAccountTOS(this ILogger logger, AccountId accountId);

    [LoggerMessage(
        EventId = 1129,
        Level = LogLevel.Debug,
        Message = "Updating status for account {AccountId} to {Status}")]
    public static partial void UpdatingAccountStatus(this ILogger logger, AccountId accountId, AccountStatus status);

    [LoggerMessage(
        EventId = 1130,
        Level = LogLevel.Warning,
        Message = "Inner JWS did not contain a JWK.")]
    public static partial void InnerJwsMissingJwk(this ILogger logger);

    [LoggerMessage(
        EventId = 1131,
        Level = LogLevel.Warning,
        Message = "Inner JWS did not have a valid signature.")]
    public static partial void InnerJwsInvalidSignature(this ILogger logger);

    [LoggerMessage(
        EventId = 1132,
        Level = LogLevel.Warning,
        Message = "Inner JWS URL does not match outer JWS URL.")]
    public static partial void InnerJwsUrlMismatch(this ILogger logger);

    [LoggerMessage(
        EventId = 1133,
        Level = LogLevel.Warning,
        Message = "Inner JWS may not contain nonce.")]
    public static partial void InnerJwsContainsNonce(this ILogger logger);

    [LoggerMessage(
        EventId = 1134,
        Level = LogLevel.Warning,
        Message = "Payload did not contain the correct accountUrl")]
    public static partial void PayloadAccountUrlMismatch(this ILogger logger);

    [LoggerMessage(
        EventId = 1135,
        Level = LogLevel.Warning,
        Message = "Payload did not contain the correct old key.")]
    public static partial void PayloadOldKeyMismatch(this ILogger logger);

    [LoggerMessage(
        EventId = 1136,
        Level = LogLevel.Warning,
        Message = "The JWK used to change the account key was already known.")]
    public static partial void JwkAlreadyInUse(this ILogger logger);

    #endregion

    #region Http01ChallengeValidator (1160-1179)

    [LoggerMessage(
        EventId = 1160,
        Level = LogLevel.Information,
        Message = "Loaded http-01 challenge response from {ChallengeUrl}: {Content}")]
    public static partial void Http01ChallengeResponseLoaded(this ILogger logger, string challengeUrl, string content);

    [LoggerMessage(
        EventId = 1161,
        Level = LogLevel.Information,
        Message = "Could not load http-01 challenge response from {ChallengeUrl}")]
    public static partial void Http01ChallengeResponseFailed(this ILogger logger, string challengeUrl);

    #endregion

    #region Dns01ChallengeValidator (1180-1199)

    [LoggerMessage(
        EventId = 1180,
        Level = LogLevel.Information,
        Message = "Loaded dns-01 challenge response from {DnsRecordName}: {Contents}")]
    public static partial void Dns01ChallengeResponseLoaded(this ILogger logger, string dnsRecordName, string contents);

    [LoggerMessage(
        EventId = 1181,
        Level = LogLevel.Information,
        Message = "Could not load dns-01 challenge response from {DnsRecordName}")]
    public static partial void Dns01ChallengeResponseFailed(this ILogger logger, string dnsRecordName);

    #endregion


    #region CertificateIssuer (2000-2099)

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Debug,
        Message = "Try to issue certificate for CSR: {Csr}")]
    public static partial void TryIssueCertificate(this ILogger logger, string csr);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "Certificate has been issued.")]
    public static partial void CertificateIssued(this ILogger logger);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Error,
        Message = "Failed issuing certificate using Config {CaServer} and Template {TemplateName}.")]
    public static partial void FailedIssuingCertificate(this ILogger logger, string caServer, string templateName);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Error,
        Message = "Certificate could not be issued. ResponseCode: {SubmitResponseCode}.")]
    public static partial void CertificateIssuanceResponseCode(this ILogger logger, int submitResponseCode);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Error,
        Message = "Exception has been raised during certificate issuance.")]
    public static partial void CertificateIssuanceException(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Debug,
        Message = "Attempting to revoke certificate {Certificate}")]
    public static partial void AttemptRevokeCertificate(this ILogger logger, string certificate);

    [LoggerMessage(
        EventId = 2008,
        Level = LogLevel.Information,
        Message = "Certificate {SerialNumber} has been revoked.")]
    public static partial void CertificateRevoked(this ILogger logger, string serialNumber);

    [LoggerMessage(
        EventId = 2009,
        Level = LogLevel.Error,
        Message = "Failed revoking certificate {CertificateSerial} from {CaServer}.")]
    public static partial void FailedRevokingCertificate(this ILogger logger, string certificateSerial, string caServer);

    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Error,
        Message = "Exception has been raised during certificate revocation.")]
    public static partial void CertificateRevocationException(this ILogger logger, Exception ex);

    #endregion

    #region AcmeExceptionHandlerMiddleware (3000-3019)

    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Debug,
        Message = "ACME Error of type {ExceptionType} and will be send with status code {StatusCode}.")]
    public static partial void AcmeErrorOccurred(this ILogger logger, Type exceptionType, int statusCode, Exception ex);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Debug,
        Message = "ACME Error data for '{Identifier}': '{Type}', '{Detail}'")]
    public static partial void AcmeErrorDataWithIdentifier(this ILogger logger, string identifier, string? type, string? detail);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Debug,
        Message = "ACME Error data: '{Type}', '{Detail}'")]
    public static partial void AcmeErrorData(this ILogger logger, string? type, string? detail);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Debug,
        Message = "ACME Error contains {Count} subproblems.")]
    public static partial void AcmeErrorSubproblems(this ILogger logger, int count);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Error,
        Message = "Unhandled exception in request.")]
    public static partial void UnhandledExceptionInRequest(this ILogger logger, Exception ex);

    #endregion

    #region AcmeRequestMiddleware (3020-3039)

    [LoggerMessage(
        EventId = 3020,
        Level = LogLevel.Debug,
        Message = "Added Replay-Nonce: {Nonce}")]
    public static partial void ReplayNonceAdded(this ILogger logger, string nonce);

    #endregion

    #region JWSAuthenticationHandler (3040-3059)

    [LoggerMessage(
        EventId = 3040,
        Level = LogLevel.Debug,
        Message = "Found JWK in request, validating signature.")]
    public static partial void FoundJwkInRequest(this ILogger logger);

    [LoggerMessage(
        EventId = 3041,
        Level = LogLevel.Debug,
        Message = "Loading account with ID {AccountId} from KID")]
    public static partial void LoadingAccountFromKid(this ILogger logger, AccountId accountId);

    #endregion

    #region CAAQueryHandler (3060-3079)

    [LoggerMessage(
        EventId = 3060,
        Level = LogLevel.Warning,
        Message = "CNAME loop detected when querying CAA records for domain {DomainName}")]
    public static partial void CnameLoopDetected(this ILogger logger, string domainName);

    [LoggerMessage(
        EventId = 3061,
        Level = LogLevel.Debug,
        Message = "CNAME record found for {DomainName}, pointing to {CanonicalName}")]
    public static partial void CnameRecordFound(this ILogger logger, string domainName, string canonicalName);

    #endregion

    #region CertificateIssuanceProcessor (3080-3119)

    [LoggerMessage(
        EventId = 3080,
        Level = LogLevel.Information,
        Message = "Processing order {OrderId}.")]
    public static partial void ProcessingOrderForIssuance(this ILogger logger, OrderId orderId);

    [LoggerMessage(
        EventId = 3081,
        Level = LogLevel.Error,
        Message = "Error processing orders for validation.")]
    public static partial void ErrorProcessingOrdersForValidation(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 3082,
        Level = LogLevel.Warning,
        Message = "Certificate cannot be issued, due to unkown order {OrderId}")]
    public static partial void UnknownOrderForIssuance(this ILogger logger, OrderId orderId);

    [LoggerMessage(
        EventId = 3083,
        Level = LogLevel.Warning,
        Message = "Certificate cannot be issued, due to order {OrderId} not being in processing state")]
    public static partial void OrderNotInProcessingState(this ILogger logger, OrderId orderId);

    [LoggerMessage(
        EventId = 3084,
        Level = LogLevel.Warning,
        Message = "Certificate cannot be issued, due to unkown account {AccountId}")]
    public static partial void UnknownAccountForIssuance(this ILogger logger, AccountId accountId);

    [LoggerMessage(
        EventId = 3085,
        Level = LogLevel.Warning,
        Message = "Certificate cannot be issued, due to account {AccountId} not being in a valid state")]
    public static partial void AccountNotValidForIssuance(this ILogger logger, AccountId accountId);

    [LoggerMessage(
        EventId = 3086,
        Level = LogLevel.Information,
        Message = "Certificate issued for order {OrderId} with subject {Subject} and serial number {SerialNumber}.")]
    public static partial void CertificateIssuedForOrder(this ILogger logger, OrderId orderId, string subject, string serialNumber);

    [LoggerMessage(
        EventId = 3087,
        Level = LogLevel.Error,
        Message = "Error processing orders for issuance.")]
    public static partial void ErrorProcessingOrdersForIssuance(this ILogger logger, Exception ex);

    #endregion

    #region OrderValidationProcessor (3120-3159)

    [LoggerMessage(
        EventId = 3120,
        Level = LogLevel.Information,
        Message = "Processing order {OrderId}.")]
    public static partial void ProcessingOrderForValidation(this ILogger logger, OrderId orderId);

    [LoggerMessage(
        EventId = 3121,
        Level = LogLevel.Information,
        Message = "Attempting to validate order {OrderId}.")]
    public static partial void AttemptingToValidateOrder(this ILogger logger, OrderId orderId);

    [LoggerMessage(
        EventId = 3122,
        Level = LogLevel.Information,
        Message = "Found pending authorization {AuthorizationId} with selected challenge {ChallengeId} ({ChallengeType})")]
    public static partial void FoundPendingAuthorization(this ILogger logger, AuthorizationId authorizationId, ChallengeId challengeId, string challengeType);

    [LoggerMessage(
        EventId = 3123,
        Level = LogLevel.Information,
        Message = "Challenge {ChallengeId} ({ChallengeType}) was valid.")]
    public static partial void ChallengeWasValid(this ILogger logger, ChallengeId challengeId, string challengeType);

    [LoggerMessage(
        EventId = 3124,
        Level = LogLevel.Information,
        Message = "Challenge {ChallengeId} ({ChallengeType}) was invalid.")]
    public static partial void ChallengeWasInvalid(this ILogger logger, ChallengeId challengeId, string challengeType);

    [LoggerMessage(
        EventId = 3125,
        Level = LogLevel.Warning,
        Message = "Validation cannot be done, due to unkown order {OrderId}")]
    public static partial void UnknownOrderForValidation(this ILogger logger, OrderId orderId);

    [LoggerMessage(
        EventId = 3126,
        Level = LogLevel.Warning,
        Message = "Validation cannot be done, due to order {OrderId} not being in a pending state")]
    public static partial void OrderNotInPendingState(this ILogger logger, OrderId orderId);

    [LoggerMessage(
        EventId = 3127,
        Level = LogLevel.Warning,
        Message = "Validation cannot be done, due to unkown account {AccountId}")]
    public static partial void UnknownAccountForValidation(this ILogger logger, AccountId accountId);

    [LoggerMessage(
        EventId = 3128,
        Level = LogLevel.Warning,
        Message = "Validation cannot be done, due to account {AccountId} not being in a valid state")]
    public static partial void AccountNotValidForValidation(this ILogger logger, AccountId accountId);

    #endregion

    #region RevokationService (3160-3179)

    [LoggerMessage(
        EventId = 3160,
        Level = LogLevel.Warning,
        Message = "Attempt to revoke an already revoked certificate. CertificateId: {CertificateId}")]
    public static partial void AttemptToRevokeRevokedCertificate(this ILogger logger, CertificateId certificateId);

    [LoggerMessage(
        EventId = 3161,
        Level = LogLevel.Warning,
        Message = "Unauthorized revokation attempt. CertificateId: {CertificateId}")]
    public static partial void UnauthorizedRevokationAttempt(this ILogger logger, CertificateId certificateId);

    [LoggerMessage(
        EventId = 3162,
        Level = LogLevel.Warning,
        Message = "Could not locate order for certificate: {CertificateId} - revocation not possible")]
    public static partial void CouldNotLocateOrderForCertificate(this ILogger logger, CertificateId certificateId);

    #endregion

    #region ChallengeValidator (3180-3219)

    [LoggerMessage(
        EventId = 3180,
        Level = LogLevel.Information,
        Message = "Attempting to validate challenge {ChallengeId} ({ChallengeType})")]
    public static partial void AttemptingToValidateChallenge(this ILogger logger, ChallengeId challengeId, string challengeType);

    [LoggerMessage(
        EventId = 3181,
        Level = LogLevel.Information,
        Message = "Account {AccountId} is not valid. Challenge validation failed.")]
    public static partial void AccountNotValidForChallenge(this ILogger logger, AccountId accountId);

    [LoggerMessage(
        EventId = 3182,
        Level = LogLevel.Information,
        Message = "Challenges authorization already expired.")]
    public static partial void ChallengeAuthorizationExpired(this ILogger logger);

    [LoggerMessage(
        EventId = 3183,
        Level = LogLevel.Information,
        Message = "Order already expired.")]
    public static partial void ChallengeOrderExpired(this ILogger logger);

    #endregion

    #region StringTokenChallengeValidator (3220-3239)

    [LoggerMessage(
        EventId = 3220,
        Level = LogLevel.Error,
        Message = "Challenge is not of type TokenChallenge.")]
    public static partial void ChallengeNotTokenChallenge(this ILogger logger);

    [LoggerMessage(
        EventId = 3221,
        Level = LogLevel.Information,
        Message = "Could not load challenge response: {ErrorDetail}")]
    public static partial void CouldNotLoadChallengeResponse(this ILogger logger, string? errorDetail);

    [LoggerMessage(
        EventId = 3222,
        Level = LogLevel.Information,
        Message = "Expected content of challenge is {ExpectedContent}.")]
    public static partial void ExpectedChallengeContent(this ILogger logger, string expectedContent);

    [LoggerMessage(
        EventId = 3223,
        Level = LogLevel.Information,
        Message = "Challenge did not match expected value.")]
    public static partial void ChallengeDidNotMatch(this ILogger logger);

    [LoggerMessage(
        EventId = 3224,
        Level = LogLevel.Information,
        Message = "Challenge matched expected value.")]
    public static partial void ChallengeMatched(this ILogger logger);

    #endregion

    #region TlsAlpn01ChallengeValidator (3240-3259)

    [LoggerMessage(
        EventId = 3240,
        Level = LogLevel.Information,
        Message = "Could not connect to {IdentifierHostName} for tls-alpn-01 challenge validation.")]
    public static partial void TlsAlpn01ConnectionFailed(this ILogger logger, string identifierHostName, Exception ex);

    [LoggerMessage(
        EventId = 3241,
        Level = LogLevel.Information,
        Message = "The remote server did not present a certificate.")]
    public static partial void TlsAlpn01NoCertificate(this ILogger logger);

    [LoggerMessage(
        EventId = 3242,
        Level = LogLevel.Information,
        Message = "The remote server presented an invalid number of Subject Alternative Name (SAN) extensions.")]
    public static partial void TlsAlpn01InvalidSanCount(this ILogger logger);

    [LoggerMessage(
        EventId = 3243,
        Level = LogLevel.Information,
        Message = "The remote server presented an invalid number of DNS names in the Subject Alternative Name (SAN) extension.")]
    public static partial void TlsAlpn01InvalidDnsNameCount(this ILogger logger);

    [LoggerMessage(
        EventId = 3244,
        Level = LogLevel.Information,
        Message = "The remote server presented an invalid DNS name in the Subject Alternative Name (SAN) extension. Expected {Expected}, Actual {Actual}")]
    public static partial void TlsAlpn01InvalidDnsName(this ILogger logger, string expected, string actual);

    [LoggerMessage(
        EventId = 3245,
        Level = LogLevel.Information,
        Message = "The remote server presented an invalid number of id-pe-acmeIdentifier extensions.")]
    public static partial void TlsAlpn01InvalidAcmeIdentifierCount(this ILogger logger);

    [LoggerMessage(
        EventId = 3246,
        Level = LogLevel.Information,
        Message = "The remote server presented a non-critical id-pe-acmeIdentifier extension.")]
    public static partial void TlsAlpn01NonCriticalAcmeIdentifier(this ILogger logger);

    [LoggerMessage(
        EventId = 3247,
        Level = LogLevel.Information,
        Message = "The remote server presented an invalid id-pe-acmeIdentifier content. Expected {Expected}, Actual {Actual}")]
    public static partial void TlsAlpn01InvalidAcmeIdentifierContent(this ILogger logger, byte[] expected, byte[] actual);

    #endregion

    #region DeviceAttest01RemoteValidator (3260-3279)

    [LoggerMessage(
        EventId = 3260,
        Level = LogLevel.Information,
        Message = "Remote DeviceAttest01 validation indicated non success status code: {StatusCode}")]
    public static partial void DeviceAttest01RemoteValidationNonSuccess(this ILogger logger, int statusCode);

    [LoggerMessage(
        EventId = 3261,
        Level = LogLevel.Error,
        Message = "Failed to retrieve DeviceAttest01 result.")]
    public static partial void DeviceAttest01RemoteValidationError(this ILogger logger, Exception ex);

    #endregion

    #region AlternativeNameValidator (3280-3319)

    [LoggerMessage(
        EventId = 3280,
        Level = LogLevel.Information,
        Message = "All subject alternative names are valid through identifiers.")]
    public static partial void AllSansValidThroughIdentifiers(this ILogger logger);

    [LoggerMessage(
        EventId = 3281,
        Level = LogLevel.Debug,
        Message = "Not all subject alternative names are valid through identifiers. Validating via profile configuration.")]
    public static partial void ValidatingSansViaProfileConfig(this ILogger logger);

    [LoggerMessage(
        EventId = 3282,
        Level = LogLevel.Debug,
        Message = "Validating SubjectAlternativeName '{San}' against identifiers.")]
    public static partial void ValidatingSanAgainstIdentifiers(this ILogger logger, string san);

    [LoggerMessage(
        EventId = 3283,
        Level = LogLevel.Information,
        Message = "SubjectAlternativeName '{San}' has matched identifier {Identifier}. Setting usage flag.")]
    public static partial void SanMatchedIdentifier(this ILogger logger, string san, Identifier identifier);

    [LoggerMessage(
        EventId = 3284,
        Level = LogLevel.Information,
        Message = "Setting SubjectAlternativeName '{San}' to valid, since it had matching identifiers.")]
    public static partial void SanSetToValid(this ILogger logger, string san);

    [LoggerMessage(
        EventId = 3285,
        Level = LogLevel.Debug,
        Message = "Validating {Count} alternative names against profile configuration.")]
    public static partial void ValidatingAlternativeNamesAgainstProfile(this ILogger logger, int count);

    [LoggerMessage(
        EventId = 3286,
        Level = LogLevel.Debug,
        Message = "Validating alternative name: {AlternativeName}")]
    public static partial void ValidatingAlternativeName(this ILogger logger, string alternativeName);

    [LoggerMessage(
        EventId = 3287,
        Level = LogLevel.Warning,
        Message = "Validation for alternative name type {AlternativeNameType} is not implemented.")]
    public static partial void AlternativeNameTypeNotImplemented(this ILogger logger, string alternativeNameType);

    [LoggerMessage(
        EventId = 3288,
        Level = LogLevel.Debug,
        Message = "No validation parameters configured for {Type}. Skipping validation.")]
    public static partial void NoValidationParametersConfigured(this ILogger logger, string type);

    [LoggerMessage(
        EventId = 3289,
        Level = LogLevel.Debug,
        Message = "No validation regex configured for {Type}. Skipping validation.")]
    public static partial void NoValidationRegexConfigured(this ILogger logger, string type);

    [LoggerMessage(
        EventId = 3290,
        Level = LogLevel.Error,
        Message = "Failed to parse regex: {Regex} for type {Type}")]
    public static partial void FailedToParseRegex(this ILogger logger, string regex, string type, Exception ex);

    [LoggerMessage(
        EventId = 3291,
        Level = LogLevel.Information,
        Message = "Validating {Value} against regex {ValueRegex} from profile configuration: {IsMatch}.")]
    public static partial void ValidatingAgainstRegex(this ILogger logger, string value, string valueRegex, bool isMatch);

    [LoggerMessage(
        EventId = 3292,
        Level = LogLevel.Debug,
        Message = "No valid networks configured for IPAddress validation. Skipping validation.")]
    public static partial void NoValidNetworksConfigured(this ILogger logger);

    [LoggerMessage(
        EventId = 3293,
        Level = LogLevel.Warning,
        Message = "Invalid IP network format: {IpNetwork}")]
    public static partial void InvalidIpNetworkFormat(this ILogger logger, string ipNetwork);

    [LoggerMessage(
        EventId = 3294,
        Level = LogLevel.Information,
        Message = "Validating IPAddress {IPAddress} against allowed network {AllowedIPNetwork} from profile configuration: {IsInNetwork}")]
    public static partial void ValidatingIpAddressAgainstNetwork(this ILogger logger, string ipAddress, string allowedIPNetwork, bool isInNetwork);

    [LoggerMessage(
        EventId = 3295,
        Level = LogLevel.Debug,
        Message = "Validating OtherName with type {TypeId}")]
    public static partial void ValidatingOtherName(this ILogger logger, string typeId);

    [LoggerMessage(
        EventId = 3296,
        Level = LogLevel.Error,
        Message = "Failed to parse permanent identifier value regex")]
    public static partial void FailedToParsePermanentIdentifierValueRegex(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 3297,
        Level = LogLevel.Error,
        Message = "Failed to parse permanent identifier assigner regex")]
    public static partial void FailedToParsePermanentIdentifierAssignerRegex(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 3298,
        Level = LogLevel.Information,
        Message = "Validated permanent identifier value {Value} against regex {ValueRegex} from profile configuration: {IsValid}")]
    public static partial void ValidatedPermanentIdentifierValue(this ILogger logger, string? value, string? valueRegex, bool isValid);

    [LoggerMessage(
        EventId = 3299,
        Level = LogLevel.Information,
        Message = "Validated permanent identifier assigner {Assigner} against regex {AssignerRegex} from profile configuration: {IsValid}")]
    public static partial void ValidatedPermanentIdentifierAssigner(this ILogger logger, string assigner, string? assignerRegex, bool isValid);

    [LoggerMessage(
        EventId = 3300,
        Level = LogLevel.Information,
        Message = "Permanent identifier assigner is null, skipping validation against regex.")]
    public static partial void PermanentIdentifierAssignerIsNull(this ILogger logger);

    [LoggerMessage(
        EventId = 3301,
        Level = LogLevel.Debug,
        Message = "No valid type regex configured for HardwareModuleName validation. Skipping validation.")]
    public static partial void NoValidTypeRegexConfigured(this ILogger logger);

    [LoggerMessage(
        EventId = 3302,
        Level = LogLevel.Error,
        Message = "Failed to parse hardware module type regex: {TypeRegex}")]
    public static partial void FailedToParseHardwareModuleTypeRegex(this ILogger logger, string typeRegex, Exception ex);

    [LoggerMessage(
        EventId = 3303,
        Level = LogLevel.Information,
        Message = "Validating hardware module name {TypeId} against regex {TypeRegex} from profile configuration: {IsMatch}.")]
    public static partial void ValidatingHardwareModuleName(this ILogger logger, string typeId, string typeRegex, bool isMatch);

    [LoggerMessage(
        EventId = 3304,
        Level = LogLevel.Debug,
        Message = "There's currently no validation for the hardware module serial number as it's binary")]
    public static partial void NoValidationForHardwareModuleSerialNumber(this ILogger logger);

    [LoggerMessage(
        EventId = 3305,
        Level = LogLevel.Debug,
        Message = "No parameters configured for OtherName ignored types. Skipping validation.")]
    public static partial void NoParametersForOtherNameIgnoredTypes(this ILogger logger);

    [LoggerMessage(
        EventId = 3306,
        Level = LogLevel.Debug,
        Message = "OtherName with type {TypeId} is ignored: {IsIgnored}")]
    public static partial void OtherNameIsIgnored(this ILogger logger, string typeId, bool isIgnored);

    #endregion

    #region CommonNameValidator (3320-3339)

    [LoggerMessage(
        EventId = 3320,
        Level = LogLevel.Information,
        Message = "No common names found in subject name, skipping common name validation.")]
    public static partial void NoCommonNamesFound(this ILogger logger);

    [LoggerMessage(
        EventId = 3321,
        Level = LogLevel.Information,
        Message = "Common name '{CommonName}' matches identifier '{Identifier}'.")]
    public static partial void CommonNameMatchesIdentifier(this ILogger logger, string commonName, Identifier identifier);

    [LoggerMessage(
        EventId = 3322,
        Level = LogLevel.Information,
        Message = "Common name '{CommonName}' is valid because it matches an identifier.")]
    public static partial void CommonNameValidBecauseIdentifier(this ILogger logger, string commonName);

    [LoggerMessage(
        EventId = 3323,
        Level = LogLevel.Information,
        Message = "Common name '{CommonName}' is valid because it matches an alternative name.")]
    public static partial void CommonNameValidBecauseAlternativeName(this ILogger logger, string commonName);

    #endregion

    #region ExpectedPublicKeyValidator (3340-3359)

    [LoggerMessage(
        EventId = 3340,
        Level = LogLevel.Debug,
        Message = "The validation context did not contain expected public keys. Skipping validation")]
    public static partial void NoExpectedPublicKeys(this ILogger logger);

    [LoggerMessage(
        EventId = 3341,
        Level = LogLevel.Information,
        Message = "Validated expectedPublicKey against certificate request. Result: {IsValid}")]
    public static partial void ValidatedExpectedPublicKey(this ILogger logger, bool isValid);

    #endregion

    #region DefaultCAAEvaluator (3360-3399)

    [LoggerMessage(
        EventId = 3360,
        Level = LogLevel.Debug,
        Message = "No CAA entries were present for {Identifier}. Issuance is allowed.")]
    public static partial void NoCaaEntriesPresent(this ILogger logger, Identifier identifier);

    [LoggerMessage(
        EventId = 3361,
        Level = LogLevel.Warning,
        Message = "CAA evaluation was requested, but no CAA identities are configured. CAA evaluation failed for {Identifier}.")]
    public static partial void CaaEvaluationNoCaaIdentities(this ILogger logger, Identifier identifier);

    [LoggerMessage(
        EventId = 3362,
        Level = LogLevel.Warning,
        Message = "No CAA entry matched our CAA identifiers. CAA evaluation failed for {Identifier}")]
    public static partial void NoCaaEntryMatched(this ILogger logger, Identifier identifier);

    [LoggerMessage(
        EventId = 3363,
        Level = LogLevel.Warning,
        Message = "CAA AccountURI parameters did not match the requesting account. CAA evaluation failed for {Identifier} and AccountId {AccountId}")]
    public static partial void CaaAccountUriMismatch(this ILogger logger, Identifier identifier, AccountId accountId);

    [LoggerMessage(
        EventId = 3364,
        Level = LogLevel.Information,
        Message = "Found validationMethods requirements in CAA: {ValidationMethods}")]
    public static partial void CaaValidationMethodsFound(this ILogger logger, string validationMethods);

    [LoggerMessage(
        EventId = 3365,
        Level = LogLevel.Information,
        Message = "Could not understand parameter with value {P}")]
    public static partial void CaaParameterNotUnderstood(this ILogger logger, string p);

    [LoggerMessage(
        EventId = 3366,
        Level = LogLevel.Information,
        Message = "Parameter {Key} was not a known parameter")]
    public static partial void CaaParameterNotKnown(this ILogger logger, string key);

    #endregion

    #region DefaultExternalAccountBindingValidator (3400-3439)

    [LoggerMessage(
        EventId = 3400,
        Level = LogLevel.Debug,
        Message = "External account binding JWS header alg is not a HMAC algorithm: {Alg}")]
    public static partial void EabAlgNotHmac(this ILogger logger, string alg);

    [LoggerMessage(
        EventId = 3401,
        Level = LogLevel.Debug,
        Message = "External account binding JWS header contains a nonce: {Nonce}")]
    public static partial void EabContainsNonce(this ILogger logger, string? nonce);

    [LoggerMessage(
        EventId = 3402,
        Level = LogLevel.Debug,
        Message = "External account binding JWS header url: {Url} does not match request url: {RequestUrl}")]
    public static partial void EabUrlMismatch(this ILogger logger, string? url, string? requestUrl);

    [LoggerMessage(
        EventId = 3403,
        Level = LogLevel.Debug,
        Message = "External account binding JWS payload: {Payload} does not match request JWK: {Jwk}")]
    public static partial void EabPayloadMismatch(this ILogger logger, string payload, string jwk);

    [LoggerMessage(
        EventId = 3404,
        Level = LogLevel.Debug,
        Message = "External account binding JWS header does not contain a kid: {Kid}")]
    public static partial void EabMissingKid(this ILogger logger, string? kid);

    [LoggerMessage(
        EventId = 3405,
        Level = LogLevel.Debug,
        Message = "Retrieved external account binding MAC key: {Key} for Kid: {Kid}")]
    public static partial void EabRetrievedMacKey(this ILogger logger, byte[] key, string kid);

    [LoggerMessage(
        EventId = 3406,
        Level = LogLevel.Debug,
        Message = "External account binding MAC key: {Key} for Kid: {Kid} is invalid")]
    public static partial void EabMacKeyInvalid(this ILogger logger, byte[] key, string kid);

    [LoggerMessage(
        EventId = 3407,
        Level = LogLevel.Debug,
        Message = "External account binding MAC key: {Key} for Kid: {Kid} is valid")]
    public static partial void EabMacKeyValid(this ILogger logger, byte[] key, string kid);

    [LoggerMessage(
        EventId = 3408,
        Level = LogLevel.Warning,
        Message = "Error during External Account Binding validation")]
    public static partial void EabValidationError(this ILogger logger, Exception ex);

    #endregion

    #region DefaultExternalAccountBindingClient (3440-3459)

    [LoggerMessage(
        EventId = 3440,
        Level = LogLevel.Warning,
        Message = "Failed to retrieve MAC: ({StatusCode} - {ReasonPhrase}) {ResponseText}")]
    public static partial void EabClientFailedToRetrieveMac(this ILogger logger, int statusCode, string? reasonPhrase, string responseText);

    [LoggerMessage(
        EventId = 3441,
        Level = LogLevel.Warning,
        Message = "Failed to retrieve MAC or decode MAC")]
    public static partial void EabClientFailedToRetrieveOrDecodeMac(this ILogger logger, Exception ex);

    #endregion

    #region RequestValidationService (3460-3489)

    [LoggerMessage(
        EventId = 3460,
        Level = LogLevel.Debug,
        Message = "Request header validation failed due to header url not being well-formed")]
    public static partial void RequestHeaderUrlNotWellFormed(this ILogger logger);

    [LoggerMessage(
        EventId = 3461,
        Level = LogLevel.Warning,
        Message = "Request header validation failed due to header url not matching actual path")]
    public static partial void RequestHeaderUrlMismatch(this ILogger logger);

    [LoggerMessage(
        EventId = 3462,
        Level = LogLevel.Debug,
        Message = "Request header validation failed due to algorithm '{Alg}' not being supported.")]
    public static partial void RequestHeaderAlgorithmNotSupported(this ILogger logger, string alg);

    [LoggerMessage(
        EventId = 3463,
        Level = LogLevel.Debug,
        Message = "Request header validation failed due to Jwk and Kid being present at the same time.")]
    public static partial void RequestHeaderJwkAndKidPresent(this ILogger logger);

    [LoggerMessage(
        EventId = 3464,
        Level = LogLevel.Debug,
        Message = "Request header validation failed due to neither Jwk nor Kid being present.")]
    public static partial void RequestHeaderNeitherJwkNorKid(this ILogger logger);

    [LoggerMessage(
        EventId = 3465,
        Level = LogLevel.Debug,
        Message = "Request headers have been successfully validated.")]
    public static partial void RequestHeadersValidated(this ILogger logger);

    [LoggerMessage(
        EventId = 3466,
        Level = LogLevel.Debug,
        Message = "Replay nonce could not be validated: Nonce was empty.")]
    public static partial void NonceEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 3467,
        Level = LogLevel.Debug,
        Message = "Replay nonce could not be validated: Nonce was invalid or replayed.")]
    public static partial void NonceInvalidOrReplayed(this ILogger logger);

    [LoggerMessage(
        EventId = 3468,
        Level = LogLevel.Debug,
        Message = "Replay-nonce has been successfully validated.")]
    public static partial void NonceValidated(this ILogger logger);

    #endregion

    #region IdentifierValidator (3490-3509)

    [LoggerMessage(
        EventId = 3490,
        Level = LogLevel.Warning,
        Message = "The IP network {AllowedNetwork} is not a valid CIDR notation.")]
    public static partial void InvalidCidrNotation(this ILogger logger, string allowedNetwork);

    #endregion

    #region IssuanceProfileSelector (3510-3529)

    [LoggerMessage(
        EventId = 3510,
        Level = LogLevel.Information,
        Message = "No issuance profile found for order {OrderId} with identifiers {Identifiers}")]
    public static partial void NoIssuanceProfileFound(this ILogger logger, OrderId orderId, string identifiers);

    [LoggerMessage(
        EventId = 3511,
        Level = LogLevel.Debug,
        Message = "Selected profile {ProfileName} for order {OrderId} with identifiers {Identifiers}")]
    public static partial void ProfileSelected(this ILogger logger, ProfileName profileName, OrderId orderId, string identifiers);

    [LoggerMessage(
        EventId = 3512,
        Level = LogLevel.Debug,
        Message = "Profile {ProfileName} was not considered due to invalid identifiers: {Errors}")]
    public static partial void ProfileNotConsideredDueToInvalidIdentifiers(this ILogger logger, ProfileName profileName, string errors);

    #endregion

    #region NonceFactory (3530-3549)

    [LoggerMessage(
        EventId = 3530,
        Level = LogLevel.Debug,
        Message = "Created and saved new nonce: {Nonce}.")]
    public static partial void NonceCreated(this ILogger logger, string nonce);

    #endregion
}
