namespace Th11s.ACMEServer.Model;

public static class IdentifierTypes
{
    // RFC 8555 https://www.rfc-editor.org/rfc/rfc8555#section-9.7.7
    public const string DNS = "dns";

    // RFC 8738 https://www.rfc-editor.org/rfc/rfc8738.html
    public const string IP = "ip";

    // RFC 8823 https://www.rfc-editor.org/rfc/rfc8823
    public const string Email = "email";

    // https://www.ietf.org/archive/id/draft-acme-device-attest-03.html
    public const string PermanentIdentifier = "permanent-identifier"; 
    public const string HardwareModule = "hardware-module";            
}
