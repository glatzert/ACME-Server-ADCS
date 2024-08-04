using Microsoft.IdentityModel.Tokens;
using System.Formats.Asn1;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Th11s.ACMEServer.Services.ChallengeValidation;

namespace ACMEServer.Services.ChallengeValidation.Tests
{
    internal class TlsAlpnServer
    {
        public static async Task RunServer(CancellationToken cancellationToken)
        {
            // Create a TCP/IP (IPv4) socket and listen for incoming connections.
            TcpListener listener = new TcpListener(IPAddress.Loopback, 443);
            listener.Start();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Waiting for a client to connect...");

                    TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
                    await ProcessClientAsync(client, cancellationToken);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                listener.Stop();
                listener.Dispose();
            }
        }


        static async Task ProcessClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            // A client has connected. Create the SslStream using the client's network stream.
            SslStream sslStream = new SslStream(client.GetStream(), false);

            // Authenticate the server but don't require the client to authenticate.
            try
            {
                await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ApplicationProtocols = [new("acme-tls/1")],

                    ClientCertificateRequired = false,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ServerCertificateSelectionCallback = (_, hostName) => CreateCertificate(hostName)
                }, cancellationToken);

                // Do we need to wait for the client to send data?
                _ = await sslStream.ReadAsync(new byte[4096], cancellationToken);
            }
            finally
            {
                sslStream.Close();
            }
        }

        private static X509Certificate2 CreateCertificate(string? hostName)
        {
            ArgumentNullException.ThrowIfNull(hostName);

            using var rsa = RSA.Create(2048);
            var name = new X500DistinguishedName($"CN={hostName}");

            var request = new CertificateRequest(
                name,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var acmeChallengeHash = Base64UrlEncoder.Encode(
                SHA256.HashData(Encoding.UTF8.GetBytes("-- Token here --"))
            );

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteOctetString(Encoding.UTF8.GetBytes(acmeChallengeHash));
            byte[] acmeChallengeAsn = writer.Encode();

            request.CertificateExtensions.Add(
                new X509Extension(
                    new AsnEncodedData(TlsAlpn01ChallengeValidator.OIDs.ID_PE_ACMEIdentifier, acmeChallengeAsn),
                    critical: true));

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(hostName);
            request.CertificateExtensions.Add(sanBuilder.Build());

            return request.CreateSelfSigned(
                new DateTimeOffset(DateTime.UtcNow.AddDays(-1)),
                new DateTimeOffset(DateTime.UtcNow.AddDays(1)));
        }
    }
}
