using Microsoft.Extensions.Logging;
using System.Formats.Asn1;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

/// <summary>
/// Implements challenge validation as described in the ACME RFC 8737 (https://www.rfc-editor.org/rfc/rfc8737) for the "tls-alpn-01" challenge type.
/// </summary>
public sealed class TlsAlpn01ChallengeValidator(ILogger<TlsAlpn01ChallengeValidator> logger) 
    : ChallengeValidator(logger)
{
    public class OIDs
    {
        public const string ID_PE_ACMEIdentifier = "1.3.6.1.5.5.7.1.31";
    }

    private readonly ILogger<TlsAlpn01ChallengeValidator> _logger = logger;

    public override string ChallengeType => ChallengeTypes.TlsAlpn01;
    public override IEnumerable<string> SupportedIdentiferTypes => [IdentifierTypes.DNS, IdentifierTypes.IP];

    private static byte[] GetExpectedContent(TokenChallenge challenge, Account account)
        => GetKeyAuthDigest(challenge, account);

    protected override async Task<ChallengeValidationResult> ValidateChallengeInternalAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
    {
        if (challenge is not TokenChallenge tokenChallenge)
        {
            _logger.ChallengeNotTokenChallenge();
            throw new InvalidOperationException("Challenge is not of type TokenChallenge.");
        }

        var connectionHost = challenge.Authorization.Identifier.Value;
        var sniHostName = GetSNIHostName(tokenChallenge);

        X509Certificate2? remoteCertificate = null;
        // RFC 8737 requires the use of port 443 for the "tls-alpn-01" challenge.
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(connectionHost, 443, cancellationToken);

            using var sslStream = new SslStream(tcpClient.GetStream());
            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions()
            {
                // RFC 8737 requires SNI Hostname to be set to the challenge host.
                TargetHost = sniHostName,
                // RFC 8737 requires the use of the "acme-tls/1" ALPN protocol.
                ApplicationProtocols = [new("acme-tls/1")],

                // RFC 8737 requires the use of TLS 1.2 or higher.
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,

                // RFC 8737 requires the use of a self-signed certificate.
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            },
            cancellationToken);

            remoteCertificate = sslStream.RemoteCertificate as X509Certificate2;
        }
        catch (Exception ex)
        {
            _logger.TlsAlpn01ConnectionFailed(connectionHost, ex);
            return ChallengeValidationResult.Invalid(AcmeErrors.Connection(challenge.Authorization.Identifier, ex.Message));
        }

        if (remoteCertificate == null)
        {
            _logger.TlsAlpn01NoCertificate();
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The server did not present a certificate."));
        }

        // -- Validate the certificate SAN extension --
        var subjectAlternateNameExtensions = remoteCertificate.Extensions
            .Where(x => x is X509SubjectAlternativeNameExtension)
            .Cast<X509SubjectAlternativeNameExtension>()
            .ToList();

        // RFC 8737 requires the certificate to contain exactly one SAN extension.
        if (subjectAlternateNameExtensions.Count != 1)
        {
            _logger.TlsAlpn01InvalidSanCount();
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The server presented an invalid number of Subject Alternative Name (SAN) extensions."));
        }

        var dnsNames = subjectAlternateNameExtensions[0].EnumerateDnsNames().ToList();

        // RFC 8737 requires the certificate to contain exactly one DNS name in the SAN extension.
        if (dnsNames.Count != 1)
        {
            _logger.TlsAlpn01InvalidDnsNameCount();
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The server presented an invalid number of DNS names in the Subject Alternative Name (SAN) extension."));
        }

        // RFC 8737 requires the certificate to contain a SAN extension with the value of the identifiers host name.
        if (dnsNames[0] != connectionHost)
        {
            _logger.TlsAlpn01InvalidDnsName(connectionHost, dnsNames.First());
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The server presented an invalid DNS name in the Subject Alternative Name (SAN) extension."));
        }

        

        // -- Validate the id-pe-acmeIdentifier extension --
        var acmeIdentifierExtensions = remoteCertificate.Extensions
            .Where(x => x.Oid!.Value == OIDs.ID_PE_ACMEIdentifier)
            .ToList();

        if(acmeIdentifierExtensions.Count != 1)
        {
            _logger.TlsAlpn01InvalidAcmeIdentifierCount();
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The server presented an invalid number of id-pe-acmeIdentifier extensions."));
        }

        var acmeIdentifierExtension = acmeIdentifierExtensions.First();
        if (!acmeIdentifierExtension.Critical) {
            _logger.TlsAlpn01NonCriticalAcmeIdentifier();
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The server presented a non-critical id-pe-acmeIdentifier extension."));
        }

        var presentedChallengeResponse = AsnDecoder.ReadOctetString(acmeIdentifierExtensions.First().RawData, AsnEncodingRules.DER, out var bytesConsumed);
        var expectedChallengeResponse = GetExpectedContent(tokenChallenge, account);

        if (!presentedChallengeResponse.SequenceEqual(expectedChallengeResponse))
        {
            _logger.TlsAlpn01InvalidAcmeIdentifierContent(expectedChallengeResponse, presentedChallengeResponse);
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The server presented an invalid id-pe-acmeIdentifier extension."));
        }

        return ChallengeValidationResult.Valid();
    }


    private string GetSNIHostName(TokenChallenge challenge)
    {
        if(challenge.Authorization.Identifier.Type == IdentifierTypes.DNS)
        {
            return challenge.Authorization.Identifier.Value;
        }
        else if (challenge.Authorization.Identifier.Type == IdentifierTypes.IP)
        {
            // RFC 8738 requires the IN-ADDR.ARPA [RFC1034] or IP6.ARPA [RFC3596] reverse mapping of the IP address.
            var ipAddress = IPAddress.Parse(challenge.Authorization.Identifier.Value);
            var arpaName = ipAddress.GetArpaName();

            return arpaName;
        }
        
        throw new NotSupportedException($"The identifier type {challenge.Authorization.Identifier.Type} is not supported.");
    }
}
