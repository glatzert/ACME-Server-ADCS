using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.Configuration
{
    public class DefaultProfileConfiguration : IPostConfigureOptions<ProfileConfiguration>
    {
        public void PostConfigure(string? name, ProfileConfiguration options)
        {
            // Set all default challenge types, if none are configured
            options.AllowedChallengeTypes ??= [];
            if (!options.AllowedChallengeTypes.ContainsKey(IdentifierTypes.DNS))
            {
                options.AllowedChallengeTypes[IdentifierTypes.DNS] = [..ChallengeTypes.DefaultDNSChallenges];
            }
            if (!options.AllowedChallengeTypes.ContainsKey(IdentifierTypes.IP))
            {
                options.AllowedChallengeTypes[IdentifierTypes.IP] = [..ChallengeTypes.DefaultIPChallenges];
            }
            if (!options.AllowedChallengeTypes.ContainsKey(IdentifierTypes.Email))
            {
                options.AllowedChallengeTypes[IdentifierTypes.Email] = [..ChallengeTypes.DefaultEmailChallenges];
            }
            if (!options.AllowedChallengeTypes.ContainsKey(IdentifierTypes.PermanentIdentifier))
            {
                options.AllowedChallengeTypes[IdentifierTypes.PermanentIdentifier] = [.. ChallengeTypes.DefaultPermanentIdentifierChallenges];
            }
            if (!options.AllowedChallengeTypes.ContainsKey(IdentifierTypes.HardwareModule))
            {
                options.AllowedChallengeTypes[IdentifierTypes.HardwareModule] = [.. ChallengeTypes.DefaultHardwareModuleChallenges];
            }

            // Ensure only possible challenge types are configured for each identifier type
            options.AllowedChallengeTypes[IdentifierTypes.IP].IntersectWith(ChallengeTypes.IpChallenges);
            options.AllowedChallengeTypes[IdentifierTypes.Email].IntersectWith(ChallengeTypes.EmailChallenges);
            options.AllowedChallengeTypes[IdentifierTypes.PermanentIdentifier].IntersectWith(ChallengeTypes.PermanentIdentifierChallenges);
            options.AllowedChallengeTypes[IdentifierTypes.HardwareModule].IntersectWith(ChallengeTypes.HardwareModuleChallenges);


            options.IdentifierValidation.DNS.AllowedDNSNames ??= [""];
            options.IdentifierValidation.IP.AllowedIPNetworks ??= ["::0/0", "0.0.0.0/0"];

            options.ChallengeValidation.DeviceAttest01.Apple.RootCertificates ??= ["MIICJDCCAamgAwIBAgIUQsDCuyxyfFxeq/bxpm8frF15hzcwCgYIKoZIzj0EAwMwUTEtMCsGA1UEAwwkQXBwbGUgRW50ZXJwcmlzZSBBdHRlc3RhdGlvbiBSb290IENBMRMwEQYDVQQKDApBcHBsZSBJbmMuMQswCQYDVQQGEwJVUzAeFw0yMjAyMTYxOTAxMjRaFw00NzAyMjAwMDAwMDBaMFExLTArBgNVBAMMJEFwcGxlIEVudGVycHJpc2UgQXR0ZXN0YXRpb24gUm9vdCBDQTETMBEGA1UECgwKQXBwbGUgSW5jLjELMAkGA1UEBhMCVVMwdjAQBgcqhkjOPQIBBgUrgQQAIgNiAAT6Jigq+Ps9Q4CoT8t8q+UnOe2poT9nRaUfGhBTbgvqSGXPjVkbYlIWYO+1zPk2Sz9hQ5ozzmLrPmTBgEWRcHjA2/y77GEicps9wn2tj+G89l3INNDKETdxSPPIZpPj8VmjQjBAMA8GA1UdEwEB/wQFMAMBAf8wHQYDVR0OBBYEFPNqTQGd8muBpV5du+UIbVbi+d66MA4GA1UdDwEB/wQEAwIBBjAKBggqhkjOPQQDAwNpADBmAjEA1xpWmTLSpr1VH4f8Ypk8f3jMUKYz4QPG8mL58m9sX/b2+eXpTv2pH4RZgJjucnbcAjEA4ZSB6S45FlPuS/u4pTnzoz632rA+xW/TZwFEh9bhKjJ+5VQ9/Do1os0u3LEkgN/r"];
        }
    }
}
