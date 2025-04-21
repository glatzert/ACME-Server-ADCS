using System.Formats.Asn1;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Th11s.ACMEServer.Services.ChallengeValidation.Tests;

internal class TlsAlpnServer : IDisposable
{
    private readonly string _hostName;
    private readonly byte[] _challengeContent;

    private readonly List<X509Certificate2> _certificates = [];

    public bool HasAuthorizedAsServer { get; private set; }

    public TlsAlpnServer(string hostName, byte[] challengeContent)
    {
        _hostName = hostName;
        _challengeContent = challengeContent;
    }

    public async Task RunServer(CancellationToken cancellationToken)
    {
        // Create a TCP/IP (IPv4) socket and listen for incoming connections.
        TcpListener listener = new TcpListener(IPAddress.Loopback, 443);
        listener.Start();

        try
        {
            TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
            await ProcessClientAsync(client, cancellationToken);
        }
        catch (TaskCanceledException) { }
        finally
        {
            listener.Stop();
            listener.Dispose();
        }
    }


    private async Task ProcessClientAsync(TcpClient client, CancellationToken cancellationToken)
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

            HasAuthorizedAsServer = true;

            // Do we need to wait for the client to send data?
            _ = await sslStream.ReadAsync(new byte[4096], cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            sslStream.Close();
        }
    }

    private X509Certificate2 CreateCertificate(string? hostName)
    {
        ArgumentNullException.ThrowIfNull(hostName);
        Assert.Equal(_hostName, hostName);

        using var rsa = RSA.Create(2048);
        var name = new X500DistinguishedName($"CN={_hostName}");

        var request = new CertificateRequest(
            name,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.WriteOctetString(_challengeContent);
        byte[] acmeChallengeAsn = writer.Encode();

        request.CertificateExtensions.Add(
            new X509Extension(
                new AsnEncodedData(TlsAlpn01ChallengeValidator.OIDs.ID_PE_ACMEIdentifier, acmeChallengeAsn),
                critical: true));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName(_hostName);
        request.CertificateExtensions.Add(sanBuilder.Build());

        using var x509Certificate = request.CreateSelfSigned(
            new DateTimeOffset(DateTime.UtcNow.AddDays(-1)),
            new DateTimeOffset(DateTime.UtcNow.AddDays(1)));

        // Windows only supports "non ephemeral" certificates, so we need to add it to the store and read it from there.
        using var persistable = new X509Certificate2(
            x509Certificate.Export(X509ContentType.Pfx),
            (string?)null, 
            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
        
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite);
        store.Add(persistable);
       
        // Find the certificate in the store
        var storedCertificate = store.Certificates.Single(x => x.Thumbprint == x509Certificate.Thumbprint);
        store.Close();

        _certificates.Add(storedCertificate);

        return storedCertificate;
    }

    public void Dispose()
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite);
        foreach(var certificate in _certificates)
        {
            var storedCertificate = store.Certificates.Single(x => x.Thumbprint == certificate.Thumbprint);
            store.Remove(storedCertificate);
        }

        store.Dispose();
    }
}
