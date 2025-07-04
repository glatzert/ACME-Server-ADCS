﻿using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Services;

public class DefaultExternalAccountBindingValidator(
    IExternalAccountBindingClient eabClient,
    ILogger<DefaultExternalAccountBindingValidator> logger) : IExternalAccountBindingValidator
{
    private static readonly HashSet<string> _hmacAlgorithms = ["HS256", "HS384", "HS512"];

    private readonly IExternalAccountBindingClient _eabClient = eabClient;
    private readonly ILogger<DefaultExternalAccountBindingValidator> _logger = logger;

    public async Task<AcmeError?> ValidateExternalAccountBindingAsync(AcmeJwsHeader requestHeader, AcmeJwsToken externalAccountBinding, CancellationToken ct)
    {
        if (!_hmacAlgorithms.Contains(externalAccountBinding.AcmeHeader.Alg))
        {
            _logger.LogDebug("External account binding JWS header alg is not a HMAC algorithm: {alg}", externalAccountBinding.AcmeHeader.Alg);
            return AcmeErrors.ExternalAccountBindingFailed("JWS header may only indicate HMAC algs like HS256");
        }

        if (externalAccountBinding.AcmeHeader.Nonce != null)
        {
            _logger.LogDebug("External account binding JWS header contains a nonce: {nonce}", externalAccountBinding.AcmeHeader.Nonce);
            return AcmeErrors.ExternalAccountBindingFailed("JWS header may not contain a nonce.");
        }

        if (requestHeader.Url != externalAccountBinding.AcmeHeader.Url)
        {
            _logger.LogDebug("External account binding JWS header url: {url} does not match request url: {requestUrl}", externalAccountBinding.AcmeHeader.Url, requestHeader.Url);
            return AcmeErrors.ExternalAccountBindingFailed("JWS header and request JWS header need to have the same url.");
        }

        if (requestHeader.Jwk!.Json != Base64UrlEncoder.Decode(externalAccountBinding.Payload))
        {
            _logger.LogDebug("External account binding JWS payload: {payload} does not match request JWK: {jwk}", externalAccountBinding.Payload, requestHeader.Jwk.Json);
            return AcmeErrors.ExternalAccountBindingFailed("JWS payload and request JWS header JWK need to be identical.");
        }

        if (externalAccountBinding.AcmeHeader.Kid == null)
        {
            _logger.LogDebug("External account binding JWS header does not contain a kid: {kid}", externalAccountBinding.AcmeHeader.Kid);
            return AcmeErrors.ExternalAccountBindingFailed("JWS header must contain a kid.");
        }

        try
        {
            var eabMACKey = await _eabClient.GetEABHMACfromKidAsync(externalAccountBinding.AcmeHeader.Kid, ct);
            _logger.LogDebug("Retrieved external account binding MAC key: {Key} for Kid: {kid}", eabMACKey, externalAccountBinding.AcmeHeader.Kid);

            var symmetricKey = new SymmetricSignatureProvider(new SymmetricSecurityKey(eabMACKey), externalAccountBinding.AcmeHeader.Alg);
            var plainText = System.Text.Encoding.UTF8.GetBytes($"{externalAccountBinding.Protected}.{externalAccountBinding.Payload ?? ""}");
            var isEabMacValid = symmetricKey.Verify(plainText, externalAccountBinding.SignatureBytes);

            if (!isEabMacValid)
            {
                _logger.LogDebug("External account binding MAC key: {Key} for Kid: {kid} is invalid", eabMACKey, externalAccountBinding.AcmeHeader.Kid);

                _ = _eabClient.SignalEABFailure(externalAccountBinding.AcmeHeader.Kid);
                return AcmeErrors.ExternalAccountBindingFailed("externalAccountBinding JWS signature is invalid.");
            }

            _logger.LogDebug("External account binding MAC key: {Key} for Kid: {kid} is valid", eabMACKey, externalAccountBinding.AcmeHeader.Kid);
            _ = _eabClient.SingalEABSucces(externalAccountBinding.AcmeHeader.Kid);
            return null;
        }
        catch (Exception ex) 
        {
            _logger.LogWarning(ex, "Error during External Account Binding validation");
            _ = _eabClient.SignalEABFailure(externalAccountBinding.AcmeHeader.Kid);

            if (ex is AcmeErrorException acmeErrorException)
                return acmeErrorException.Error;

            return AcmeErrors.ExternalAccountBindingFailed($"Signature validation failed");
        }

    }
}
