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
            _logger.RequestHeaderUrlNotWellFormed();
            throw new MalformedRequestException("Header Url is not well-formed.");
        }

        if (header.Url != requestUrl)
        {
            _logger.RequestHeaderUrlMismatch();
            throw AcmeErrors.Unauthorized().AsException();
        }

        if (!_supportedAlgs.Contains(header.Alg))
        {
            _logger.RequestHeaderAlgorithmNotSupported(header.Alg);
            throw AcmeErrors.BadSignatureAlgorithm($"{header.Alg} is not supported.", _supportedAlgs).AsException();
        }

        if (header.Jwk != null && header.Kid != null)
        {
            _logger.RequestHeaderJwkAndKidPresent();
            throw new MalformedRequestException("Do not provide both Jwk and Kid.");
        }

        if (header.Jwk == null && header.Kid == null)
        {
            _logger.RequestHeaderNeitherJwkNorKid();
            throw new MalformedRequestException("Provide either Jwk or Kid.");
        }

        _logger.RequestHeadersValidated();
    }

    private async Task ValidateNonceAsync(string? nonce, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nonce))
        {
            _logger.NonceEmpty();
            throw AcmeErrors.BadNonce().AsException();
        }

        if (!await _nonceStore.TryConsumeNonceAsync(new Nonce(nonce), cancellationToken))
        {
            _logger.NonceInvalidOrReplayed();
            throw AcmeErrors.BadNonce().AsException();
        }

        _logger.NonceValidated();
    }
}