namespace Th11s.ACMEServer.Services.CertificateSigningRequest.AlternativeNames
{
    internal sealed class OtherAlternativeNames
    {
        internal sealed class PermanentIdentifier : OtherAlternativeName
        {
            public string? Value { get; init; }
            public string? Assigner { get; init; }
        }
    }
}
