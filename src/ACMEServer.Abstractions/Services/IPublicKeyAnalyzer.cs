using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface IPublicKeyAnalyzer
{
    Task<PublicKeyInfo?> AnalyzePublicKeyAsync(string certificateSigningRequest, CancellationToken cancellationToken);
}
