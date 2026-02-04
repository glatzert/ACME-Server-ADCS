using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class ProfileConfiguration : IValidatableObject
    {
        public string Name { get; set; } = "";
        public ProfileName ProfileName => new(Name);

        [NotNull]
        public string[] SupportedIdentifiers { get; set; } = default!;

        [NotNull]
        public Dictionary<string, string[]> AllowedChallengeTypes { get; set; } = default!;

        public TimeSpan AuthorizationValidityPeriod { get; set; } = TimeSpan.FromDays(1);

        public bool RequireExternalAccountBinding { get; set; } = false;

        [NotNull]
        public ADCSOptions? ADCSOptions { get; set; }


        public IdentifierValidationParameters IdentifierValidation { get; set; } = new ();

        public ChallengeValidationParameters ChallengeValidation { get; set; } = new();

        public CSRValidationParameters? CSRValidation { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Profile name was empty, do not use unnamed profiles.", [nameof(Name)]);

            if (SupportedIdentifiers is not { Length: > 0})
                yield return new ValidationResult("Profile must support at least one identifier type.", [nameof(SupportedIdentifiers)]);

            if (ADCSOptions is null)
                yield return new ValidationResult("ADCS options were not set.", [nameof(ADCSOptions)]);

            foreach(var result in ADCSOptions?.Validate(validationContext) ?? [])
            {
                yield return result;
            }

            foreach(var result in IdentifierValidation?.Validate(validationContext) ?? [])
            {
                yield return result;
            }

            foreach (var result in ChallengeValidation?.Validate(validationContext) ?? [])
            {
                yield return result;
            }
        }
    }
}
