using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.CertProvider.ADCS
{
    public class ADCSOptions : IValidatableObject
    {
        public required string CAServer { get; set; }
        public string? TemplateName { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(string.IsNullOrWhiteSpace(CAServer))
            {
                yield return new ValidationResult("CAServer is required.", [nameof(CAServer)]);
            }

            if (string.IsNullOrWhiteSpace(TemplateName))
            {
                yield return new ValidationResult("TemplateName is required.", [nameof(TemplateName)]);
            }
        }
    }
}
