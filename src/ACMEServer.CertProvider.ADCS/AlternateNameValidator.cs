﻿using CERTENROLLLib;

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
            return true;

        // We can not allow any other alternative names than DNS_Name currently
        if (validationContext.AlternativeNames.Any(x => x.Type != AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME))
            return false;

        foreach (var subjectAlternativeName in validationContext.AlternativeNames.Select(x => x.strValue))
        {
            var matchingIdentifiers = validationContext.Identifiers
                .Where(x => x.Value.Equals(subjectAlternativeName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingIdentifiers.Count == 0)
                return false;

            foreach (var identifier in matchingIdentifiers)
                validationContext.SetIdentifierToValid(identifier);
        }

        return true;
    }
}
