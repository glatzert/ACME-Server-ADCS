using Microsoft.IdentityModel.Tokens;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Unicode;
using Th11s.ACMEServer.Model.Extensions;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

[Serializable]
public class OrderCertificates : IVersioned, ISerializable
{
    public OrderCertificates(AccountId accountId, OrderId orderId, X509Certificate2Collection x509Certificates)
    {
        var leafCertificate = x509Certificates.GetLeafCertificate();

        // We'll use the identifier also used by ARI: <base64url(AKI keyIdentifier)>.<base64url(Serial)>
        var authorityKeyIdentifier = leafCertificate.Extensions.OfType<X509AuthorityKeyIdentifierExtension>()
            .FirstOrDefault()?.KeyIdentifier?.ToArray() ?? Encoding.ASCII.GetBytes("AKI not found");
        var serialNumber = leafCertificate.SerialNumberBytes.ToArray();

        CertificateId = new($"{Base64UrlEncoder.Encode(authorityKeyIdentifier)}.{Base64UrlEncoder.Encode(serialNumber)}");

        AccountId = accountId;
        OrderId = orderId;

        X509Certificates = x509Certificates.Export(X509ContentType.Pfx)!;
    }


    public CertificateId CertificateId { get; }
    public AccountId AccountId { get; }
    public OrderId OrderId { get; }

    public byte[] X509Certificates { get; }


    /// <summary>
    /// Concurrency Token
    /// </summary>
    public long Version { get; set; }


    protected OrderCertificates(SerializationInfo info, StreamingContext context)
    {
        ArgumentNullException.ThrowIfNull(info);

        CertificateId = new(info.GetRequiredString(nameof(CertificateId)));
        AccountId = new (info.GetRequiredString(nameof(AccountId)));
        OrderId = new(info.GetRequiredString(nameof(OrderId)));

        X509Certificates = info.GetRequiredValue<byte[]>(nameof(X509Certificate));

        Version = info.GetInt64(nameof(Version));
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("SerializationVersion", 1);

        info.AddValue(nameof(CertificateId), CertificateId.Value);
        info.AddValue(nameof(OrderId), OrderId.Value);
        info.AddValue(nameof(AccountId), AccountId.Value);

        info.AddValue(nameof(X509Certificate), X509Certificates);

        info.AddValue(nameof(Version), Version);
    }
}