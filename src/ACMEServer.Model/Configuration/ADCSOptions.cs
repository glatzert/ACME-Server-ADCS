using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Th11s.ACMEServer.Model.Configuration;

public class ADCSOptions : IValidatableObject
{
    public required string CAServer { get; set; }
    public required string TemplateName { get; set; }

    [NotNull]
    public string[]? PublicKeyAlgorithms { get; set; }

    [NotNull]
    public int[]? KeySizes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(string.IsNullOrWhiteSpace(CAServer))
            yield return new ValidationResult($"ADCSOptions CAServer was empty.", [nameof(CAServer)]);

        if (string.IsNullOrWhiteSpace(TemplateName))
            yield return new ValidationResult($"ADCSOptions TemplateName was empty.", [nameof(TemplateName)]);
    }
}
