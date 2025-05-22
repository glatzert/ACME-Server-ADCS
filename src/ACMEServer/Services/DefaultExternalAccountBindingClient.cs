using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public class DefaultExternalAccountBindingClient : IExternalAccountBindingClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ACMEServerOptions> _options;
    private readonly ILogger<DefaultExternalAccountBindingClient> _logger;

    public DefaultExternalAccountBindingClient(
        HttpClient httpClient,
        IOptions<ACMEServerOptions> options,
        ILogger<DefaultExternalAccountBindingClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;

        //TODO: The headers will show up duplicated in testing ...
        var headers = _options.Value.ExternalAccountBinding!.Headers
            .ToLookup(
                x => x.Key, 
                x => x.Value);

        foreach (var header in headers) {
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.AsEnumerable().Distinct());
        }
    }

    public async Task<byte[]> GetEABHMACfromKidAsync(string kid, CancellationToken ct)
    {
        var requestUri = _options.Value.ExternalAccountBinding!
            .MACRetrievalUrl
            .Replace("{kid}", kid);

        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            var responseText = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Failed to retrieve MAC: ({StatusCode} - {ReasonPhrase}) {ResponseText}", (int)response.StatusCode, response.ReasonPhrase, responseText);
                throw AcmeErrors.ExternalAccountBindingFailed($"Failed to retrieve MAC: ({(int)response.StatusCode} - {response.StatusCode}) {responseText}").AsException();
            }

            return Base64UrlEncoder.DecodeBytes(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to retrieve MAC");
            throw AcmeErrors.ExternalAccountBindingFailed($"Failed to retrieve MAC: {ex.Message}").AsException();
        }
    }

    public Task SignalEABFailure(string kid)
    {
        var requestUri = _options.Value.ExternalAccountBinding!
            .FailedSignalUrl?
            .Replace("{kid}", kid);

        if (string.IsNullOrEmpty(requestUri))
            return Task.CompletedTask;

        return _httpClient.GetAsync(requestUri);
    }

    public Task SingalEABSucces(string kid)
    {
        var requestUri = _options.Value.ExternalAccountBinding!
            .SuccessSignalUrl?
            .Replace("{kid}", kid);

        if (string.IsNullOrEmpty(requestUri))
            return Task.CompletedTask;

        return _httpClient.GetAsync(requestUri);
    }
}
