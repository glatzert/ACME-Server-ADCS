using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

/// <summary>
/// Implements challenge validation as described in the ACME RFC 8555 (https://www.rfc-editor.org/rfc/rfc8555#section-8.3) for the "http-01" challenge type.
/// </summary>
public sealed class Http01ChallengeValidator(
    IHttpClientFactory httpClientFactory,
    IProfileProvider profileProvider,
    ILogger<Http01ChallengeValidator> logger) : StringTokenChallengeValidator(profileProvider, logger)
{
    public const string IgnoreCertHttpClientSuffix = "-IgnoreCert";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<Http01ChallengeValidator> _logger = logger;

    public override string ChallengeType => ChallengeTypes.Http01;
    public override IEnumerable<string> SupportedIdentiferTypes => [IdentifierTypes.DNS, IdentifierTypes.IP];

    protected override string GetExpectedContent(TokenChallenge challenge, Account account)
        => GetKeyAuthToken(challenge, account);

    protected override async Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(TokenChallenge challenge, ProfileConfiguration profileConfiguration, CancellationToken cancellationToken)
    {
        // TODO: Use a "trusted DNS Resolver" configuration to avoid DNS spoofing attacks.
        // then we can use the IP-Address to connect to the host and add a host header.
        var challengeUrl = $"http://{challenge.Authorization.Identifier.Value}/.well-known/acme-challenge/{challenge.Token}";

        try
        {
            var httpClientName = profileConfiguration.ChallengeValidation.Http01.IgnoreServerCertificate
                ? nameof(Http01ChallengeValidator) + IgnoreCertHttpClientSuffix
                : nameof(Http01ChallengeValidator);

            var httpClient = _httpClientFactory.CreateClient(httpClientName);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, challengeUrl);
            var response = await httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var error = AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, $"Got non 200 status code: {response.StatusCode}");
                return (null, error);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            content = content?.Trim() ?? ""; 

            _logger.Http01ChallengeResponseLoaded(challengeUrl, content);
            return ([content], null);
        }
        catch (HttpRequestException ex)
        {
            _logger.Http01ChallengeResponseFailed(challengeUrl, ex);

            var error = AcmeErrors.Connection(challenge.Authorization.Identifier, ex.Message);
            return (null, error);
        }
    }
}
