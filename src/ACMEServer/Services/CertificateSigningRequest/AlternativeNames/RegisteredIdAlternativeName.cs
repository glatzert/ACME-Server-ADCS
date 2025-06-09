namespace Th11s.ACMEServer.Services.CertificateSigningRequest.AlternativeNames
{
    internal sealed class RegisteredIdAlternativeName : AlternativeName
    {
        public required string RegisteredId { get; init; }
    }
}
