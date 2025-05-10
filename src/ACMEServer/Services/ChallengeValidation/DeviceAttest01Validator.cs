using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Extensions;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

public sealed class DeviceAttest01ChallengeValidator(ILogger<DeviceAttest01ChallengeValidator> logger) : ChallengeValidator(logger)
{
    private class ChallengePayload
    {
        [JsonPropertyName("attObj")]
        public required string AttestationObject { get; set; }
    }

    public override string ChallengeType => ChallengeTypes.DeviceAttest01;
    public override IEnumerable<string> SupportedIdentiferTypes => [IdentifierTypes.PermanentIdentifier];

    protected override async Task<ChallengeValidationResult> ValidateChallengeInternalAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
    {
        if(challenge.Payload is null)
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The challenge payload was empty."));
        }


        return ValidateWebAuthNAttestation(challenge, account, cancellationToken);
    }

    private static ChallengeValidationResult ValidateWebAuthNAttestation(Challenge challenge, Account account, CancellationToken cancellationToken)
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
            "apple" => ValidateAppleAttestation(cborReader, challenge, account, cancellationToken),
            _ => ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, $"The attestation object format '{fmt}' is not supported.")),
        };
    }


    private static ChallengeValidationResult ValidateAppleAttestation(CborReader cborReader, Challenge challenge, Account account, CancellationToken cancellationToken)
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

        var certs = new List<byte[]>();
        while (cborReader.PeekState() != CborReaderState.EndArray)
        {
            certs.Add(cborReader.ReadByteString());
        }

        if (certs.Count == 0)
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The attestation object did not have any certificates in 'x5c' in 'attStmt'."));
        }

        //TODO: detect validity of the certificate chain
        var credCert = certs[0];
        using var x509CredCert = new X509Certificate2(credCert);

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

        // TODO: find the persistent-identifer in the certificate and validate it as well.

        return ChallengeValidationResult.Valid();
    }
}