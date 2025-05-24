using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.Model.Configuration
{
    /// <summary>
    /// Parameters for the Device Attest 01 challenge type.
    /// </summary>
    public class DeviceAttest01Parameters : IValidatableObject
    {
        /// <summary>
        /// An URL that the acme-server will contact to do further validation of the device attest challenge.
        /// </summary>
        public string? RemoteValidationUrl { get; set; }

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
