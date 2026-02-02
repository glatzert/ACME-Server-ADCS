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
}
