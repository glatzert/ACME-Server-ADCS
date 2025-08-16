using System.Formats.Asn1;
using System.Net;
using Th11s.ACMEServer.Services.Asn1;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.AcmeServer.Tests.Services.Asn1;

//Helpful tooling: https://github.com/Crypt32/Asn1Editor.WPF/releases
public class AlternativeNamesTests
{
    [Fact]
    public void OtherName_PrincipalName_Can_Be_Constructed()
    {
        // Arrange
        var encodedData = "oB8GCisGAQQBgjcUAgOgEQwPdGhvbWFzQHRoMTFzLmRl";

        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 0),
            Convert.FromBase64String(encodedData)
        );


        var principalName = Assert.IsType<AlternativeNames.PrincipalName>(generalName);
        if (principalName is not null)
        {
            Assert.Equal("1.3.6.1.4.1.311.20.2.3", principalName.TypeId);
            Assert.Equal("thomas@th11s.de", principalName.Value);
        }
    }

    [Fact(Skip = "TODO: find valid testdata")]
    public void OtherName_HardwareModuleName_Can_Be_Constructed()
    {
        // Arrange
        var encodedData = "oBoGCCoDAQUFBwgEEA4GBAgPLwsEBgAAAAAAAQ==";
        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 0),
            Convert.FromBase64String(encodedData)
        );
        var hardwareModuleName = Assert.IsType<AlternativeNames.HardwareModuleName>(generalName);
        if (hardwareModuleName is not null)
        {
            Assert.Equal("1.2.3.1.5.5.7.8.4", hardwareModuleName.TypeId);
            Assert.Equal("0.8.15.47.11", hardwareModuleName.HardwareType);
            Assert.Equal([0,0,0,0,0,1], hardwareModuleName.SerialNumber.ToArray());
        }
    }


    [Fact(Skip = "TODO: find valid testdata")]
    public void OtherName_PermanentIdentifier_Can_Be_Constructed()
    {
        // Arrange
        var encodedData = "";
        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 0),
            Convert.FromBase64String(encodedData)
        );
        var permanentIdentifier = Assert.IsType<AlternativeNames.PermanentIdentifier>(generalName);
        if (permanentIdentifier is not null)
        {
            Assert.Equal("1.3.6.1.5.5.7.8.3", permanentIdentifier.TypeId);
            Assert.Equal("permanent-identifier", permanentIdentifier.Value);
            Assert.Equal("assigner", permanentIdentifier.Assigner);
        }
    }


    [Fact(Skip = "TODO: find valid testdata")]
    public void OtherName_Can_Be_Constructed()
    {
        // Arrange
        var encodedData = "";
        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 0),
            Convert.FromBase64String(encodedData)
        );
        var otherName = Assert.IsType<AlternativeNames.OtherName>(generalName);
        if (otherName is not null)
        {
            Assert.Equal("0.8.15.47.11", otherName.TypeId);
        }
    }


    [Fact]
    public void Rfc822Name_Can_Be_Constructed()
    {
        // Arrange
        var encodedData = "gQ90aG9tYXNAdGgxMXMuZGU=";
        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 1),
            Convert.FromBase64String(encodedData)
        );
        var rfc822Name = Assert.IsType<AlternativeNames.Rfc822Name>(generalName);
        if (rfc822Name is not null)
        {
            Assert.Equal("thomas@th11s.de", rfc822Name.Value);
        }
    }


    [Fact]
    public void DnsName_Can_Be_Constructed()
    {
        // Arrange
        var encodedData = "ggh0aDExcy5kZQ==";
        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 2),
            Convert.FromBase64String(encodedData)
        );
        var dnsName = Assert.IsType<AlternativeNames.DnsName>(generalName);
        if (dnsName is not null)
        {
            Assert.Equal("th11s.de", dnsName.Value);
        }
    }


    [Fact]
    public void Uri_Can_Be_Constructed()
    {
        // Arrange
        var encodedData = "hhBodHRwczovL3RoMTFzLmRl";
        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 6),
            Convert.FromBase64String(encodedData)
        );
        var uri = Assert.IsType<AlternativeNames.Uri>(generalName);
        if (uri is not null)
        {
            Assert.Equal("https://th11s.de", uri.Value);
        }
    }


    [Fact]
    public void IPAddress_Can_Be_Constructed()
    {
        // Arrange
        var encodedData = "hxAgAQ24ASIDRAAAAAAAAAAB";
        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 7),
            Convert.FromBase64String(encodedData)
        );
        var ipAddress = Assert.IsType<AlternativeNames.IPAddress>(generalName);
        if (ipAddress is not null)
        {
            Assert.True(IPAddress.Parse("2001:db8:122:344::1").Equals(ipAddress.Value));
        }
    }


    [Fact]
    public void RegisteredId_Can_Be_Constructed()
    {
        // Arrange
        var encodedData = "iAQIDy8L";
        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 8),
            Convert.FromBase64String(encodedData)
        );
        var registeredId = Assert.IsType<AlternativeNames.RegisteredId>(generalName);
        if (registeredId is not null)
        {
            Assert.Equal("0.8.15.47.11", registeredId.Value);
        }
    }
}
