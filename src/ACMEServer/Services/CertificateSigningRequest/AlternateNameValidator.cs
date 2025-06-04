using CERTENROLLLib;
using System.Formats.Asn1;
using System.Net;
using Th11s.ACMEServer.Model;

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

        foreach (var subjectAlternativeName in validationContext.AlternativeNames)
        {
            // https://github.com/dotnet/runtime/blob/571861b01eabf7bc86b21c03e0e13b0e21dc5a54/src/libraries/Common/src/System/Security/Cryptography/Oids.cs#L192
            // https://github.com/dotnet/runtime/blob/571861b01eabf7bc86b21c03e0e13b0e21dc5a54/src/libraries/Common/src/System/Security/Cryptography/Asn1/GeneralNameAsn.xml.cs
            var matchedIdentifiers = subjectAlternativeName.OID switch
            {
                AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME => GetMatchingDNSIdentifiers(validationContext, subjectAlternativeName),
                AlternativeNameType.XCN_CERT_ALT_NAME_IP_ADDRESS => GetMatchingIPIdentifiers(validationContext, subjectAlternativeName),
                AlternativeNameType.XCN_CERT_ALT_NAME_OTHER_NAME => GetMatchingIdentifiers(validationContext, subjectAlternativeName),

                _ => []
            };

            if (matchedIdentifiers.Length == 0)
            {
                return false;
            }

            foreach (var identifier in matchedIdentifiers)
            {
                validationContext.SetIdentifierIsUsed(identifier);
            }
        }

        return true;
    }

    private static Identifier[] GetMatchingDNSIdentifiers(CSRValidationContext validationContext, CAlternativeName subjectAlternateName)
    {
        var sanValue = subjectAlternateName.strValue;

        return validationContext.Identifiers
            .Where(x =>
                x.Type == IdentifierTypes.DNS &&
                x.Value.Equals(sanValue, StringComparison.OrdinalIgnoreCase) // DNS names are considered to be case insensitive
            )
            .ToArray();
    }

    private static Identifier[] GetMatchingIPIdentifiers(CSRValidationContext validationContext, CAlternativeName subjectAlternateName)
    {
        var sanBase64Value = subjectAlternateName.RawData[EncodingType.XCN_CRYPT_STRING_BASE64];
        var sanBytes = Convert.FromBase64String(sanBase64Value.Trim());

        var sanIPAddress = new IPAddress(sanBytes);

        return validationContext.Identifiers
            .Where(x =>
                x.Type == IdentifierTypes.IP &&
                IPAddress.Parse(x.Value).Equals(sanIPAddress)
            )
            .ToArray();
    }
    
    
    private static Identifier[] GetMatchingIdentifiers(CSRValidationContext validationContext, CAlternativeName subjectAlternateName)
    {
        return subjectAlternateName.ObjectId.Value switch
        {
            "1.3.6.1.5.5.7.8.3" => GetMatchingPermanentIdIdentifiers(validationContext, subjectAlternateName),

            _ => []
        };
    }

    private static Identifier[] GetMatchingPermanentIdIdentifiers(CSRValidationContext validationContext, CAlternativeName subjectAlternateName)
    {
        var sanBase64Value = subjectAlternateName.RawData[EncodingType.XCN_CRYPT_STRING_BASE64];
        var sanBytes = Convert.FromBase64String(sanBase64Value.Trim());

        AsnDecoder.ReadSequence(sanBytes, AsnEncodingRules.DER, out var contentOffset, out var contentLength, out var bytesConsumed);
        var content = AsnDecoder.ReadCharacterString(sanBytes[contentOffset..], AsnEncodingRules.DER, UniversalTagNumber.UTF8String, out var _);

        return validationContext.Identifiers
            .Where(x =>
                x.Type == IdentifierTypes.PermanentIdentifier &&
                x.Value == content
            )
            .ToArray();
    }
}
