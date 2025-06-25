using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Services.Asn1;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest
{
    internal class AlternativeNameEnumerator :X509Extension
    {
        public AlternativeNameEnumerator(byte[] rawData, bool critical = false)
            : base("2.5.29.17", rawData, critical)
        {
        }

        public IEnumerable<AlternativeNames.GeneralName> EnumerateAllNames()
        {
            List<AlternativeNames.GeneralName> results = new();

            try
            {
                AsnValueReader outer = new AsnValueReader(RawData, AsnEncodingRules.DER);
                AsnValueReader sequence = outer.ReadSequence();
                outer.ThrowIfNotEmpty();

                while (sequence.HasData)
                {
                    Asn1Tag tag = sequence.PeekTag();
                    ReadOnlySpan<byte> encodedData = sequence.ReadEncodedValue();

                    if (tag.TagClass != TagClass.ContextSpecific)
                    {
                        throw new CryptographicException("Cryptography_Der_Invalid_Encoding");
                    }

                    AlternativeNames.GeneralName current = tag.TagValue switch
                    {
                        0 => ProcessOtherName(encodedData),
                        1 => new AlternativeNames.Rfc822Name(encodedData),
                        2 => new AlternativeNames.DnsName(encodedData),
                        3 => new AlternativeNames.X400Address(encodedData),
                        4 => new AlternativeNames.DirectoryName(encodedData),
                        5 => new AlternativeNames.EdiPartyName(encodedData),
                        6 => new AlternativeNames.Uri(encodedData),
                        7 => new AlternativeNames.IPAddress(encodedData),
                        8 => new AlternativeNames.RegisteredId(encodedData),
                        _ => throw new CryptographicException("Cryptography_Der_Invalid_Encoding")
                    };

                    results.Add(current);
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException("Cryptography_Der_Invalid_Encoding", e);
            }

            return results;
        }


        private static AlternativeNames.OtherName ProcessOtherName(ReadOnlySpan<byte> encodedData)
        {
            AsnValueReader sequenceReader = new AsnValueReader(encodedData, AsnEncodingRules.DER)
                .ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

            string typeId = sequenceReader.ReadObjectIdentifier();
            ReadOnlySpan<byte> encodedValue = sequenceReader.PeekEncodedValue();

            return typeId switch
            {
                //id-on-permanentIdentifier
                "1.3.6.1.5.5.7.8.3" => new AlternativeNames.PermanentIdentifier(typeId, encodedValue, encodedData),
                //id-on-hardwareModuleName
                "1.2.3.1.5.5.7.8.4" => new AlternativeNames.HardwareModuleName(typeId, encodedValue, encodedData),
                _ => new AlternativeNames.OtherName(typeId, encodedValue, encodedData)
            };
        }
    }
}
