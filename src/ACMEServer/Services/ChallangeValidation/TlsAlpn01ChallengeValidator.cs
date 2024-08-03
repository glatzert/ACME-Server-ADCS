using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;

namespace Th11s.ACMEServer.Services.ChallangeValidation
{
    /// <summary>
    /// Implements challenge validation as described in the ACME RFC 8737 (https://www.rfc-editor.org/rfc/rfc8737) for the "tls-alpn-01" challenge type.
    /// </summary>
    public sealed class TlsAlpn01ChallengeValidator : ChallengeValidator
    {
        private class OIDs
        {
            public const string ID_PE_ACMEIdentifier = "";
        }

        private readonly ILogger<TlsAlpn01ChallengeValidator> _logger;

        public TlsAlpn01ChallengeValidator(ILogger<TlsAlpn01ChallengeValidator> logger)
            : base(logger)
        {
            _logger = logger;
        }

        protected override string GetExpectedContent(Challenge challenge, Account account)
            => GetKeyAuthDigest(challenge, account);

        public override async Task<ChallengeValidationResult> ValidateChallengeInternalAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
        {
            var identifierHostName = challenge.Authorization.Identifier.Value;

            X509Certificate2? remoteCertificate = null;
            // RFC 8737 requires the use of port 443 for the "tls-alpn-01" challenge.
            try
            {
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(identifierHostName, 443, cancellationToken);

                using var sslStream = new SslStream(tcpClient.GetStream());
                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions()
                {
                    // RFC 8737 requires SNI Hostname to be set to the challenge host.
                    TargetHost = identifierHostName,
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
                _logger.LogInformation(ex, "Could not connect to {identifierHostName} for tls-alpn-01 challenge validation.", identifierHostName);
                return new(ChallengeResult.Invalid, new AcmeError("connection", ex.Message, challenge.Authorization.Identifier));
            }

            if (remoteCertificate == null)
            {
                _logger.LogInformation("The remote server did not present a certificate.");
                return new(ChallengeResult.Invalid, new AcmeError("custom:tls-alpn:noCertificate", "The server did not present a certificate."));
            }

            // -- Validate the certificate SAN extension --
            var subjectAlternateNameExtensions = remoteCertificate.Extensions
                .Where(x => x is X509SubjectAlternativeNameExtension)
                .Cast<X509SubjectAlternativeNameExtension>();

            // RFC 8737 requires the certificate to contain exactly one SAN extension.
            if (subjectAlternateNameExtensions.Count() != 1)
            {
                _logger.LogInformation("The remote server presented an invalid number of Subject Alternative Name (SAN) extensions.");
                return new(ChallengeResult.Invalid, new AcmeError("custom:tls-alpn:invalidSAN", "The server presented an invalid number of Subject Alternative Name (SAN) extensions."));
            }

            var dnsNames = subjectAlternateNameExtensions.First().EnumerateDnsNames().ToList();

            // RFC 8737 requires the certificate to contain exactly one DNS name in the SAN extension.
            if (dnsNames.Count() != 1)
            {
                _logger.LogInformation("The remote server presented an invalid number of DNS names in the Subject Alternative Name (SAN) extension.");
                return new(ChallengeResult.Invalid, new AcmeError("custom:tls-alpn:invalidSAN", "The server presented an invalid number of DNS names in the Subject Alternative Name (SAN) extension."));
            }

            // RFC 8737 requires the certificate to contain a SAN extension with the value of the identifiers host name.
            if (dnsNames.First() != identifierHostName)
            {
                _logger.LogInformation("The remote server presented an invalid DNS name in the Subject Alternative Name (SAN) extension. Expected {expected}, Actual {actual}", identifierHostName, dnsNames.First());
                return new(ChallengeResult.Invalid, new AcmeError("custom:tls-alpn:invalidSAN", "The server presented an invalid DNS name in the Subject Alternative Name (SAN) extension."));
            }

            

            // -- Validate the id-pe-acmeIdentifier extension --
            var acmeIdentifierExtensions = remoteCertificate.Extensions
                .Where(x => x.Oid!.Value == OIDs.ID_PE_ACMEIdentifier)
                .ToList();

            if(acmeIdentifierExtensions.Count != 1)
            {
                _logger.LogInformation("The remote server presented an invalid number of id-pe-acmeIdentifier extensions.");
                return new(ChallengeResult.Invalid, new AcmeError("custom:tls-alpn:invalidACMEIdentifier", "The server presented an invalid number of id-pe-acmeIdentifier extensions."));
            }

            var acmeIdentifierExtension = acmeIdentifierExtensions.First();
            if (!acmeIdentifierExtension.Critical) {
                _logger.LogInformation("The remote server presented a non-critical id-pe-acmeIdentifier extension.");
                return new(ChallengeResult.Invalid, new AcmeError("custom:tls-alpn:invalidACMEIdentifier", "The server presented a non-critical id-pe-acmeIdentifier extension."));
            }

            var presentedToken = Encoding.UTF8.GetString(AsnDecoder.ReadOctetString(acmeIdentifierExtensions.First().RawData, AsnEncodingRules.DER, out var bytesConsumed));
            var expectedToken = GetExpectedContent(challenge, account);

            if (presentedToken != expectedToken)
            {
                _logger.LogInformation("The remote server presented an invalid id-pe-acmeIdentifier content. Expected {expected}, Actual {actual}", expectedToken, presentedToken);
                return new(ChallengeResult.Invalid, new AcmeError("incorrectResponse", "The server presented an invalid id-pe-acmeIdentifier extension."));
            }

            return new(ChallengeResult.Valid, null);
        }
    }
}
