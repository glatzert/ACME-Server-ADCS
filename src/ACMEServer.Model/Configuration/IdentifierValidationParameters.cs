using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class IdentifierValidationParameters : IValidatableObject
    {
        public DNSValidationParameters DNS { get; set; } = new();
        public IPValidationParameters IP { get; set; } = new();
        public PermanentIdentifierValidationParameters PermanentIdentifier { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var result in DNS?.Validate(validationContext) ?? [])
            {
                yield return result;
            }

            foreach (var result in IP?.Validate(validationContext) ?? [])
            {
                yield return result;
            }

            foreach (var result in PermanentIdentifier?.Validate(validationContext) ?? [])
            {
                yield return result;
            }
        }
    }

    public class PermanentIdentifierValidationParameters : IValidatableObject
    {
        /// <summary>
        /// The regular expression that is used to validate the permanent identifier.
        /// </summary>
        public string ValidationRegex { get; set; } = ".*";
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ValidationRegex))
            {
                yield return new ValidationResult("Validation regex cannot be null or empty.", [nameof(ValidationRegex)]);
            }
        }
    }
}
