namespace TGIT.ACME.Protocol.Model
{
    public static class ChallengeTypes
    {
        public const string Http01 = "http-01";
        public const string Dns01 = "dns-01";

        public static readonly string[] AllTypes = new[] { Http01, Dns01 };
    }
}
