using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

public sealed class Http01ChallengeValidator : StringTokenChallengeValidator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Http01ChallengeValidator> _logger;

    public Http01ChallengeValidator(HttpClient httpClient, ILogger<Http01ChallengeValidator> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public override string ChallengeType => ChallengeTypes.Http01;

    protected override string GetExpectedContent(Challenge challenge, Account account)
        => GetKeyAuthToken(challenge, account);

    protected override async Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(Challenge challenge, CancellationToken cancellationToken)
    {
        var challengeUrl = $"http://{challenge.Authorization.Identifier.Value}/.well-known/acme-challenge/{challenge.Token}";

        try
        {
            var response = await _httpClient.GetAsync(new Uri(challengeUrl), cancellationToken);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var error = new AcmeError("incorrectResponse", $"Got non 200 status code: {response.StatusCode}", challenge.Authorization.Identifier);
                return (null, error);
            }

            var content = await response.Content.ReadAsStringAsync();
            content = content?.Trim() ?? "";

            _logger.LogInformation($"Loaded http-01 challenge response from {challengeUrl}: {content}");
            return (new List<string> { content }, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogInformation($"Could not load http-01 challenge response from {challengeUrl}");

            var error = new AcmeError("connection", ex.Message, challenge.Authorization.Identifier);
            return (null, error);
        }
    }
}
