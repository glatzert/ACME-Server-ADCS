using System.Formats.Asn1;
using Th11s.ACMEServer.Services.Asn1;


namespace Th11s.ACMEServer.Services.X509.AlternativeNames
{
    public interface IStringBasedName
    {
        string GetStringRepresentation();
    }

    public class GeneralName
    {
        public ReadOnlyMemory<byte> EncodedData { get; }

        internal GeneralName(ReadOnlySpan<byte> encodedData)
        {
            EncodedData = encodedData.ToArray();
        }

        public override string ToString()
            => $"[{this.GetType().Name}]: {Convert.ToHexString(EncodedData.Span)}";
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

        override public string ToString()
            => $"[OtherName]: TypeId={TypeId}, Value={Convert.ToHexString(EncodedValue.Span)}";
    }

    public class PermanentIdentifier : OtherName, IStringBasedName
    {
        public string? Value { get; }
        public string? Assigner { get; }

        internal PermanentIdentifier(string typeId, ReadOnlySpan<byte> encodedValue, ReadOnlySpan<byte> encodedData)
            : base(typeId, encodedValue, encodedData)
        {
            AsnValueReader piSequence = new AsnValueReader(encodedValue, AsnEncodingRules.DER).ReadSequence();

            // Read the value ()
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

            if (Value is null && Assigner is null)
            {
                throw new ArgumentException("PermanentIdentifier must have at least a value or an assigner.");
            }
        }

        public override string ToString()
            => $"[OtherName/PermanentIdentifier]: TypeId={TypeId}, Value={Value}, Assigner={Assigner}";

        /// <summary>
        /// We're gonna expect the assigner to be null in most cases, 
        /// so we return the value or value@assigner, 
        /// knowing the latter will probably not work in permantent-identifier scenarios.
        /// </summary>
        public string GetStringRepresentation()
        {
            if (Value is not null && Assigner is not null)
            {
                return $"{Value}@{Assigner}";
            }
            else if (Value is not null)
            {
                return Value;
            }
            else if (Assigner is not null)
            {
                return Assigner;
            }
            else
            {
                throw new InvalidOperationException("PermanentIdentifier has no value or assigner.");
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

        public override string ToString()
            => $"[OtherName/HardwareModuleName]: TypeId={TypeId}, HardwareType={HardwareType}, SerialNumber={Convert.ToHexString(SerialNumber.Span)}";
    }

    public class PrincipalName : OtherName, IStringBasedName
    {
        public string Value { get; }

        internal PrincipalName(string typeId, ReadOnlySpan<byte> encodedValue, ReadOnlySpan<byte> encodedData)
            : base(typeId, encodedValue, encodedData)
        {
            var upnSequence = new AsnValueReader(encodedValue, AsnEncodingRules.DER)
                .ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

            Value = upnSequence.ReadCharacterString(UniversalTagNumber.UTF8String);
        }

        public string GetStringRepresentation() => Value;

        public override string ToString()
            => $"[OtherName/PrincipalName]: TypeId={TypeId}, Value={Value}";
    }


    public class Rfc822Name : GeneralName, IStringBasedName
    {
        public string Value { get; }

        internal Rfc822Name(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            Value = new AsnValueReader(encodedData, AsnEncodingRules.DER)
                .ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 1));
        }

        public string GetStringRepresentation() => Value;

        public override string ToString()
            => $"[Rfc822Name]: {Value}";
    }


    public class DnsName : GeneralName, IStringBasedName
    {
        public string Value { get; }

        internal DnsName(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            Value = new AsnValueReader(encodedData, AsnEncodingRules.DER)
                .ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 2));
        }

        public string GetStringRepresentation() => Value;

        public override string ToString()
            => $"[DnsName]: {Value}";
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

    public class Uri : GeneralName, IStringBasedName
    {
        public string Value { get; }

        internal Uri(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            Value = new AsnValueReader(encodedData, AsnEncodingRules.DER)
                .ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 6));
        }

        public string GetStringRepresentation() => Value;

        public override string ToString()
            => $"[Uri]: {Value}";
    }

    public class IPAddress : GeneralName, IStringBasedName
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

        public string GetStringRepresentation() => Value.ToString();

        public override string ToString()
            => $"[IPAddress]: {Value}";
    }

    public class RegisteredId : GeneralName, IStringBasedName
    {
        public string Value { get; }

        internal RegisteredId(ReadOnlySpan<byte> encodedData)
            : base(encodedData)
        {
            Value = new AsnValueReader(encodedData, AsnEncodingRules.DER)
                .ReadObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 8));
        }

        public string GetStringRepresentation() => Value;

        public override string ToString()
            => $"[RegisteredId]: {Value}";
    }
}