namespace Th11s.ACMEServer.Model.Configuration
{
    public class ProfileConfiguration
    {
        public string Name { get; set; } = "";


        public required string[] SupportedIdentifiers { get; set; } = [];


        public TimeSpan AuthorizationValidityPeriod { get; set; } = TimeSpan.FromDays(1);

        public bool RequireExternalAccountBinding { get; set; } = false;


        public required ADCSOptions ADCSOptions { get; set; }


        public IdentifierValidationParameters IdentifierValidation { get; set; } = new ();

        public ChallengeValidationParameters ChallengeValidation { get; set; } = new();
    }
}
