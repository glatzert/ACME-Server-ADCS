using System.Security.Cryptography.X509Certificates;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest;

internal static class X509Extensions
{
    internal static string[] GetCommonNames(this X500DistinguishedName subject)
    {
        return [.. subject.Name.Split(',', StringSplitOptions.TrimEntries) // split CN=abc,OU=def,XY=foo into parts
            .Select(x => x.Split('=', 2, StringSplitOptions.TrimEntries)) // split each part into [CN, abc], [OU, def], [XY, foo]
            .Where(x => string.Equals("cn", x.First(), StringComparison.OrdinalIgnoreCase)) // Check for cn
            .Select(x => x.Last()) // take abc
            ];
    }


    internal static AlternativeNames.GeneralName[] GetSubjectAlternativeNames(this IEnumerable<X509Extension> extensions)
    {
        return [.. extensions.OfType<X509SubjectAlternativeNameExtension>()
            .Select(x => new AlternativeNameEnumerator(x.RawData))
            .SelectMany(x => x.EnumerateAllNames())
            ];
    }
}

