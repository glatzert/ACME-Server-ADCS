using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class IdentifierValidationParameters : IValidatableObject
    {
        public DNSValidationParameters DNS { get; set; } = new();
        public IPValidationParameters IP { get; set; } = new();

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
        }
    }
}
