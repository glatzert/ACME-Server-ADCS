using CERTENROLLLib;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.CertProvider.ADCS;

internal class AlternateNameValidator
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

        foreach (var subjectAlternativeName in validationContext.AlternativeNames)
        {
            var sanValue = subjectAlternativeName.strValue;

            var matchedIdentifiers = subjectAlternativeName.Type switch
            {
                AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME => validationContext.Identifiers
                    .Where(x => 
                        x.Type == IdentifierTypes.DNS && 
                        x.Value.Equals(sanValue, StringComparison.OrdinalIgnoreCase) // DNS names are considered to be case insensitive
                    )
                    .ToArray(),

                AlternativeNameType.XCN_CERT_ALT_NAME_IP_ADDRESS => validationContext.Identifiers
                    .Where(x => 
                        x.Type == IdentifierTypes.IP && 
                        x.Value.Equals(sanValue, StringComparison.Ordinal)) // IP addresses probably have no casing rules
                    .ToArray(),

                _ => []
            };


            if (matchedIdentifiers.Length == 0)
            {
                return false;
            }

            foreach (var identifier in matchedIdentifiers)
            {
                validationContext.SetIdentifierToValid(identifier);
            }
        }

        return true;
    }
}
