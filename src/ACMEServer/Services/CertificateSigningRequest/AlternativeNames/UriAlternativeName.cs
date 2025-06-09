namespace Th11s.ACMEServer.Services.CertificateSigningRequest.AlternativeNames
{
    internal sealed class UriAlternativeName : AlternativeName
    {
        public required string Uri { get; init; }
    }
}
