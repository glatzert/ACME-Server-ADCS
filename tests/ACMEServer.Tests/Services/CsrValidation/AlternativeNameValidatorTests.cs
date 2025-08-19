using Microsoft.Extensions.Logging.Abstractions;
using System.Formats.Asn1;
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
        public void AnlternativeNameValidator_Will_allow_configured_values(GeneralName generalName, bool expectedResult)
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
                        ValidNetworks = ["127.0.0.0/8", "2001:db8:122:344::/64"]
                    },

                    RegisteredId = new()
                    {
                        ValidationRegex = "^0.8.15.47.11$"
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
                            ValidAssignerRegex = "^0\\.8\\.15\\.47\\.11$"
                        },
                        HardwareModuleName = new()
                        {
                            ValidTypeRegex = "^0\\.8\\.15\\.47\\.11$"
                        },
                        PrincipalName = new()
                        {
                            ValidationRegex = "^valid-principal-name$"
                        },

                        IgnoredTypes = ["0.8.15.47.11"]
                    }
                }
            };


            var sut = new AlternativeNameValidator(NullLogger.Instance);

            var validationContext = new CsrValidationContext([], [generalName], [], []);
            sut.ValidateWithCsrParameters(validationContext, [generalName], csrValidationParameters);

            Assert.Equal(expectedResult, validationContext.IsAlternativeNameValid(generalName));
        }

        public static IEnumerable<object[]> GetAlternativeNames()
        {
            yield return [GetGeneralName(2, Asn1TestHelpers.CreateDnsName("valid.th11s.it")), true];
            yield return [GetGeneralName(2, Asn1TestHelpers.CreateDnsName("invalid.th11s.it")), false];

            yield return [GetGeneralName(7, Asn1TestHelpers.CreateIpAddress(System.Net.IPAddress.Parse("127.0.0.1"))), true];
            yield return [GetGeneralName(7, Asn1TestHelpers.CreateIpAddress(System.Net.IPAddress.Parse("2001:db8:122:344::1"))), true];
            yield return [GetGeneralName(7, Asn1TestHelpers.CreateIpAddress(System.Net.IPAddress.Parse("128.0.0.1"))), false];
            yield return [GetGeneralName(7, Asn1TestHelpers.CreateIpAddress(System.Net.IPAddress.Parse("2002:db8:122:344::1"))), false];

            yield return [GetGeneralName(8, Asn1TestHelpers.CreateRegisteredId("0.8.15.47.11")), true];
            yield return [GetGeneralName(8, Asn1TestHelpers.CreateRegisteredId("0.9.16")), false];

            yield return [GetGeneralName(1, Asn1TestHelpers.CreateRfc822Name("valid@th11s.it")), true];
            yield return [GetGeneralName(1, Asn1TestHelpers.CreateRfc822Name("invalid@th11s.it")), false];

            yield return [GetGeneralName(6, Asn1TestHelpers.CreateUri("urn:valid.th11s.it")), true];
            yield return [GetGeneralName(6, Asn1TestHelpers.CreateUri("urn:invalid.th11s.it")), false];

            yield return [GetGeneralName(0, Asn1TestHelpers.CreatePermanentIdentifier("valid-permanent-identifier", "0.8.15.47.11")), true];
            yield return [GetGeneralName(0, Asn1TestHelpers.CreatePermanentIdentifier("invalid-permanent-identifier", "0.8.15.47.11")), false];

            yield return [GetGeneralName(0, Asn1TestHelpers.CreateHardwareModuleName("0.8.15.47.11", [0,0,0,1])), true];
            yield return [GetGeneralName(0, Asn1TestHelpers.CreateHardwareModuleName("0.9.16", [0,0,0,1])), false];

            yield return [GetGeneralName(0, Asn1TestHelpers.CreatePrincipalName("valid-principal-name")), true];
            yield return [GetGeneralName(0, Asn1TestHelpers.CreatePrincipalName("invalid-principal-name")), false];

            yield return [GetGeneralName(0, Asn1TestHelpers.CreateOtherName("0.8.15.47.11")), true];
            yield return [GetGeneralName(0, Asn1TestHelpers.CreateOtherName("0.9.16")), false];
        }

        private static GeneralName GetGeneralName(int tagValue, byte[] value)  
            => AlternativeNameFactory.CreateGeneralNameFromAsn1Value(new Asn1Tag(TagClass.ContextSpecific, tagValue), value);
    }
}
