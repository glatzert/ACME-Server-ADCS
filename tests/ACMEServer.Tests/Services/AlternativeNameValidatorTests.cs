//using Microsoft.Extensions.Logging.Abstractions;
//using Th11s.ACMEServer.Model;
//using Th11s.ACMEServer.Model.Configuration;
//using Th11s.ACMEServer.Services.CsrValidation;
//using Th11s.ACMEServer.Services.X509.AlternativeNames;

//namespace Th11s.AcmeServer.Tests.Services
//{
//    /// <summary>
//    /// Tests for the <see cref="AlternativeNameValidator"/> class.
//    /// This especially tests the SAN validation logic that allows to deviate from the ACME default logic
//    /// </summary>
//    public class AlternativeNameValidatorTests
//    {
//        [Theory,
//            MemberData(nameof(GetAlternativeNames))]
//        public void AnlternativeNameValidator_Will_Allow_configured_values(GeneralName generalName, bool expectedResult)
//        {
//            var profileConfiguration = new ProfileConfiguration
//            {
//                ADCSOptions = new ADCSOptions
//                {
//                    CAServer = "localhost\\CA1",
//                    TemplateName = "Template"
//                },

//                SupportedIdentifiers = [IdentifierTypes.DNS, IdentifierTypes.IP, IdentifierTypes.PermanentIdentifier, IdentifierTypes.HardwareModule],

//                CSRValidation = new()
//                {
//                    SANValidationParameters = new()
//                    {
//                        DnsName = new()
//                        {
//                            ValidationRegex = "^valid\\.th11s\\.it$"
//                        },

//                        IPAddress = new()
//                        {
//                            ValidNetworks = ["::0/128", "0.0.0.0/0"]
//                        },

//                        RegisteredId = new()
//                        {
//                            ValidationRegex = "^valid$"
//                        },

//                        Rfc822Name = new()
//                        {
//                            ValidationRegex = "^valid@th11s\\.it$"
//                        },

//                        URI = new()
//                        {
//                            ValidationRegex = "^urn:valid\\.th11s\\.it$"
//                        },

//                        OtherName = new()
//                        {
//                            PermanentIdentifier = new()
//                            {
//                                ValidValueRegex = "^valid-permanent-identifier$",
//                                ValidAssignerRegex = "^valid-assigner$"
//                            },
//                            HardwareModuleName = new()
//                            {
//                                ValidTypeRegex = "^valid-hardware-module$"
//                            },
//                            PrincipalName = new()
//                            {
//                                ValidationRegex = "^valid-principal-name$"
//                            },

//                            IgnoredTypes = ["0.8.15.47.11"]
//                        }
//                    }
//                }
//            };

//            var order = new Order("accountId", [])
//            {
//                CertificateSigningRequest = """
//                    MIIDTDCCAjQCAQAwfjEZMBcGA1UEAwwQZXhhbXBsZS50aDExcy5kZTELMAkGA1UE
//                    BhMCREUxDzANBgNVBAgMBkhlc3NlbjESMBAGA1UEBwwJV2llc2JhZGVuMQ4wDAYD
//                    VQQKDAVUaDExczEfMB0GCSqGSIb3DQEJARYQdGgxMXNAb3V0bG9vay5kZTCCASIw
//                    DQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAMI68EWYLwUYhtCSRBW2aB2CMWgR
//                    1ZmTQUL7g9cl81VyoAlKroY9rdqgesKM3GavXdzetrdnKo+3BdxVfBfcR/40isn9
//                    uNPhvT7bQ1VNFvqFBi+dLLx42mX4JRyzjKLEbKGE5NKY7Cte1KNr7lyG3lgvLz88
//                    cPatr2n/jkLIB3KGBI2rUL2CR170lTfwpChe9oHCybDKlt4S+u5j2eT0eeYf9IKr
//                    Ia72KiUUacCNTV9QgcTTh/h2Mfb69ko20ukpB7hHLbeXjrBjJYIHpqkEo5sfuhak
//                    6o2Yzekrdp37+mjHHzcdwYpwPw62MpSKJZHDOpOhA00X98QI9oPLD29bxGUCAwEA
//                    AaCBiDCBhQYJKoZIhvcNAQkOMXgwdjAOBgNVHQ8BAf8EBAMCBaAwIAYDVR0lAQH/
//                    BBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMEIGA1UdEQQ7MDmCEGV4YW1wbGUudGgx
//                    MXMuZGWCDXRlc3QudGgxMXMuZGWHBMYzZCqHECABDbgAAAAAAAAAAAAAAEIwDQYJ
//                    KoZIhvcNAQELBQADggEBALrP0qQNJXLAoz80p22Vjkaop9hY9HzXUn9g3T4G0aJ8
//                    4OkAbxzeZcBo8Jkg/jlpzVYQczPOBl6wWpH5GHvwqdszsBBzbXi782zAsv3cwnXa
//                    NxB5YwBKetB1x63yVrKXFZ4+lglvgq8U+b4Ts4afLqa41WJ+IdS0iFyHpQ3D5Vmy
//                    vYf0F2RhZ0JvSvV1Sga1n3UnLyoRXw65hkoELl2PFnqZWc5lO7OaAnXysf23XYuP
//                    CBFnBPLtkgw4hFeyzoTHYNIWzjbdN0RZ6W00WYQ5OYFVTNI+htPeIQgx2QdLZj0o
//                    H1tRShOrnbUJ7pfbUk+hfSMY6Urqby4wW3UufuCGml0=
//                    """.AsBase64Url()
//            };

//            var validationContext = new CsrValidationContext(order, profileConfiguration)
//            {
//                AlternativeNames = [generalName]
//            };

//            var alternativeNameValidator = new AlternativeNameValidator(NullLogger.Instance);



//            var result = alternativeNameValidator.AreAllAlternateNamesValid(validationContext);

//            Assert.Equal(expectedResult, result);
//        }

//        public static IEnumerable<object[]> GetAlternativeNames()
//        {
//            yield return [new DnsName(Convert.FromBase64String("gg52YWxpZC50aDExcy5pdA==")), true]; // valid.th11s.it
//        }
//    }
//}
