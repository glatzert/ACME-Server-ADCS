using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Th11s.ACMEServer.Model.Extensions;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

[Serializable]
public class CertificateContainer : IVersioned, ISerializable
{
    public CertificateContainer(AccountId accountId, OrderId orderId, X509Certificate2Collection x509Certificates)
    {
        var leafCertificate = x509Certificates.GetLeafCertificate();

        // We'll use the identifier also used by ARI: <base64url(AKI keyIdentifier)>.<base64url(Serial)>
        var authorityKeyIdentifier = leafCertificate.Extensions.OfType<X509AuthorityKeyIdentifierExtension>()
            .FirstOrDefault()?.KeyIdentifier?.ToArray() ?? Encoding.ASCII.GetBytes("AKI not found");
        var serialNumber = leafCertificate.SerialNumberBytes.ToArray();

        CertificateId = CertificateId.FromX509Certificate(leafCertificate);

        AccountId = accountId;
        OrderId = orderId;

        X509Certificates = x509Certificates.Export(X509ContentType.Pfx)!;
    }


    public CertificateId CertificateId { get; }
    public AccountId AccountId { get; }
    public OrderId OrderId { get; }

    public byte[] X509Certificates { get; }

    public RevokationStatus RevokationStatus { get; set; }


    /// <summary>
    /// Concurrency Token
    /// </summary>
    public long Version { get; set; }


    public CertificateContainer(
        CertificateId certificateId,
        AccountId accountId,
        OrderId orderId,

        byte[] x509Certificates,
        RevokationStatus revokationStatus,

        long version
    ) {
        CertificateId = certificateId;
        AccountId = accountId;
        OrderId = orderId;
        
        X509Certificates = x509Certificates;
        RevokationStatus = revokationStatus;

        Version = version;
    }

    protected CertificateContainer(SerializationInfo info, StreamingContext context)
    {
        ArgumentNullException.ThrowIfNull(info);

        CertificateId = new(info.GetRequiredString(nameof(CertificateId)));
        AccountId = new (info.GetRequiredString(nameof(AccountId)));
        OrderId = new(info.GetRequiredString(nameof(OrderId)));

        X509Certificates = info.GetRequiredValue<byte[]>(nameof(X509Certificates));
        RevokationStatus = info.GetEnumFromString<RevokationStatus>(nameof(RevokationStatus), RevokationStatus.NotRevoked);

        Version = info.GetInt64(nameof(Version));
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("SerializationVersion", 1);

        info.AddValue(nameof(CertificateId), CertificateId.Value);
        info.AddValue(nameof(OrderId), OrderId.Value);
        info.AddValue(nameof(AccountId), AccountId.Value);

        info.AddValue(nameof(X509Certificates), X509Certificates);
        info.AddValue(nameof(RevokationStatus), RevokationStatus.ToString());

        info.AddValue(nameof(Version), Version);
    }
}