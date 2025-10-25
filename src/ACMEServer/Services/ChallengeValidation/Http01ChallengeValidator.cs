using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

/// <summary>
/// Implements challenge validation as described in the ACME RFC 8555 (https://www.rfc-editor.org/rfc/rfc8555#section-8.3) for the "http-01" challenge type.
/// </summary>
public sealed class Http01ChallengeValidator(HttpClient httpClient, ILogger<Http01ChallengeValidator> logger) : StringTokenChallengeValidator(logger)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<Http01ChallengeValidator> _logger = logger;

    public override string ChallengeType => ChallengeTypes.Http01;
    public override IEnumerable<string> SupportedIdentiferTypes => [IdentifierTypes.DNS, IdentifierTypes.IP];

    protected override string GetExpectedContent(Challenge challenge, Account account)
        => GetKeyAuthToken(challenge, account);

    protected override async Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(Challenge challenge, CancellationToken cancellationToken)
    {
        // TODO: Use a "trusted DNS Resolver" configuration to avoid DNS spoofing attacks.
        // then we can use the IP-Address to connect to the host and add a host header.
        var challengeUrl = $"http://{challenge.Authorization.Identifier.Value}/.well-known/acme-challenge/{challenge.Token}";

        try
        {
            var response = await _httpClient.GetAsync(new Uri(challengeUrl), cancellationToken);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var error = AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, $"Got non 200 status code: {response.StatusCode}");
                return (null, error);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            content = content?.Trim() ?? "";

            _logger.LogInformation("Loaded http-01 challenge response from {challengeUrl}: {content}", challengeUrl, content);
            return ([content], null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogInformation("Could not load http-01 challenge response from {challengeUrl}", challengeUrl);

            var error = AcmeErrors.Connection(challenge.Authorization.Identifier, ex.Message);
            return (null, error);
        }
    }
}
