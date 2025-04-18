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

        _logger.LogDebug("Attempting to validate AcmeHeader ...");

        if (!Uri.IsWellFormedUriString(header.Url, UriKind.RelativeOrAbsolute))
            throw new MalformedRequestException("Header Url is not well-formed.");

        if (header.Url != requestUrl)
            throw AcmeErrors.Unauthorized().AsException();

        if (!_supportedAlgs.Contains(header.Alg))
            throw AcmeErrors.BadSignatureAlgorithm($"{header.Alg} is not supported.", _supportedAlgs).AsException();

        if (header.Jwk != null && header.Kid != null)
            throw new MalformedRequestException("Do not provide both Jwk and Kid.");
        if (header.Jwk == null && header.Kid == null)
            throw new MalformedRequestException("Provide either Jwk or Kid.");

        _logger.LogDebug("successfully validated AcmeHeader.");
    }

    private async Task ValidateNonceAsync(string? nonce, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to validate replay nonce ...");
        if (string.IsNullOrWhiteSpace(nonce))
        {
            _logger.LogDebug($"Nonce was empty.");
            throw AcmeErrors.BadNonce().AsException();
        }

        if (!await _nonceStore.TryRemoveNonceAsync(new Nonce(nonce), cancellationToken))
        {
            _logger.LogDebug($"Nonce was invalid.");
            throw AcmeErrors.BadNonce().AsException();
        }

        _logger.LogDebug("successfully validated replay nonce.");
    }
}