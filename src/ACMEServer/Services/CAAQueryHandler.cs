using DnsClient;
using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public class CAAQueryHandler(ILogger<CAAQueryHandler> logger) : ICAAQueryHandler
{
    private readonly ILogger<CAAQueryHandler> _logger = logger;

    public async Task<CAAQueryResult> GetCAAFromDomain(string domainName, CancellationToken cancellationToken)
    {
        var lookupClient = new LookupClient();

        // First, resolve CNAMEs to get the canonical domain name
        var canonicalDomainName = await QueryCanonicalDomainName(domainName, lookupClient, cancellationToken);

        // Then we can query the CAA records for the canonical domain name


        // If there were no CAA records, repeat for the parent domain
        if (caaEntries.Length == 0 && domainName.Count(x => x == '.') > 1)
        {
            var parentDomain = domainName.Split('.', 2).Last();
            return await GetCAAFromDomain(parentDomain, cancellationToken);
        }

        return CAAQueryResult.Empty;
    }

    private async Task<string> QueryCanonicalDomainName(string domainName, LookupClient lookupClient, CancellationToken cancellationToken)
    {
        var canonicalName = domainName;
        var visitedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool responseWasCNAME;
        do
        {
            if (visitedNames.Contains(canonicalName))
            {
                // CNAME loop detected
                _logger.LogWarning("CNAME loop detected when querying CAA records for domain {DomainName}", domainName);
                throw new InvalidOperationException("CNAME loop detected when querying CAA records.");
            }

            var cnameResult = await lookupClient.QueryAsync(canonicalName, QueryType.CNAME, cancellationToken: cancellationToken);
            var cnameRecords = cnameResult.Answers.CnameRecords().ToArray();

            if (responseWasCNAME = cnameRecords.Length > 0)
            {
                var nextCanonicalName = cnameRecords.First().CanonicalName.Value.TrimEnd('.');
                _logger.LogDebug("CNAME record found for {DomainName}, pointing to {CanonicalName}", canonicalName, nextCanonicalName);

                visitedNames.Add(canonicalName);
                canonicalName = nextCanonicalName;
            }
            }
        while (responseWasCNAME);
        
        return canonicalName;
    }
}
