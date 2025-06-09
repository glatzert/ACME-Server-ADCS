namespace Th11s.ACMEServer.Services.CertificateSigningRequest.AlternativeNames
{
    internal sealed class DnsAlternativeName : AlternativeName
    {
        public required string DnsName { get; init; }
    }
}
