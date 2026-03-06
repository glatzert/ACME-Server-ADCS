using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface IPublicKeyAnalyzer
{
    Task AnalyzePublicKeyAsync(Order order, CancellationToken cancellationToken);
}
