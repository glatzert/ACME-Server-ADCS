using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class HardwareModuleValidationParameters : IValidatableObject
    {
        /// <summary>
        /// The regular expression that is used to validate the permanent identifier.
        /// https://datatracker.ietf.org/doc/draft-ietf-acme-device-attest/03/ Section 4.1 specifies either UTF-8 string without / OR Identifier/OID
        /// </summary>
        public string ValidationRegex { get; set; } = "[^/]+(/[012](\\.\\d+)+)?";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ValidationRegex))
            {
                yield return new ValidationResult("Validation regex cannot be null or empty.", [nameof(ValidationRegex)]);
            }
        }
    }
}
