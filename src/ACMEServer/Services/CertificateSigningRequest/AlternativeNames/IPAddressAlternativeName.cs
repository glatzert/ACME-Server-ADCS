using System.Net;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest.AlternativeNames
{
    internal sealed class IPAddressAlternativeName : AlternativeName
    {
        public required IPAddress IPAddress { get; init; }
    }
}
