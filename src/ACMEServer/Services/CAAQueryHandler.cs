using DnsClient;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public class CAAQueryHandler() : ICAAQueryHandler
{
    public async Task<CAAQueryResult> GetCAAFromDomain(string domainName, CancellationToken cancellationToken)
    {
        var lookupClient = new LookupClient();

        // First, resolve CNAMEs to get the canonical domain name
        var canonicalDomainName = await QueryCanonicalDomainName(domainName, lookupClient, cancellationToken);

        // Then we can query the CAA records for the canonical domain name


        // If there were no CAA records, repeat for the parent domain


        return new();
    }

    private static async Task<string> QueryCanonicalDomainName(string domainName, LookupClient lookupClient, CancellationToken cancellationToken)
    {
        var effectiveDomainName = domainName;
        var seenDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool responseWasCNAME;
        do
        {
            if (seenDomains.Contains(effectiveDomainName))
            {
                // CNAME loop detected
                throw new InvalidOperationException("CNAME loop detected when querying CAA records.");
            }

            var cnameResult = await lookupClient.QueryAsync(effectiveDomainName, QueryType.CNAME, cancellationToken: cancellationToken);

            if (responseWasCNAME = cnameResult.Answers.CnameRecords().Any())
            {
                effectiveDomainName = cnameResult.Answers.CnameRecords().First().CanonicalName.Value.TrimEnd('.');
                seenDomains.Add(effectiveDomainName);
            }
        } while (responseWasCNAME);
        return effectiveDomainName;
    }
}
