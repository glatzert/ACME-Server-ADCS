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
        public HashSet<string> SupportedIdentifiers { get; set; } = [];

        [NotNull]
        public Dictionary<string, HashSet<string>> AllowedChallengeTypes { get; set; } = [];

        public TimeSpan AuthorizationValidityPeriod { get; set; } = TimeSpan.FromDays(1);

        public bool RequireExternalAccountBinding { get; set; } = false;

        [Obsolete("Use CertificateServices instead. This property will be removed in a future version.")]
        public ADCSOptions? ADCSOptions { get; set; }
        public ADCSOptions[]? CertificateServices { get; set; }

        public ADCSOptions[] GetCertificateServices()
        {
            if (CertificateServices is not null && CertificateServices.Length > 0)
            {
                return CertificateServices;
            }

            if (ADCSOptions is not null)
            {
                return [ADCSOptions];
            }

            return [];
        }

        public IdentifierValidationParameters IdentifierValidation { get; set; } = new ();

        public ChallengeValidationParameters ChallengeValidation { get; set; } = new();

        public CSRValidationParameters? CSRValidation { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Profile name was empty, do not use unnamed profiles.", [nameof(Name)]);

            if (SupportedIdentifiers is not { Count: > 0})
                yield return new ValidationResult("Profile must support at least one identifier type.", [nameof(SupportedIdentifiers)]);

            if (CertificateServices is null)
                yield return new ValidationResult("ADCS options were not set.", [nameof(CertificateServices)]);

            if (CertificateServices is not null)
            {
                foreach (var result in CertificateServices.SelectMany(option => option.Validate(validationContext)))
                {
                    yield return result;
                }
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
