using Microsoft.Extensions.Logging;
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

        // Deserialize the outer challenge payload
        var challengePayload = challenge.Payload.DeserializeBase64UrlEncodedJson<ChallengePayload>();
        if(challengePayload?.AttestationObject is null)
        {
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "The attestation object was empty."));
        }

        throw new NotImplementedException();
    }
}