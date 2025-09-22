using System.ComponentModel.DataAnnotations;

namespace ACMEServer.Storage.FileSystem.Configuration;

public class FileStoreOptions : IValidatableObject
{
    public string BasePath { get; set; } = null!;

    public string NonceDirectory => Path.Combine(BasePath, "Nonces");
    public string AccountDirectory => Path.Combine(BasePath, "Accounts");
    public string OrderDirectory => Path.Combine(BasePath, "Orders");
    public string WorkingDirectory => Path.Combine(BasePath, "_work");

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(BasePath))
            yield return new ValidationResult($"FileStore BasePath ({BasePath}) was empty.", [nameof(BasePath)]);
        
        if (!Directory.Exists(BasePath))
            yield return new ValidationResult($"FileStore BasePath ({BasePath}) did not exist.", [nameof(BasePath)]);
    }
}
