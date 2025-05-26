using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.RequestServices;

public class DefaultRequestValidationService(INonceStore nonceStore,
    ILogger<DefaultRequestValidationService> logger) : IRequestValidationService
{
    private readonly INonceStore _nonceStore = nonceStore;

    private readonly ILogger<DefaultRequestValidationService> _logger = logger;

    private readonly string[] _supportedAlgs = ["RS256", "ES256", "ES384", "ES512"];

    public async Task ValidateRequestAsync(AcmeJwsToken request,
        string requestUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUrl);

        ValidateRequestHeader(request.AcmeHeader, requestUrl);
        await ValidateNonceAsync(request.AcmeHeader.Nonce, cancellationToken);
        //await ValidateSignatureAsync(request, cancellationToken);
    }

    private void ValidateRequestHeader(AcmeJwsHeader header, string requestUrl)
    {
        ArgumentNullException.ThrowIfNull(header);

        if (!Uri.IsWellFormedUriString(header.Url, UriKind.RelativeOrAbsolute))
        {
            _logger.LogDebug("Request header validation failed due to header url not being well-formend");
            throw new MalformedRequestException("Header Url is not well-formed.");
        }

        if (header.Url != requestUrl)
        {
            _logger.LogWarning("Request header validation failed du to header url not matching actual path");
            throw AcmeErrors.Unauthorized().AsException();
        }

        if (!_supportedAlgs.Contains(header.Alg))
        {
            _logger.LogDebug("Request header validation failed due to algorithm '{alg}' not being supported.", header.Alg);
            throw AcmeErrors.BadSignatureAlgorithm($"{header.Alg} is not supported.", _supportedAlgs).AsException();
        }

        if (header.Jwk != null && header.Kid != null)
        {
            _logger.LogDebug("Request header validation failed due to Jwk and Kid being present at the same time.");
            throw new MalformedRequestException("Do not provide both Jwk and Kid.");
        }

        if (header.Jwk == null && header.Kid == null)
        {
            _logger.LogDebug("Request header validation failed due to neither Jwk nor Kid being present.");
            throw new MalformedRequestException("Provide either Jwk or Kid.");
        }

        _logger.LogDebug("Request headers have been successfully validated.");
    }

    private async Task ValidateNonceAsync(string? nonce, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nonce))
        {
            _logger.LogDebug($"Replay nonce could not be validated: Nonce was empty.");
            throw AcmeErrors.BadNonce().AsException();
        }

        if (!await _nonceStore.TryRemoveNonceAsync(new Nonce(nonce), cancellationToken))
        {
            _logger.LogDebug($"Replay nonce could not be validated: Nonce was invalid or replayed.");
            throw AcmeErrors.BadNonce().AsException();
        }

        _logger.LogDebug("Replay-nonce has been successfully validated.");
    }
}