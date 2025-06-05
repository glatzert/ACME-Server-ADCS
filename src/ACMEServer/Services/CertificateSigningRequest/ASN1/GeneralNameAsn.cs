// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest.ASN1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct GeneralNameAsn
    {
        internal OtherNameAsn? OtherName;
        internal string? Rfc822Name;
        internal string? DnsName;
        internal ReadOnlyMemory<byte>? X400Address;
        internal ReadOnlyMemory<byte>? DirectoryName;
        //internal System.Security.Cryptography.Asn1.EdiPartyNameAsn? EdiPartyName;
        internal string? Uri;
        internal ReadOnlyMemory<byte>? IPAddress;
        internal string? RegisteredId;


        internal static GeneralNameAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, encoded, out GeneralNameAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException("SR.Cryptography_Der_Invalid_Encoding", e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out GeneralNameAsn decoded)
        {
            try
            {
                DecodeCore(ref reader, rebind, out decoded);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException("SR.Cryptography_Der_Invalid_Encoding", e);
            }
        }

        private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out GeneralNameAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            AsnValueReader explicitReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                OtherNameAsn tmpOtherName;
                OtherNameAsn.Decode(ref reader, new Asn1Tag(TagClass.ContextSpecific, 0), rebind, out tmpOtherName);
                decoded.OtherName = tmpOtherName;

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                decoded.Rfc822Name = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 1));
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                decoded.DnsName = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 2));
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
            {
                tmpSpan = reader.ReadEncodedValue();
                decoded.X400Address = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 4)))
            {
                explicitReader = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
                tmpSpan = explicitReader.ReadEncodedValue();
                decoded.DirectoryName = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                explicitReader.ThrowIfNotEmpty();
            }
            //else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 5)))
            //{
            //    System.Security.Cryptography.Asn1.EdiPartyNameAsn tmpEdiPartyName;
            //    System.Security.Cryptography.Asn1.EdiPartyNameAsn.Decode(ref reader, new Asn1Tag(TagClass.ContextSpecific, 5), rebind, out tmpEdiPartyName);
            //    decoded.EdiPartyName = tmpEdiPartyName;

            //}
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 6)))
            {
                decoded.Uri = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 6));
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 7)))
            {

                if (reader.TryReadPrimitiveOctetString(out tmpSpan, new Asn1Tag(TagClass.ContextSpecific, 7)))
                {
                    decoded.IPAddress = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.IPAddress = reader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 7));
                }

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 8)))
            {
                decoded.RegisteredId = reader.ReadObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 8));
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}