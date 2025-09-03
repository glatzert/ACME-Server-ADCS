using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Th11s.ACMEServer.Services.ChallengeValidation
{
    public class DeviceAttest01RemoteValidator(
        HttpClient httpClient,
        ILogger<DeviceAttest01RemoteValidator> logger
        ) : IDeviceAttest01RemoteValidator
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<DeviceAttest01RemoteValidator> _logger = logger;

        public async Task<bool> ValidateAsync(string requestUri, Dictionary<string, object?> payload, CancellationToken ct)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = JsonContent.Create(payload);

            try
            {
                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Remote DeviceAttest01 validation indicated non success status code: {code}", (int)response.StatusCode);
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<RemoteDeviceAttest01Result>(cancellationToken: ct);
                return result?.IsValid ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve DeviceAttest01 result.");
                return false;
            }
        }
    }

    public class RemoteDeviceAttest01Result
    {
        /// <summary>
        /// Indicates whether the attestation validation was successful.
        /// </summary>
        public bool IsValid { get; set; }
    }
}
