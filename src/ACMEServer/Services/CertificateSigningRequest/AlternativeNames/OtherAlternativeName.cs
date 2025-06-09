using System.Formats.Asn1;
using Th11s.ACMEServer.Services.CertificateSigningRequest.ASN1;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest.AlternativeNames
{
    internal class OtherAlternativeName : AlternativeName
    {
        public required string Oid { get; init; }
        public required byte[] RawValue { get; init; }

        public static OtherAlternativeName Create(byte[] rawData, string oid, byte[] rawValue)
        {
            if (oid == "1.3.6.1.5.5.7.8.3")
            {
                AsnValueReader outer = new AsnValueReader(rawValue, AsnEncodingRules.DER);
                AsnValueReader sequence = outer.ReadSequence();

                string? value = null;
                string? assigner = null;

                // Read the value (which is an optional UTF8String)
                var asn1Tag = sequence.PeekTag();
                if (asn1Tag.TagValue == (int)UniversalTagNumber.UTF8String)
                {
                    value = sequence.ReadCharacterString(UniversalTagNumber.UTF8String);
                }

                // If the sequence still has data, read the assigner (which is an optional ObjectIdentifier)
                if (sequence.HasData)
                {
                    assigner = sequence.ReadObjectIdentifier();
                }

                return new OtherAlternativeNames.PermanentIdentifier
                {
                    RawData = rawData,

                    Oid = oid,
                    RawValue = rawValue,

                    Value = value,
                    Assigner = assigner,
                };
            }

            return new OtherAlternativeName
            {
                RawData = rawData,

                Oid = oid,
                RawValue = rawValue,
            };
        }
    }
}
