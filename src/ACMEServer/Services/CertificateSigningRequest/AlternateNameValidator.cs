using System.Formats.Asn1;
using System.Net;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services.CertificateSigningRequest.ASN1;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest;

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

        var identifierLookup = validationContext.Identifiers
            .ToLookup(x => x.Type);

        foreach (var subjectAlternativeName in validationContext.AlternativeNames)
        {
            Identifier[] matchedIdentifiers = [];

            if(subjectAlternativeName.DnsName is string dnsName)
            {
                matchedIdentifiers = identifierLookup[IdentifierTypes.DNS]
                    .Where(x => x.Value.Equals(dnsName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (subjectAlternativeName.IPAddress is ReadOnlyMemory<byte> ipAddress)
            {
                var sanIPAddress = new IPAddress(ipAddress.Span.ToArray());
                matchedIdentifiers = identifierLookup[IdentifierTypes.IP]
                    .Where(x => IPAddress.Parse(x.Value).Equals(sanIPAddress))
                    .ToArray();
            }

            if (subjectAlternativeName.OtherName is OtherNameAsn otherName)
            {
                if (otherName.TypeId == "1.3.6.1.5.5.7.8.3") //id-on-permanentIdentifier
                {
                    var asn1ValueReader = new AsnValueReader(otherName.Value.Span, AsnEncodingRules.DER);
                    var contentReader = asn1ValueReader.ReadSequence();
                    var content = contentReader.ReadCharacterString(UniversalTagNumber.UTF8String);

                    matchedIdentifiers = identifierLookup[IdentifierTypes.PermanentIdentifier]
                        .Where(x => x.Value == content)
                        .ToArray();
                }
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
