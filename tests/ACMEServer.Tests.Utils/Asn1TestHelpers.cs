using System.Formats.Asn1;
using System.Net;

namespace Th11s.ACMEServer.Tests.Utils;

internal static class Asn1TestHelpers
{
    public static byte[] CreateRfc822Name(string rfc822Name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rfc822Name, nameof(rfc822Name));

        var asn1Writer = new AsnWriter(AsnEncodingRules.DER);
        asn1Writer.WriteCharacterString(
            UniversalTagNumber.IA5String,
            rfc822Name,
            new Asn1Tag(TagClass.ContextSpecific, 1));

        return asn1Writer.Encode();
    }

    public static byte[] CreateDnsName(string dnsName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dnsName, nameof(dnsName));

        var asn1Writer = new AsnWriter(AsnEncodingRules.DER);
        asn1Writer.WriteCharacterString(
            UniversalTagNumber.IA5String,
            dnsName,
            new Asn1Tag(TagClass.ContextSpecific, 2));

        return asn1Writer.Encode();
    }

    public static byte[] CreateUri(string uri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri, nameof(uri));

        var asn1Writer = new AsnWriter(AsnEncodingRules.DER);
        asn1Writer.WriteCharacterString(
            UniversalTagNumber.IA5String,
            uri,
            new Asn1Tag(TagClass.ContextSpecific, 6));

        return asn1Writer.Encode();
    }

    public static byte[] CreateIpAddress(IPAddress ipAddress)
    {
        ArgumentNullException.ThrowIfNull(ipAddress, nameof(ipAddress));

        var asn1Writer = new AsnWriter(AsnEncodingRules.DER);
        asn1Writer.WriteOctetString(
            ipAddress.GetAddressBytes(),
            new Asn1Tag(TagClass.ContextSpecific, 7));

        return asn1Writer.Encode();
    }

    public static byte[] CreateRegisteredId(string registeredId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registeredId, nameof(registeredId));

        var asn1Writer = new AsnWriter(AsnEncodingRules.DER);
        asn1Writer.WriteObjectIdentifier(
            registeredId,
            new Asn1Tag(TagClass.ContextSpecific, 8));

        return asn1Writer.Encode();
    }



    public static byte[] CreateOtherName(string typeid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeid, nameof(typeid));

        var asn1Writer = new AsnWriter(AsnEncodingRules.DER);
        asn1Writer.PushSequence(
            new Asn1Tag(TagClass.ContextSpecific, 0));
            asn1Writer.WriteObjectIdentifier(typeid);
            asn1Writer.PushSequence(
                new Asn1Tag(TagClass.ContextSpecific, 0));
                
            asn1Writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
        asn1Writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

        return asn1Writer.Encode();
    }

    public static byte[] CreatePrincipalName(string principalName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principalName, nameof(principalName));

        var asn1Writer = new AsnWriter(AsnEncodingRules.DER);
        asn1Writer.PushSequence(
            new Asn1Tag(TagClass.ContextSpecific, 0));

            asn1Writer.WriteObjectIdentifier("1.3.6.1.4.1.311.20.2.3");
            asn1Writer.PushSequence(
                new Asn1Tag(TagClass.ContextSpecific, 0));

                asn1Writer.WriteCharacterString(
                    UniversalTagNumber.UTF8String,
                    principalName);

            asn1Writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
        asn1Writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

        return asn1Writer.Encode();
    }

    public static byte[] CreateHardwareModuleName(string hardwareType, ReadOnlySpan<byte> serialNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hardwareType, nameof(hardwareType));

        var asn1Writer = new AsnWriter(AsnEncodingRules.DER);
        asn1Writer.PushSequence(
            new Asn1Tag(TagClass.ContextSpecific, 0));

            asn1Writer.WriteObjectIdentifier("1.2.3.1.5.5.7.8.4");
        
            asn1Writer.PushSequence();
                asn1Writer.WriteObjectIdentifier(hardwareType);
                asn1Writer.WriteOctetString(serialNumber);
            asn1Writer.PopSequence();

        asn1Writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

        return asn1Writer.Encode();
    }

    public static byte[] CreatePermanentIdentifier(string? value, string? assigner)
    {
        var asn1Writer = new AsnWriter(AsnEncodingRules.DER);
        asn1Writer.PushSequence(
            new Asn1Tag(TagClass.ContextSpecific, 0));

            asn1Writer.WriteObjectIdentifier("1.3.6.1.5.5.7.8.3");
            asn1Writer.PushSequence();
                if (value != null)
                {
                    asn1Writer.WriteCharacterString(
                        UniversalTagNumber.UTF8String,
                        value);
                }

                if(assigner != null)
                {
                    asn1Writer.WriteObjectIdentifier(assigner);
                }
            asn1Writer.PopSequence();
        asn1Writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

        return asn1Writer.Encode();
    }
}
