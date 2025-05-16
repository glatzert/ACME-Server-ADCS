namespace Th11s.ACMEServer.Model.Configuration
{
    public class ProfileConfiguration
    {
        public string Name { get; set; } = "";

        public TimeSpan AuthorizationValidityPeriod { get; set; } = TimeSpan.FromDays(1);

        public required string[] SupportedIdentifiers { get; set; } = [];

        public required ADCSOptions ADCSOptions { get; set; }

        public IdentifierValidationParameters IdentifierValidation { get; set; } = new ();

        public ChallengeValidationParameters ChallengeValidation { get; set; } = new();
    }
}
