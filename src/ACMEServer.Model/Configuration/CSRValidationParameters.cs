using System.Text.RegularExpressions;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class CSRValidationParameters
    {
        public CSRSANParameters SANValidationParameters { get; set; } = new ();
    }

    public class CSRSANParameters
    {
        public string? RemoteValidationUrl { get; set; }

        public StringValueSANParameters DnsName { get; set; } = new ();
        public StringValueSANParameters URI { get; set; } = new ();
        public StringValueSANParameters Rfc822Name { get; set; } = new ();
        public StringValueSANParameters RegisteredId { get; set; } = new ();

        public IPAddressSANParameters IPAddress { get; set; } = new ();

        public OtherNameSANParameters OtherName { get; set; } = new ();
    }

    public class IPAddressSANParameters
    {
        public string[] ValidNetworks { get; set; } = [];
    }


    public class OtherNameSANParameters
    {
        public PermanentIdentifierSANParameters PermanentIdentifier { get; set; } = new();
        public HardwareModuleNameSANParameters HardwareModuleName { get; set; } = new();
        public StringValueSANParameters PrincipalName { get; set; } = new();

        public string[] IgnoredTypes { get; set; } = [];
    }

    public class  StringValueSANParameters
    {
        public string? ValidationRegex { get; set; }

        public Regex? CreateRegex() =>
            ValidationRegex is not null 
                ? new Regex(ValidationRegex) 
                : null;
    }

    public class PermanentIdentifierSANParameters
    {
        public string? ValidValueRegex { get; set; }
        public Regex? CreateValidValueRegex() =>
            ValidValueRegex is not null 
                ? new Regex(ValidValueRegex) 
                : null;

        public string? ValidAssignerRegex { get; set; }
        public Regex? CreateValidAssignerRegex() =>
            ValidAssignerRegex is not null 
                ? new Regex(ValidAssignerRegex) 
                : null;
    }

    public class HardwareModuleNameSANParameters
    {
        public string? ValidTypeRegex { get; set; }
        public Regex? CreateValidTypeRegex() =>
            ValidTypeRegex is not null 
                ? new Regex(ValidTypeRegex) 
                : null;
    }
}