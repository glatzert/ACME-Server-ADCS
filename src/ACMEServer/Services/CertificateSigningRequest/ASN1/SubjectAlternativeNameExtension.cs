// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Services.CertificateSigningRequest.AlternativeNames;
using Th11s.ACMEServer.Services.CertificateSigningRequest.ASN1;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest
{
    internal sealed class SubjectAlternativeNameExtension : X509Extension
    {
        private List<GeneralNameAsn>? _decoded;

      
        public SubjectAlternativeNameExtension(byte[] rawData, bool critical = false)
            : base("2.5.29.17", rawData, critical)
        {
            _decoded = Decode(RawData);
        }

        public SubjectAlternativeNameExtension(ReadOnlySpan<byte> rawData, bool critical = false)
            : base("2.5.29.17", rawData, critical)
        {
            _decoded = Decode(RawData);
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = null;
        }

        public IEnumerable<AlternativeName> EnumerateAlternativeNames()
        {
            List<GeneralNameAsn> decoded = (_decoded ??= Decode(RawData));

            return EnumerateAlternativeNames(decoded);
        }

        private static IEnumerable<AlternativeName> EnumerateAlternativeNames(List<GeneralNameAsn> decoded)
        {
            foreach (GeneralNameAsn item in decoded)
            {
                yield return AlternativeName.CreateFromGeneralName(item);
            }
        }

        private static List<GeneralNameAsn> Decode(ReadOnlySpan<byte> rawData)
        {
            try
            {
                AsnValueReader outer = new AsnValueReader(rawData, AsnEncodingRules.DER);
                AsnValueReader sequence = outer.ReadSequence();
                outer.ThrowIfNotEmpty();

                List<GeneralNameAsn> decoded = new List<GeneralNameAsn>();

                while (sequence.HasData)
                {
                    GeneralNameAsn.Decode(ref sequence, default, out GeneralNameAsn item);

                    // GeneralName already validates dNSName is a valid IA5String,
                    // so check iPAddress here so that it's always a consistent decode failure.
                    if (item.IPAddress.HasValue)
                    {
                        switch (item.IPAddress.GetValueOrDefault().Length)
                        {
                            case 4:
                            // IPv4
                            case 16:
                                // UPv6
                                break;
                            default:
                                throw new CryptographicException(
                                    "SR.Cryptography_X509_SAN_UnknownIPAddressSize");
                        }
                    }

                    decoded.Add(item);
                }

                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException("SR.Cryptography_Der_Invalid_Encoding", e);
            }
        }
    }
}