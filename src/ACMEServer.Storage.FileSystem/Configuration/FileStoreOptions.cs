using System.ComponentModel.DataAnnotations;

namespace ACMEServer.Storage.FileSystem.Configuration;

public class FileStoreOptions : IValidatableObject
{
    public string BasePath { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(BasePath))
            yield return new ValidationResult($"FileStore BasePath ({BasePath}) was empty.", [nameof(BasePath)]);
        
        if (!Directory.Exists(BasePath))
            yield return new ValidationResult($"FileStore BasePath ({BasePath}) did not exist.", [nameof(BasePath)]);
    }
}

public static class FileStoreOptionsExtension
{
    extension(FileStoreOptions options)
    {
        public string NonceDirectory => Path.Combine(options.BasePath, "Nonces");
        public string AccountDirectory => Path.Combine(options.BasePath, "Accounts");
        public string OrderDirectory => Path.Combine(options.BasePath, "Orders");
        public string CertificateDirectory => Path.Combine(options.BasePath, "Certificates");
        public string WorkingDirectory => Path.Combine(options.BasePath, "_work");
    }
}