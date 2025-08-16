using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.Asn1;

internal class AlternativeNameEnumerator(byte[] rawData, bool critical = false) 
    : X509Extension("2.5.29.17", rawData, critical)
{
    public IEnumerable<AlternativeNames.GeneralName> EnumerateAllNames()
    {
        List<AlternativeNames.GeneralName> results = [];

        try
        {
            var outer = new AsnValueReader(RawData, AsnEncodingRules.DER);
            var sequence = outer.ReadSequence();
            outer.ThrowIfNotEmpty();

            while (sequence.HasData)
            {
                var tag = sequence.PeekTag();
                var encodedData = sequence.ReadEncodedValue();

                if (tag.TagClass != TagClass.ContextSpecific)
                {
                    throw new CryptographicException("Cryptography_Der_Invalid_Encoding");
                }

                var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(tag, encodedData);
                results.Add(generalName);
            }
        }
        catch (AsnContentException e)
        {
            throw new CryptographicException("Cryptography_Der_Invalid_Encoding", e);
        }

        return results;
    }
}
