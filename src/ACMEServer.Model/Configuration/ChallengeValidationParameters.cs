using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class ChallengeValidationParameters : IValidatableObject
    {
        public DeviceAttest01Parameters DeviceAttest01 { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var validationResult in DeviceAttest01?.Validate(validationContext) ?? [])
            {
                yield return validationResult;
            }
        }
    }
}
