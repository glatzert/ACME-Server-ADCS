namespace Th11s.ACMEServer.Model.Configuration
{
    public class IdentifierValidationParameters
    {
        public DNSValidationParameters DNS { get; set; } = new();
    }
}
