using System.Net;
using Th11s.ACMEServer.Model;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest;

internal class AlternativeNameValidator
{
    /// <summary>
    /// All SANs must have a matching identifier in the order. If not, the order is invalid.
    /// This method returns false, if any SAN does not have a matching identifier.
    /// </summary>
    internal bool AreAllAlternateNamesValid(CSRValidationContext validationContext)
    {
        // No alternative names might be useless, but is valid.
        if (validationContext.AlternativeNames == null)
        {
            return true;
        }

        var identifierLookup = validationContext.Identifiers
            .ToLookup(x => x.Type);

        foreach (var subjectAlternativeName in validationContext.AlternativeNames)
        {
            Identifier[] matchedIdentifiers = [];

            if(subjectAlternativeName is AlternativeNames.DnsName dnsName)
            {
                matchedIdentifiers = identifierLookup[IdentifierTypes.DNS]
                    .Where(x => x.Value.Equals(dnsName.Value, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (subjectAlternativeName is AlternativeNames.IPAddress ipAddress)
            {
                matchedIdentifiers = identifierLookup[IdentifierTypes.IP]
                    .Where(x => IPAddress.Parse(x.Value).Equals(ipAddress.Value))
                    .ToArray();
            }

            if (subjectAlternativeName is AlternativeNames.PermanentIdentifier pe)
            {
                matchedIdentifiers = identifierLookup[IdentifierTypes.PermanentIdentifier]
                    .Where(x => x.Value == pe.Value)
                    .ToArray();
            }

            if (matchedIdentifiers?.Length > 0)
            {
                for (int i = 0; i < matchedIdentifiers.Length; i++)
                {
                    validationContext.SetIdentifierIsUsed(matchedIdentifiers[i]);
                }

                validationContext.SetAlternateNameValid(subjectAlternativeName);
            }
        }

        // TODO: validate additional values here

        return validationContext.AreAllAlternativeNamesValidated();
    }
}
