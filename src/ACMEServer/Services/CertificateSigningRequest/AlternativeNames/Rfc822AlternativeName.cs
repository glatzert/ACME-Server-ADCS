namespace Th11s.ACMEServer.Services.CertificateSigningRequest.AlternativeNames
{
    internal sealed class Rfc822AlternativeName : AlternativeName
    {
        public required string EmailAddress { get; init; }
    }
}
