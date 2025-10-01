using System.Diagnostics;

namespace Th11s.ACMEServer.Model.Primitives;

[DebuggerDisplay("Certificate: {Value}")]
[DebuggerStepThrough]
public record class CertificateId : ResourceIdentifier
{
    public CertificateId() : base()
    { }

    public CertificateId(string value) : base(value)
    { }
}

