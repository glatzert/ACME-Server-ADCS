using Th11s.ACMEServer.Model.CAA;

namespace Th11s.ACMEServer.Services;

public interface ICAAQueryHandler
{
    Task<CAAQueryResults> GetCAAFromDomain(string domainName, CancellationToken cancellationToken);
}