using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class DeviceAttest01Parameters : IValidatableObject
    {
        public AppleDeviceParameters Apple { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var validationResult in Apple?.Validate(validationContext) ?? [])
            {
                yield return validationResult;
            }
        }
    }
}
