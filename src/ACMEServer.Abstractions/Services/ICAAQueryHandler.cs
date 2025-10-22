using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface ICAAQueryHandler
{
    Task<CAAQueryResult> GetCAAFromDomain(string domainName);
}