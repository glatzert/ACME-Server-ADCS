using System.Formats.Asn1;
using Th11s.ACMEServer.Services.Asn1;


namespace Th11s.ACMEServer.Services.X509.AlternativeNames
{
    public class GeneralName
    {
        public ReadOnlyMemory<byte> EncodedData { get; }

        internal GeneralName(ReadOnlySpan<byte> encodedData)
        {
            EncodedData = encodedData.ToArray();
        }
    }

    public class OtherName : GeneralName
    {
        public string TypeId { get; }
        public ReadOnlyMemory<byte> EncodedValue { get; }


        internal OtherName(string typeId, ReadOnlySpan<byte> encodedValue, ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            TypeId = typeId;
            EncodedValue = encodedValue.ToArray();
        }
    }

    public class PermanentIdentifier : OtherName
    {
        public string? Value { get; }
        public string? Assigner { get; }

        internal PermanentIdentifier(string typeId, ReadOnlySpan<byte> encodedValue, ReadOnlySpan<byte> encodedData)
            : base(typeId, encodedValue, encodedData)
        {
            AsnValueReader piSequence = new AsnValueReader(encodedValue, AsnEncodingRules.DER).ReadSequence();

            // Read the value (which is an optional UTF8String)
            var asn1Tag = piSequence.PeekTag();
            if (asn1Tag.TagValue == (int)UniversalTagNumber.UTF8String)
            {
                Value = piSequence.ReadCharacterString(UniversalTagNumber.UTF8String);
            }

            // If the sequence still has data, read the assigner (which is an optional ObjectIdentifier)
            if (piSequence.HasData)
            {
                Assigner = piSequence.ReadObjectIdentifier();
            }
        }
    }

    public class HardwareModuleName : OtherName
    {
        internal HardwareModuleName(string typeId, ReadOnlySpan<byte> encodedValue, ReadOnlySpan<byte> encodedData)
            : base(typeId, encodedValue, encodedData)
        {
            AsnValueReader hwSequence = new AsnValueReader(encodedValue, AsnEncodingRules.DER).ReadSequence();

            HardwareType = hwSequence.ReadObjectIdentifier();
            SerialNumber = hwSequence.ReadOctetString();
        }

        public string HardwareType { get; }
        public ReadOnlyMemory<byte> SerialNumber { get; }
    }


    public class Rfc822Name : GeneralName
    {
        public string Value { get; }

        internal Rfc822Name(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            Value = new AsnValueReader(encodedData, AsnEncodingRules.DER)
                .ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 1));
        }
    }


    public class DnsName : GeneralName
    {
        public string Value { get; }

        internal DnsName(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            Value = new AsnValueReader(encodedData, AsnEncodingRules.DER)
                .ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 2));
        }
    }

    public class X400Address : GeneralName
    {
        internal X400Address(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        { }
    }

    public class DirectoryName : GeneralName
    {
        public ReadOnlyMemory<byte> Value { get; }
        internal DirectoryName(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            var innerReader = new AsnValueReader(encodedData, AsnEncodingRules.DER)
                .ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
            Value = innerReader.ReadEncodedValue().ToArray();
            innerReader.ThrowIfNotEmpty();
        }
    }

    public class EdiPartyName : GeneralName
    {
        internal EdiPartyName(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        { }
    }

    public class Uri : GeneralName
    {
        public string Value { get; }

        internal Uri(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            Value = new AsnValueReader(encodedData, AsnEncodingRules.DER)
                .ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 6));
        }
    }

    public class IPAddress : GeneralName
    {
        public System.Net.IPAddress Value { get; }
        internal IPAddress(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            var reader = new AsnValueReader(encodedData, AsnEncodingRules.DER);
            if (reader.TryReadPrimitiveOctetString(out var primitiveValue, new Asn1Tag(TagClass.ContextSpecific, 7)))
            {
                Value = new System.Net.IPAddress(primitiveValue);
            }
            else
            {
                var value = reader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 7));
                Value = new System.Net.IPAddress(value);
            }
        }
    }

    public class RegisteredId : GeneralName
    {
        public string Value { get; }

        internal RegisteredId(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            Value = new AsnValueReader(encodedData, AsnEncodingRules.DER)
                .ReadObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 8));
        }
    }


}