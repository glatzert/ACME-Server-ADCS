using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.HttpModel.Services;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.RequestServices;

public class DefaultRequestValidationService : IRequestValidationService
{
    private readonly IAccountService _accountService;
    private readonly INonceStore _nonceStore;

    private readonly ILogger<DefaultRequestValidationService> _logger;

    private readonly string[] _supportedAlgs = ["RS256", "ES256", "ES384", "ES512"];

    public DefaultRequestValidationService(IAccountService accountService, INonceStore nonceStore,
        ILogger<DefaultRequestValidationService> logger)
    {
        _accountService = accountService;
        _nonceStore = nonceStore;
        _logger = logger;
    }

    public async Task ValidateRequestAsync(AcmeJwsToken request,
        string requestUrl, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(requestUrl))
            throw new ArgumentNullException(nameof(requestUrl));

        ValidateRequestHeader(request.AcmeHeader, requestUrl);
        await ValidateNonceAsync(request.AcmeHeader.Nonce, cancellationToken);
        //await ValidateSignatureAsync(request, cancellationToken);
    }

    private void ValidateRequestHeader(AcmeJwsHeader header, string requestUrl)
    {
        if (header is null)
            throw new ArgumentNullException(nameof(header));

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

    private async Task ValidateSignatureAsync(AcmeJwsToken request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogDebug("Attempting to validate signature ...");

        var jwk = request.AcmeHeader.Jwk;
        if (jwk == null)
        {
            try
            {
                var accountId = request.AcmeHeader.GetAccountId();
                var account = await _accountService.LoadAcountAsync(accountId, cancellationToken);
                jwk = account?.Jwk;
            }
            catch (InvalidOperationException)
            {
                throw new MalformedRequestException("KID could not be found.");
            }
        }

        if (jwk == null)
            throw new MalformedRequestException("Could not load JWK.");

        var securityKey = jwk.SecurityKey;

        var signatureProvider = TryCreateSignatureProvider(securityKey, request.AcmeHeader.Alg);
        if (signatureProvider == null)
            throw AcmeErrors.BadSignatureAlgorithm("A signature provider could not be created.", _supportedAlgs).AsException();

        using (signatureProvider)
        {
            var plainText = System.Text.Encoding.UTF8.GetBytes($"{request.Protected}.{request.Payload ?? ""}");

            if (!signatureProvider.Verify(plainText, request.SignatureBytes))
            {
                throw AcmeErrors.InvalidSignature().AsException();
            }
        }

        _logger.LogDebug("Successfully validated signature.");
    }


    private AsymmetricSignatureProvider? TryCreateSignatureProvider(SecurityKey securityKey, string alg)
    {
        try
        {
            return new AsymmetricSignatureProvider(securityKey, alg);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Error creating AsymmetricSignatureProvider");
            return null;
        }
    }
}