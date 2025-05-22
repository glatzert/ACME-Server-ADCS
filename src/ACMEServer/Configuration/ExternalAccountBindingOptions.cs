using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.Configuration
{
    public class ExternalAccountBindingOptions : IValidatableObject
    {
        public bool Required { get; set; }
        
        public required string MACRetrievalUrl { get; set; }

        public string? SuccessSignalUrl { get; set; }
        public string? FailedSignalUrl { get; set; }

        public List<ExternalAccountBindingHeader> Headers { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(MACRetrievalUrl))
            {
                yield return new ValidationResult("MACRetrievalUrl is required.", [nameof(MACRetrievalUrl)]);
            }
            else
            {
                if (!Uri.TryCreate(MACRetrievalUrl, UriKind.Absolute, out _))
                {
                    yield return new ValidationResult("MACRetrievalUrl must be a valid absolute URL.", [nameof(MACRetrievalUrl)]);
                }
            }

            if (SuccessSignalUrl != null && !Uri.TryCreate(SuccessSignalUrl, UriKind.Absolute, out _))
            {
                yield return new ValidationResult("SuccessSignalUrl must be a valid absolute URL or empty.", [nameof(SuccessSignalUrl)]);
            }

            if (FailedSignalUrl != null && !Uri.TryCreate(FailedSignalUrl, UriKind.Absolute, out _))
            {
                yield return new ValidationResult("FailedSignalUrl must be a valid absolute URL or empty.", [nameof(FailedSignalUrl)]);
            }
        }
    }

    public class ExternalAccountBindingHeader
    {
        public required string Key { get; set; }
        public required string Value { get; set; }
    }
}