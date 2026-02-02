using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model.CAA;

namespace Th11s.ACMEServer.Services;

public class CAAQueryHandler(
    [FromKeyedServices(nameof(CAAQueryHandler))] ILookupClient lookupClient,
    ILogger<CAAQueryHandler> logger
    ) : ICAAQueryHandler
{
    private readonly ILookupClient _lookupClient = lookupClient;
    private readonly ILogger<CAAQueryHandler> _logger = logger;

    public async Task<CAAQueryResults> GetCAAFromDomain(string domainName, CancellationToken cancellationToken)
    {
        // First, resolve CNAMEs to get the canonical domain name
        var canonicalDomainName = await QueryCanonicalDomainName(domainName, cancellationToken);

        // Then we can query the CAA records for the canonical domain name
        var caaQueryResponse = await _lookupClient.QueryAsync(canonicalDomainName, QueryType.CAA, cancellationToken: cancellationToken);
        var dnsCAAEntries = caaQueryResponse.Answers.CaaRecords().ToArray();

        // If there were no CAA records, repeat for the parent domain
        if (dnsCAAEntries.Length == 0 && domainName.Count(x => x == '.') > 1)
        {
            var parentDomain = domainName.Split('.', 2).Last();
            return await GetCAAFromDomain(parentDomain, cancellationToken);
        }

        if (dnsCAAEntries.Length > 0)
        {
            var caaEntries = dnsCAAEntries
                .Select(entry => CAAQueryResult.Parse(entry.Tag, entry.Flags, entry.Value))
                .ToList();

            return new CAAQueryResults(caaEntries);
        }

        return new CAAQueryResults([]);
    }

    private async Task<string> QueryCanonicalDomainName(string domainName, CancellationToken cancellationToken)
    {
        var canonicalName = domainName;
        var visitedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool responseWasCNAME;
        do
        {
            if (visitedNames.Contains(canonicalName))
            {
                // CNAME loop detected
                _logger.CnameLoopDetected(domainName);
                throw new InvalidOperationException("CNAME loop detected when querying CAA records.");
            }

            var cnameResult = await _lookupClient.QueryAsync(canonicalName, QueryType.CNAME, cancellationToken: cancellationToken);
            var cnameRecords = cnameResult.Answers.CnameRecords().ToArray();

            if (responseWasCNAME = cnameRecords.Length > 0)
            {
                var nextCanonicalName = cnameRecords.First().CanonicalName.Value.TrimEnd('.');
                _logger.CnameRecordFound(canonicalName, nextCanonicalName);

                visitedNames.Add(canonicalName);
                canonicalName = nextCanonicalName;
            }
        }
        while (responseWasCNAME);
        
        return canonicalName;
    }
}
