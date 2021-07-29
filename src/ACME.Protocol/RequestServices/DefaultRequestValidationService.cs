using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TGIT.ACME.Protocol.HttpModel.Requests;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Services;
using TGIT.ACME.Protocol.Storage;

namespace TGIT.ACME.Protocol.RequestServices
{
    public class DefaultRequestValidationService : IRequestValidationService
    {
        private readonly IAccountService _accountService;
        private readonly INonceStore _nonceStore;

        private readonly ILogger<DefaultRequestValidationService> _logger;

        private readonly string[] _supportedAlgs = new[] { "RS256" };

        public DefaultRequestValidationService(IAccountService accountService, INonceStore nonceStore,
            ILogger<DefaultRequestValidationService> logger)
        {
            _accountService = accountService;
            _nonceStore = nonceStore;
            _logger = logger;
        }

        public async Task ValidateRequestAsync(AcmeRawPostRequest request, AcmeHeader header,
            string requestUrl, CancellationToken cancellationToken)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (header is null)
                throw new ArgumentNullException(nameof(header));
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new ArgumentNullException(nameof(requestUrl));

            ValidateRequestHeader(header, requestUrl);
            await ValidateNonceAsync(header.Nonce, cancellationToken);
            await ValidateSignatureAsync(request, header, cancellationToken);
        }

        private void ValidateRequestHeader(AcmeHeader header, string requestUrl)
        {
            if (header is null)
                throw new ArgumentNullException(nameof(header));

            _logger.LogDebug("Attempting to validate AcmeHeader ...");

            if (!Uri.IsWellFormedUriString(header.Url, UriKind.RelativeOrAbsolute))
                throw new MalformedRequestException("Header Url is not well-formed.");

            if (header.Url != requestUrl)
                throw new NotAuthorizedException();

            if (!_supportedAlgs.Contains(header.Alg))
                throw new BadSignatureAlgorithmException();

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
                throw new BadNonceException();
            }

            if (!await _nonceStore.TryRemoveNonceAsync(new Nonce(nonce), cancellationToken))
            {
                _logger.LogDebug($"Nonce was invalid.");
                throw new BadNonceException();
            }

            _logger.LogDebug("successfully validated replay nonce.");
        }

        private async Task ValidateSignatureAsync(AcmeRawPostRequest request, AcmeHeader header, CancellationToken cancellationToken)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (header is null)
                throw new ArgumentNullException(nameof(header));

            _logger.LogDebug("Attempting to validate signature ...");

            var jwk = header.Jwk;
            if(jwk == null)
            {
                try
                {
                    var accountId = header.GetAccountId();
                    var account = await _accountService.LoadAcountAsync(accountId, cancellationToken);
                    jwk = account?.Jwk;
                } 
                catch (InvalidOperationException)
                {
                    throw new MalformedRequestException("KID could not be found.");
                }
            }

            if(jwk == null)
                throw new MalformedRequestException("Could not load JWK.");

            var securityKey = jwk.SecurityKey;
            
            using var signatureProvider = new AsymmetricSignatureProvider(securityKey, header.Alg);
            var plainText = System.Text.Encoding.UTF8.GetBytes($"{request.Header}.{request.Payload ?? ""}");
            var signature = Base64UrlEncoder.DecodeBytes(request.Signature);

            if (!signatureProvider.Verify(plainText, signature))
                throw new MalformedRequestException("The signature could not be verified");

            _logger.LogDebug("successfully validated signature.");
        }
    }
}
