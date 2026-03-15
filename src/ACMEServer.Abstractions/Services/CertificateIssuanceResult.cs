using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public class CertificateIssuanceResult
{
    private CertificateIssuanceResult(X509Certificate2Collection? certificates, AcmeError? error, Dictionary<string, string>? metadata)
    {
        Certificates = certificates;
        Error = error;
        Metadata = metadata ?? [];
    }

    public CertificateIssuanceResult(X509Certificate2Collection certificates, Dictionary<string, string>? metadata = null)
        : this(certificates, null, metadata)
    { }

    public CertificateIssuanceResult(AcmeError error)
        : this(null, error, null)
    { }

    [MemberNotNullWhen(true, nameof(Certificates))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Certificates is not null && Error is null;

    public X509Certificate2Collection? Certificates { get; }

    public AcmeError? Error { get; }

    public Dictionary<string, string> Metadata { get; } = [];
}
