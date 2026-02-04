namespace Th11s.ACMEServer.Model;

public static class ChallengeTypes
{
    public const string Http01 = "http-01";
    public const string Dns01 = "dns-01";
    public const string TlsAlpn01 = "tls-alpn-01";
    public const string DeviceAttest01 = "device-attest-01";
    public const string DnsPersist01 = "dns-persist-01";

    public static readonly string[] AllTypes = [Http01, Dns01, TlsAlpn01, DeviceAttest01, DnsPersist01];
    public static readonly string[] TokenChallenges = [Http01, Dns01, TlsAlpn01, DeviceAttest01];

    public static readonly string[] DnsChallenges = [Http01, Dns01, DnsPersist01, TlsAlpn01];
    public static readonly string[] DnsWildcardChallenges = [Dns01, DnsPersist01];
    public static readonly string[] IpChallenges = [Http01, TlsAlpn01];
    public static readonly string[] EmailChallenges = [];
    public static readonly string[] PermanentIdentifierChallenges = [DeviceAttest01];
    public static readonly string[] HardwareModuleChallenges = [DeviceAttest01];
}
