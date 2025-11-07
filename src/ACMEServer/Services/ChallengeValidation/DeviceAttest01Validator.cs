using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Extensions;

namespace Th11s.ACMEServer.Services.ChallengeValidation;


/// <summary>
/// Implements challenge validation as described in the Draft (https://datatracker.ietf.org/doc/html/draft-acme-device-attest-03) for the "device-attest-01" challenge type.
/// </summary>
public sealed class DeviceAttest01ChallengeValidator(
    IDeviceAttest01RemoteValidator remoteValidatorClient,
    IOptionsSnapshot<ProfileConfiguration> options,
    ILogger<DeviceAttest01ChallengeValidator> logger
    ) : ChallengeValidator(logger)
{
    private readonly IDeviceAttest01RemoteValidator _remoteValidatorClient = remoteValidatorClient;
    private readonly IOptionsSnapshot<ProfileConfiguration> _options = options;
    private readonly ILogger<DeviceAttest01ChallengeValidator> _logger = logger;

    private class ChallengePayload
    {
        [JsonPropertyName("attObj")]
        public required string AttestationObject { get; set; }
    }

    public override string ChallengeType => ChallengeTypes.DeviceAttest01;
    public override IEnumerable<string> SupportedIdentiferTypes => [IdentifierTypes.PermanentIdentifier];


    protected override Task<ChallengeValidationResult> ValidateChallengeInternalAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
    {
        if (challenge.Payload is null)
        {
            return Task.FromResult(
                ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The challenge payload was empty."))
            );
        }

        var profileConfiguration = _options.Get(challenge.Authorization.Order.Profile.Value);
        if (profileConfiguration is null)
        {
            _logger.LogError("No configuration found for profile '{profile}'", challenge.Authorization.Order.Profile);
            return Task.FromResult(
                ChallengeValidationResult.Invalid(AcmeErrors.InvalidProfile(challenge.Authorization.Order.Profile))
            );
        }

        return ValidateWebAuthNAttestation(challenge, account, profileConfiguration.ChallengeValidation.DeviceAttest01, cancellationToken);
    }


    private async Task<ChallengeValidationResult> ValidateWebAuthNAttestation(Challenge challenge, Account account, DeviceAttest01Parameters parameters, CancellationToken cancellationToken)
    {
        // Deserialize the outer challenge payload
        var challengePayload = challenge.Payload.DeserializeBase64UrlEncodedJson<ChallengePayload>();
        if (challengePayload?.AttestationObject is null)
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The attestation object was empty."));
        }

        var challengePayloadBytes = Base64UrlEncoder.DecodeBytes(challengePayload.AttestationObject);

        var cborReader = new CborReader(challengePayloadBytes, CborConformanceMode.Ctap2Canonical);

        // Read the CBOR object
        cborReader.ReadStartMap();
        if (cborReader.ReadTextString() != "fmt")
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The attestation object did not start with 'fmt'."));
        }

        var fmt = cborReader.ReadTextString();
        return fmt switch
        {
            "apple" => await ValidateAppleAttestation(cborReader, challenge, account, parameters, cancellationToken),
            _ => ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, $"The attestation object format '{fmt}' is not supported.")),
        };
    }


    /// <summary>
    /// Validates the Apple attestation object.
    /// https://support.apple.com/en-gb/guide/security/sec8a37b4cb2/web
    /// https://www.w3.org/TR/webauthn-2/#sctn-apple-anonymous-attestation
    /// </summary>
    private async Task<ChallengeValidationResult> ValidateAppleAttestation(CborReader cborReader, Challenge challenge, Account account, DeviceAttest01Parameters parameters, CancellationToken cancellationToken)
    {
        if (cborReader.ReadTextString() != "attStmt")
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The attestation object did not have 'attStmt' after 'fmt'."));
        }

        cborReader.ReadStartMap();
        if (cborReader.ReadTextString() != "x5c")
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The attestation object did not have 'x5c' in 'attStmt'."));
        }

        cborReader.ReadStartArray();

        List<X509Certificate2> x509Certs = [];
        while (cborReader.PeekState() != CborReaderState.EndArray)
        {
            var bytes = cborReader.ReadByteString();
#if NET10_0_OR_GREATER
            x509Certs.Add(X509CertificateLoader.LoadCertificate(bytes));
#else
            x509Certs.Add(new(bytes));
#endif
        }

        if (x509Certs.Count == 0)
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The attestation object did not have any certificates in 'x5c' in 'attStmt'."));
        }


        if (!IsCertificateChainValid(x509Certs, parameters.Apple.RootCertificates))
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The attestation object did not have a valid certificate chain."));
        }

        var x509CredCert = x509Certs[0];

        // While this is neither defined in the WebAuthN spec nor the device-attest-01 spec, it contains the challenge-token
        var freshnessCode = x509CredCert.Extensions.OfType<X509Extension>()
            .Where(x => x.Oid?.Value == "1.2.840.113635.100.8.11.1")
            .Select(x => x.RawData)
            .ToList();

        if (freshnessCode.Count != 1)
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The attestation object did contain multiply or no freshness-codes (OID: 1.2.840.113635.100.8.11.1)"));
        }

        // The spec wants the KeyAuthorization, but it seems like the Apple devices send the challenge-token
        var expectedFreshnessCode = SHA256.HashData(Encoding.UTF8.GetBytes(challenge.Token));

        if (!freshnessCode[0].SequenceEqual(expectedFreshnessCode))
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The freshness code did not match the expected value."));
        }

        // Store the public key of the device-attestation certificate in the identifier, since we need to use it for the csr validation.
        challenge.Authorization.Identifier.Metadata[Identifier.MetadataKeys.PublicKey] = Convert.ToBase64String(x509CredCert.PublicKey.ExportSubjectPublicKeyInfo());

        if (parameters.HasRemoteUrl)
        {
            var remoteParameters = new Dictionary<string, object?>()
            {
                ["account"] = new Dictionary<string, object?>
                {
                    ["id"] = account.AccountId,
                    ["eab"] = account.ExternalAccountBinding
                },

                ["challenge"] = new Dictionary<string, object?>
                {
                    ["type"] = "device-attest-01",
                    ["id"] = challenge.ChallengeId,
                    ["payload"] = challenge.Payload,

                    ["identifier"] = new Dictionary<string, object?>
                    {
                        ["type"] = challenge.Authorization.Identifier.Type,
                        ["value"] = challenge.Authorization.Identifier.Value
                    }
                },

                ["attestation"] = new Dictionary<string, object?>
                {
                    ["format"] = "apple",

                    ["certificates"] = x509Certs.Select(cert => cert.ExportCertificatePem()).ToArray(),
                    ["cred-cert-extensions"] = x509CredCert.Extensions
                        .Where(ext => ext.Oid?.Value is not null)
                        .ToDictionary(ext => ext.Oid!.Value!, ext => ext.RawData)
                }
            };


            if (!await _remoteValidatorClient.ValidateAsync(parameters.RemoteValidationUrl, remoteParameters, cancellationToken))
            {
                _logger.LogError("Remote validation for device-attest-01:Apple failed.");
                return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "Remote validation failed."));
            }
        }
        else
        {
            _logger.LogDebug("No remote validation URL configured for device-attest-01, skipping remote validation.");
        }

        return ChallengeValidationResult.Valid();
    }


    private bool IsCertificateChainValid(List<X509Certificate2> certs, string[] base64RootCertificates)
    {
        if (base64RootCertificates.Length == 0)
        {
            _logger.LogError("ChallengeValidation parameters did not contain a root certificate for device-attest-01:Apple. Validation not possible.");
            return false;
        }

        X509Chain chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        chain.ChainPolicy.VerificationTime = DateTime.Now;
        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0);

        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        foreach (var base64RootCertificate in base64RootCertificates)
        {
            var rootCertBytes = Base64UrlEncoder.DecodeBytes(base64RootCertificate);
#if NET10_0_OR_GREATER
            var x509RootCert = X509CertificateLoader.LoadCertificate(rootCertBytes);
#else
            var x509RootCert = new X509Certificate2(rootCertBytes);
#endif

            chain.ChainPolicy.CustomTrustStore.Add(x509RootCert);
        }

        foreach (var intermediate in certs.Skip(1).Select(c => new X509Certificate2(c)))
        {
            chain.ChainPolicy.ExtraStore.Add(intermediate);
        }

        return chain.Build(certs[0]);
    }
}