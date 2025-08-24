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
        [Fact]
        public void AlternativeNameMatchesIdentifer_IsValid()
        {
            var identifier = new Identifier(IdentifierTypes.DNS, "valid.th11s.it");

            var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
                new Asn1Tag(TagClass.ContextSpecific, 2), 
                Asn1TestHelpers.CreateDnsName("valid.th11s.it"));
            
            var sut = new AlternativeNameValidator(NullLogger.Instance);
            
            var validationContext = new CsrValidationContext([identifier], [generalName], [], []);
            sut.ValidateWithIdentifiers(validationContext, [generalName], [identifier]);
            
            Assert.True(validationContext.IsAlternativeNameValid(generalName));
            Assert.True(validationContext.IsIdentifierUsed(identifier));

            Assert.True(validationContext.AreAllAlternativeNamesValid());
            Assert.True(validationContext.AreAllIdentifiersUsed());
        }


        [Fact]
        public void AlternativeNameDoesNotMatchIdentifier_IsInvalid()
        {
            var identifier = new Identifier(IdentifierTypes.DNS, "valid.th11s.it");

            var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
                new Asn1Tag(TagClass.ContextSpecific, 2), 
                Asn1TestHelpers.CreateDnsName("invalid.th11s.it"));

            var sut = new AlternativeNameValidator(NullLogger.Instance);
            
            var validationContext = new CsrValidationContext([identifier], [generalName], [], []);
            sut.ValidateWithIdentifiers(validationContext, [generalName], [identifier]);
            
            Assert.False(validationContext.IsAlternativeNameValid(generalName));
            Assert.False(validationContext.IsIdentifierUsed(identifier));
            Assert.False(validationContext.AreAllAlternativeNamesValid());
            Assert.False(validationContext.AreAllIdentifiersUsed());
        }


        [Theory, MemberData(nameof(ConfiguredAlternativeNames))]
        public void Configured_Parameters_Will_be_used(GeneralName generalName, bool expectedResult)
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

        public static IEnumerable<object[]> ConfiguredAlternativeNames()
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


        [Theory, MemberData(nameof(IdentifierAlternativeNames))]
        public void Identifier_Alternative_Names_Are_Validated(GeneralName generalName, bool expectedResult)
        {
            Identifier[] identifiers = [
                new (IdentifierTypes.DNS, "valid.th11s.it"),
                new (IdentifierTypes.IP, "127.0.0.1"),
                new (IdentifierTypes.IP, "2001:db8:122:344::1"),
                new (IdentifierTypes.PermanentIdentifier, "valid-permanent-identifier"),
                // TODO: new (IdentifierTypes.HardwareModule, ""),
            ];

            var sut = new AlternativeNameValidator(NullLogger.Instance);
            
            var validationContext = new CsrValidationContext(identifiers, [generalName], [], []);
            sut.ValidateWithIdentifiers(validationContext, [generalName], identifiers);

            Assert.Equal(expectedResult, validationContext.IsAlternativeNameValid(generalName));
        }

        public static IEnumerable<object[]> IdentifierAlternativeNames()
        {
            yield return [GetGeneralName(2, Asn1TestHelpers.CreateDnsName("valid.th11s.it")), true];
            yield return [GetGeneralName(2, Asn1TestHelpers.CreateDnsName("invalid.th11s.it")), false];

            yield return [GetGeneralName(7, Asn1TestHelpers.CreateIpAddress(System.Net.IPAddress.Parse("127.0.0.1"))), true];
            yield return [GetGeneralName(7, Asn1TestHelpers.CreateIpAddress(System.Net.IPAddress.Parse("2001:db8:122:344::1"))), true];
            yield return [GetGeneralName(7, Asn1TestHelpers.CreateIpAddress(System.Net.IPAddress.Parse("128.0.0.1"))), false];
            yield return [GetGeneralName(7, Asn1TestHelpers.CreateIpAddress(System.Net.IPAddress.Parse("2002:db8:122:344::1"))), false];

            yield return [GetGeneralName(0, Asn1TestHelpers.CreatePermanentIdentifier("valid-permanent-identifier", null)), true];
            yield return [GetGeneralName(0, Asn1TestHelpers.CreatePermanentIdentifier("invalid-permanent-identifier", null)), false];

            // TODO: yield return [GetGeneralName(0, Asn1TestHelpers.CreateHardwareModuleName("0.8.15.47.11", [0, 0, 0, 1])), true];
            // TODO: yield return [GetGeneralName(0, Asn1TestHelpers.CreateHardwareModuleName("0.9.16", [0, 0, 0, 1])), false];
        }



        private static GeneralName GetGeneralName(int tagValue, byte[] value)  
            => AlternativeNameFactory.CreateGeneralNameFromAsn1Value(new Asn1Tag(TagClass.ContextSpecific, tagValue), value);
    }
}
