using Microsoft.Extensions.Logging.Abstractions;
using System.Formats.Asn1;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Services.Asn1;
using Th11s.ACMEServer.Services.CsrValidation;
using Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.AcmeServer.Tests.Services.CertificateSigningRequest
{
    /// <summary>
    /// Tests for the <see cref="AlternativeNameValidator"/> class.
    /// This especially tests the SAN validation logic that allows to deviate from the ACME default logic
    /// </summary>
    public class AlternativeNameValidatorTests
    {
        [Theory,
            MemberData(nameof(GetAlternativeNames))]
        public void AnlternativeNameValidator_Will_Allow_configured_values(GeneralName generalName, bool expectedResult)
        {
            var csrValidationParameters = new CSRValidationParameters()
            {
                SANValidationParameters = new()
                {
                    DnsName = new()
                    {
                        ValidationRegex = "^valid\\.th11s\\.it$"
                    },

                    IPAddress = new()
                    {
                        ValidNetworks = ["::0/128", "0.0.0.0/0"]
                    },

                    RegisteredId = new()
                    {
                        ValidationRegex = "^valid$"
                    },

                    Rfc822Name = new()
                    {
                        ValidationRegex = "^valid@th11s\\.it$"
                    },

                    URI = new()
                    {
                        ValidationRegex = "^urn:valid\\.th11s\\.it$"
                    },

                    OtherName = new()
                    {
                        PermanentIdentifier = new()
                        {
                            ValidValueRegex = "^valid-permanent-identifier$",
                            ValidAssignerRegex = "^valid-assigner$"
                        },
                        HardwareModuleName = new()
                        {
                            ValidTypeRegex = "^valid-hardware-module$"
                        },
                        PrincipalName = new()
                        {
                            ValidationRegex = "^valid-principal-name$"
                        },

                        IgnoredTypes = ["0.8.15.47.11"]
                    }
                }
            };
        }

        public static IEnumerable<object[]> GetAlternativeNames()
        {
            yield return [AlternativeNameFactory.CreateGeneralNameFromAsn1Value(new Asn1Tag(TagClass.ContextSpecific, 0), Convert.FromBase64String("")), true]; // valid.th11s.it
        }
    }
}
