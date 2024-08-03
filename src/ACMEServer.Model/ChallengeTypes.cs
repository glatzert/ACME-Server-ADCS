namespace Th11s.ACMEServer.Model
{
    public static class ChallengeTypes
    {
        public const string Http01 = "http-01";
        public const string Dns01 = "dns-01";
        public const string TlsAlpn01 = "tls-alpn-01";

        public static readonly string[] AllTypes = [Http01, Dns01, TlsAlpn01];
    }
}
