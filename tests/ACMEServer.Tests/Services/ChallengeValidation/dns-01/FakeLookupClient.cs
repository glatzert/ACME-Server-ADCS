using DnsClient;
using DnsClient.Protocol;
using System.Net;
using Th11s.ACMEServer.Services.ChallengeValidation;

namespace Th11s.ACMEServer.Tests.Services.ChallengeValidation.dns_01;

internal class FakeLookupClient : ILookupClient
{
    public bool HasAnsweredQuestion { get; private set; } = false;

    public Dictionary<string, string[]> TxtRecords { get; } = new();

    public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
    {
        if (queryType != QueryType.TXT) {
            HasAnsweredQuestion = true;
            return Task.FromResult(new FakeDnsQueryResponse() as IDnsQueryResponse);
        }

        if (queryClass != QueryClass.IN)
        {
            HasAnsweredQuestion = true;
            return Task.FromResult(new FakeDnsQueryResponse() as IDnsQueryResponse);
        }

        if (!query.StartsWith($"{Dns01ChallengeValidator.DnsRecordPrefix}."))
        {
            HasAnsweredQuestion = true;
            return Task.FromResult(new FakeDnsQueryResponse() as IDnsQueryResponse);
        }

        var domain = query.Split('.', 2).Last();
        if (!TxtRecords.TryGetValue(domain, out var txtContent))
        {
            HasAnsweredQuestion = true;
            return Task.FromResult(new FakeDnsQueryResponse() as IDnsQueryResponse);
        }

        var txtRecord = new TxtRecord(
            new (
                DnsString.Parse(query),
                ResourceRecordType.TXT,
                QueryClass.IN,
                300,
                100
            ), 
            txtContent, 
            txtContent);

        HasAnsweredQuestion = true;
        return Task.FromResult(new FakeDnsQueryResponse()
        {
            Answers = [ txtRecord ],
            AllRecords = [ txtRecord ],
            Questions = [ new DnsQuestion(DnsString.Parse(query), QueryType.TXT, QueryClass.IN) ]
        } as IDnsQueryResponse);
    }

    #region Unused Members

    public Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, DnsQueryAndServerOptions queryOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }


    public IReadOnlyCollection<NameServer> NameServers => throw new NotImplementedException();

    public LookupClientSettings Settings => throw new NotImplementedException();

    public IDnsQueryResponse Query(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse Query(DnsQuestion question)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse Query(DnsQuestion question, DnsQueryAndServerOptions queryOptions)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryCache(DnsQuestion question)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryCache(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryReverse(IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryReverse(IPAddress ipAddress, DnsQueryAndServerOptions queryOptions)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, DnsQueryAndServerOptions queryOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, DnsQuestion question)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, DnsQuestion question, DnsQueryOptions queryOptions)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryServer(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryServer(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, DnsQuestion question, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, DnsQuestion question, DnsQueryOptions queryOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, DnsQueryOptions queryOptions)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, DnsQueryOptions queryOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    #endregion
}

internal class FakeDnsQueryResponse : IDnsQueryResponse
{
    public IReadOnlyList<DnsQuestion> Questions { get; set; } = [];
    public IReadOnlyList<DnsResourceRecord> Additionals { get; set; } = [];
    public IEnumerable<DnsResourceRecord> AllRecords { get; set; } = [];
    
    public IReadOnlyList<DnsResourceRecord> Answers { get; set; } = [];
    
    public IReadOnlyList<DnsResourceRecord> Authorities { get; set; } = [];
    public string AuditTrail { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public bool HasError { get; set; } = false;
    public DnsResponseHeader Header { get; set; } = new DnsResponseHeader(1, 0, 0, 0 ,0 ,0);
    public int MessageSize => throw new NotImplementedException();
    public NameServer NameServer => throw new NotImplementedException();
    public DnsQuerySettings Settings => throw new NotImplementedException();
}