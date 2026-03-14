using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;
using Th11s.ACMEServer.Model.Configuration;
using Xunit;

namespace ACME.Protocol.Model.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void ProfileConfiguration_Can_Contain_Single_ADCSOptions()
        {
            var configJson = """
            {
              // [Required] List of supported identifiers for this profile.
              // Possible values are: dns, ip, permanent-identifier, hardware-module
              "SupportedIdentifiers": [ "dns", "ip", "permanent-identifier", "hardware-module" ],

              // [Required] The following settings are used to issue the certificate.
              "ADCSOptions": {
                // [Required] The CA-Server to use for certificate issuance.
                "CAServer": "CA.FQDN.com\\CA Name",

                // [Required] The template to use for certificate issuance, when no other template can be selected based on the CSR public key.
                "TemplateName": "ADCS-Options-Template",

                // [Optional] If set, this template will be used for CSRs with RSA keys, otherwise the default template will be used.
                // Possible values are: RSA, ECDsa, ECDH
                "PublicKeyAlgorithms": [ "RSA" ],

                // [Optional] The key sizes to match for this template. If empty, all key sizes will match.
                "KeySizes": [ 2048, 4096 ]
              }
            }
            """;

            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonStream(JsonToMemoryStream(configJson));
            var config = configBuilder.Build();

            var options = new ProfileConfiguration();
            config.Bind(options);

            Assert.Equal(1, options.GetCertificateServices().Length);
        }

        [Fact]
        public void ProfileConfiguration_Can_Contain_Multiple_ADCSOptions()
        {
            var configJson = """
            {
              // [Required] List of supported identifiers for this profile.
              // Possible values are: dns, ip, permanent-identifier, hardware-module
              "SupportedIdentifiers": [ "dns", "ip", "permanent-identifier", "hardware-module" ],

              // [Required] The following settings are used to issue the certificate.
              "CertificateServices": [
                {
                    // [Required] The CA-Server to use for certificate issuance.
                    "CAServer": "CA.FQDN.com\\CA Name",

                    // [Required] The template to use for certificate issuance, when no other template can be selected based on the CSR public key.
                    "TemplateName": "CertificateServices-Template-1",

                    // [Optional] If set, this template will be used for CSRs with RSA keys, otherwise the default template will be used.
                    // Possible values are: RSA, ECDsa, ECDH
                    "PublicKeyAlgorithms": [ "RSA" ],

                    // [Optional] The key sizes to match for this template. If empty, all key sizes will match.
                    "KeySizes": [ 2048, 4096 ]
                },
                {
                    // [Required] The CA-Server to use for certificate issuance.
                    "CAServer": "CA.FQDN.com\\CA Name",

                    // [Required] The template to use for certificate issuance, when no other template can be selected based on the CSR public key.
                    "TemplateName": "CertificateServices-Template-2",

                    // [Optional] If set, this template will be used for CSRs with RSA keys, otherwise the default template will be used.
                    // Possible values are: RSA, ECDsa, ECDH
                    "PublicKeyAlgorithms": [ "RSA" ],

                    // [Optional] The key sizes to match for this template. If empty, all key sizes will match.
                    "KeySizes": [ 2048, 4096 ]
                }
              ]
            }
            """;

            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonStream(JsonToMemoryStream(configJson));
            var config = configBuilder.Build();

            var options = new ProfileConfiguration();
            config.Bind(options);

            Assert.Equal(2, options.GetCertificateServices().Length);
        }

        private static MemoryStream JsonToMemoryStream(string json) 
            => new MemoryStream(Encoding.UTF8.GetBytes(json));
    }
}
