using System.Net;
using System.Security.Cryptography;
using Th11s.ACMEServer.Services.CertificateSigningRequest.ASN1;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest.AlternativeNames
{
    internal class AlternativeName
    {
        public required byte[] RawData { get; init; }

        internal static AlternativeName CreateFromGeneralName(GeneralNameAsn generalName)
        {
            if (generalName.OtherName is not null)
            {
                return OtherAlternativeName.Create(
                    generalName.RawData.ToArray(),
                    generalName.OtherName.Value.TypeId,
                    generalName.OtherName.Value.Value.ToArray());
            }

            else if (generalName.Rfc822Name is not null)
            {
                return new Rfc822AlternativeName
                {
                    RawData = generalName.RawData.ToArray(),
                    EmailAddress = generalName.Rfc822Name,
                };
            }

            else if (generalName.DnsName is not null)
            {
                return new DnsAlternativeName
                {
                    RawData = generalName.RawData.ToArray(),
                    DnsName = generalName.DnsName,
                };
            }

            else if (generalName.X400Address is not null)
            {
                return new X400AddressAlternativeName
                {
                    RawData = generalName.RawData.ToArray(),
                };
            }

            else if (generalName.DirectoryName is not null)
            {
                return new DirectoryAlternativeName
                {
                    RawData = generalName.RawData.ToArray(),
                };
            }

            else if (generalName.Uri is not null)
            {
                return new UriAlternativeName
                {
                    RawData = generalName.RawData.ToArray(),
                    Uri = generalName.Uri,
                };
            }

            else if (generalName.IPAddress is not null)
            {
                return new IPAddressAlternativeName
                {
                    RawData = generalName.RawData.ToArray(),
                    IPAddress = new IPAddress(generalName.IPAddress.Value.ToArray()),
                };
            }

            else if (generalName.RegisteredId is not null)
            {
                return new RegisteredIdAlternativeName
                {
                    RawData = generalName.RawData.ToArray(),
                    RegisteredId = generalName.RegisteredId,
                };
            }

            else
            {
                throw new CryptographicException("SR.Cryptography_X509_SAN_UnknownGeneralNameType");
            }
        }
    }
}
