using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.Configuration
{
    public class ACMEServerOptions : IValidatableObject
    {
        public BackgroundServiceOptions HostedWorkers { get; set; } = new ();

        public string? WebsiteUrl { get; set; }

        public TermsOfServiceOptions TOS { get; set; } = new ();

        public ExternalAccountBindingOptions? ExternalAccountBinding { get; set; }


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return ExternalAccountBinding?.Validate(validationContext) ?? [];
        }
    }
}
