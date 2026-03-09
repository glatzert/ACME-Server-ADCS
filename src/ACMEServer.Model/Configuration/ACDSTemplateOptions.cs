using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.Model.Configuration;

public class ADCSTemplateOptions : IValidatableObject
{
    public required string TemplateName { get; set; }
    
    public required string[] PublicKeyAlgorithms { get; set; }

    public int[]? KeySizes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
            yield return new ValidationResult($"ADCSOptions TemplateName was empty.", [nameof(TemplateName)]);

        if (PublicKeyAlgorithms is null || PublicKeyAlgorithms.Length == 0)
            yield return new ValidationResult($"ADCSOptions PublicKeyAlgorithms was empty.", [nameof(PublicKeyAlgorithms)]);
    }
}
