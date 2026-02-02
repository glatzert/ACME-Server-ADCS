using Microsoft.Extensions.Logging;

namespace Th11s.ACMEServer.CertProvider.ADCS;

/// <summary>
/// Centralized logging messages for ACMEServer.CertProvider.ADCS project using source-generated logging.
/// EventID range: 2000-2999
/// </summary>
internal static partial class LogMessages
{
    #region CertificateIssuer (2000-2099)

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Debug,
        Message = "Try to issue certificate for CSR: {Csr}")]
    public static partial void TryIssueCertificate(this ILogger logger, string csr);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Error,
        Message = "Profile configuration for profile '{Profile}' not found.")]
    public static partial void ProfileConfigurationNotFound(this ILogger logger, string profile);

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
