namespace Th11s.ACMEServer.Model.Configuration
{
    public class CSRValidationParameters
    {
        public CSRSANParameters? SANValidationParameters { get; set; }
    }

    public class CSRSANParameters
    {
        public string? RemoteValidationUrl { get; set; }

        public StringValueSANParameters? DnsName { get; set; }
        public StringValueSANParameters? URI { get; set; }
        public StringValueSANParameters? Rfc822Name { get; set; }
        public StringValueSANParameters? RegisteredId { get; set; }

        public IPAddressSANParameters? IPAddress { get; set; }

        public OtherNameSANParameters? OtherName { get; set; }
    }

    public class IPAddressSANParameters
    {
        public string[]? ValidNetworks { get; set; }
    }


    public class OtherNameSANParameters
    {
        public PermanentIdentifierSANParameters? PermanentIdentifier { get; set; }
        public HardwareModuleNameSANParameters? HardwareModuleName { get; set; }
        public StringValueSANParameters? PrincipalName { get; set; }

        public string[]? IgnoredTypes { get; set; }
    }

    public class StringValueSANParameters
    {
        public string? ValidationRegex { get; set; }
    }

    public class PermanentIdentifierSANParameters
    {
        public string? ValidValueRegex { get; set; }
        public string? ValidAssignerRegex { get; set; }
    }

    public class HardwareModuleNameSANParameters
    {
        public string? ValidTypeRegex { get; set; }
    }
}