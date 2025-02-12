using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Configuration;

namespace Th11s.ACMEServer.Services
{
    public class DefaultExternalAccountBindingClient : IExternalAccountBindingClient
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<ACMEServerOptions> _options;

        public DefaultExternalAccountBindingClient(
            HttpClient httpClient,
            IOptions<ACMEServerOptions> options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task<byte[]> GetEABHMACfromKidAsync(string kid, CancellationToken ct)
        {
            var requestUri = _options.Value.ExternalAccountBinding!
                .MACRetrievalUrl
                .Replace("{kid}", kid);

            var base64UrlEncodedMAC = await _httpClient.GetStringAsync(requestUri, ct);
            return Base64UrlEncoder.DecodeBytes(base64UrlEncodedMAC);
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
}
