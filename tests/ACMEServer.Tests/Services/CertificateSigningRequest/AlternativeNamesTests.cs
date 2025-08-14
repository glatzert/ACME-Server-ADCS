using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.AcmeServer.Tests.Services.CertificateSigningRequest
{
    public class AlternativeNamesTests
    {
        [Fact]
        public void OtherName_PrincipalName_Can_Be_Constructed()
        {
            // Arrange
            string typeId = "1.3.6.1.4.1.311.20.2.3";
            string encodedData = "oCEGCisGAQQBgjcUAgOgEwwRa2plYnVAZXF1aW5vci5jb20=";
            string encodedValue = "oBMMEWtqZWJ1QGVxdWlub3IuY29t";

            // Act
            var principalName = new AlternativeNames.PrincipalName(
                typeId,
                Convert.FromBase64String(encodedValue),
                Convert.FromBase64String(encodedData)
            );
        }
    }
}
