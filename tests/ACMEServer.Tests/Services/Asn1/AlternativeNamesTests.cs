using System.Formats.Asn1;
using System.Net;
using Th11s.ACMEServer.Services.Asn1;
using Th11s.ACMEServer.Tests.Utils;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Tests.Services.Asn1;

//Helpful tooling: https://github.com/Crypt32/Asn1Editor.WPF/releases
public class AlternativeNamesTests
{
    [Fact]
    public void OtherName_PrincipalName_Can_Be_Constructed()
    {
        var expectedTypeId = "1.3.6.1.4.1.311.20.2.3";
        var expectedValue = "thomas@th11s.de";

        var encodedData = Asn1TestHelpers.CreatePrincipalName(expectedValue);

        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 0),
            encodedData
        );


        var principalName = Assert.IsType<AlternativeNames.PrincipalName>(generalName);
        if (principalName is not null)
        {
            Assert.Equal(expectedTypeId, principalName.TypeId);
            Assert.Equal(expectedValue, principalName.Value);
        }
    }

    [Fact]
    public void OtherName_HardwareModuleName_Can_Be_Constructed()
    {
        var expectedTypeId = "1.2.3.1.5.5.7.8.4";
        var expectedHardwareType = "0.8.15.47.11";
        byte[] expectedSerialNumber = [0, 0, 0, 0, 0, 1];

        var encodedData = Asn1TestHelpers.CreateHardwareModuleName(expectedHardwareType, expectedSerialNumber);

        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 0),
            encodedData
        );

        var hardwareModuleName = Assert.IsType<AlternativeNames.HardwareModuleName>(generalName);
        if (hardwareModuleName is not null)
        {
            Assert.Equal(expectedTypeId, hardwareModuleName.TypeId);
            Assert.Equal(expectedHardwareType, hardwareModuleName.HardwareType);
            Assert.Equal(expectedSerialNumber, hardwareModuleName.SerialNumber.ToArray());
        }
    }


    [Fact]
    public void OtherName_PermanentIdentifier_Can_Be_Constructed()
    {
        var expectedTypeId = "1.3.6.1.5.5.7.8.3";
        var expectedValue = "permanent-identifier";
        var expectedAssigner = "0.8.15.47.11";

        var encodedData = Asn1TestHelpers.CreatePermanentIdentifier(expectedValue, expectedAssigner);

        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 0),
            encodedData
        );

        var permanentIdentifier = Assert.IsType<AlternativeNames.PermanentIdentifier>(generalName);
        if (permanentIdentifier is not null)
        {
            Assert.Equal(expectedTypeId, permanentIdentifier.TypeId);
            Assert.Equal(expectedValue, permanentIdentifier.Value);
            Assert.Equal(expectedAssigner, permanentIdentifier.Assigner);
        }
    }


    [Fact]
    public void OtherName_Can_Be_Constructed()
    {
        var expectedTypeId = "0.8.15.47.11";

        var encodedData = Asn1TestHelpers.CreateOtherName(expectedTypeId);

        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 0),
            encodedData
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
        var expectedValue = "thomas@th11s.de";
        var encodedData = Asn1TestHelpers.CreateRfc822Name(expectedValue);
        
        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 1), 
            encodedData
        );

        var rfc822Name = Assert.IsType<AlternativeNames.Rfc822Name>(generalName);
        if (rfc822Name is not null)
        {
            Assert.Equal(expectedValue, rfc822Name.Value);
        }
    }


    [Fact]
    public void DnsName_Can_Be_Constructed()
    {
        var expectedValue = "th11s.de";
        var encodedData = Asn1TestHelpers.CreateDnsName(expectedValue);

        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 2),
            encodedData
        );

        var dnsName = Assert.IsType<AlternativeNames.DnsName>(generalName);
        if (dnsName is not null)
        {
            Assert.Equal(expectedValue, dnsName.Value);
        }
    }


    [Fact]
    public void Uri_Can_Be_Constructed()
    {
        var expectedValue = "https://th11s.de";
        var encodedData = Asn1TestHelpers.CreateUri(expectedValue);
        
        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 6),
            encodedData
        );
        var uri = Assert.IsType<AlternativeNames.Uri>(generalName);
        if (uri is not null)
        {
            Assert.Equal(expectedValue, uri.Value);
        }
    }


    [Fact]
    public void IPAddress_Can_Be_Constructed()
    {
        var expectedIPAddress = IPAddress.Parse("2001:db8:122:344::1");

        var encodedData = Asn1TestHelpers.CreateIpAddress(expectedIPAddress);

        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 7),
            encodedData
        );

        var ipAddress = Assert.IsType<AlternativeNames.IPAddress>(generalName);
        if (ipAddress is not null)
        {
            Assert.True(expectedIPAddress.Equals(ipAddress.Value));
        }
    }


    [Fact]
    public void RegisteredId_Can_Be_Constructed()
    {
        var expectedValue = "0.8.15.47.11";
        
        var encodedData = Asn1TestHelpers.CreateRegisteredId(expectedValue);

        var generalName = AlternativeNameFactory.CreateGeneralNameFromAsn1Value(
            new Asn1Tag(TagClass.ContextSpecific, 8),
            encodedData
        );

        var registeredId = Assert.IsType<AlternativeNames.RegisteredId>(generalName);
        if (registeredId is not null)
        {
            Assert.Equal(expectedValue, registeredId.Value);
        }
    }
}
