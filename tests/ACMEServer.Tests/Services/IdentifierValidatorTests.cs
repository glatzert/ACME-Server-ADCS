using Microsoft.Extensions.Logging.Abstractions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.Tests.Services;

public class IdentifierValidatorTests
{
    private static readonly AccountId testAccountId = new("accountId");

    private static readonly ProfileConfiguration _profileConfiguration =
        new()
        {
            Name = "Default",
            ADCSOptions = new() { CAServer = "localhost\\CA1", TemplateName = "Template" },
            SupportedIdentifiers = [IdentifierTypes.DNS, IdentifierTypes.IP, IdentifierTypes.PermanentIdentifier, IdentifierTypes.HardwareModule],
            IdentifierValidation = new()
            {
                DNS = new()
                {
                    AllowedDNSNames = ["host", "example.com"],
                },

                IP = new()
                {
                    AllowedIPNetworks = ["127.0.0.0/8", "2001:db8:122:344::/64", "::1/128"]
                },

                PermanentIdentifier = new()
                {
                    ValidationRegex = "^[\\da-f]{8}(-[\\da-f]{4}){3}-[\\da-f]{12}$"
                },
            }
        };

    [Theory,
        InlineData([true, IdentifierTypes.DNS, "host"]),
        InlineData([true, IdentifierTypes.DNS, "example.com"]),
        InlineData([true, IdentifierTypes.DNS, "some.example.com"]),
        InlineData([true, IdentifierTypes.DNS, "host1.example.com", "host2.example.com"]),
        InlineData([true, IdentifierTypes.DNS, "*.example.com"]),
        InlineData([false, IdentifierTypes.DNS, "host.test.com"]),
        InlineData([false, IdentifierTypes.DNS, "~Invalid~"]),
        InlineData([false, IdentifierTypes.DNS, "~💻~"]),
        InlineData([true, IdentifierTypes.IP, "127.0.0.1"]),
        InlineData([true, IdentifierTypes.IP, "::1"]),
        InlineData([true, IdentifierTypes.IP, "2001:db8:122:344::1"]),
        InlineData([true, IdentifierTypes.IP, "2001:db8:122:344::192.0.2.33"]),
        InlineData([false, IdentifierTypes.IP, "2002:db8:122:344::1"]),
        InlineData([false, IdentifierTypes.IP, "Invalid"]),
        InlineData([false, IdentifierTypes.Email, "INVALID"]),
        InlineData([true, IdentifierTypes.PermanentIdentifier, "12345678-9abc-def0-1234-56189abcdef0"]),
        InlineData([false, IdentifierTypes.PermanentIdentifier, "INVALID"]),
        InlineData([false, IdentifierTypes.HardwareModule, "INVALID"]),
    ]
    public async Task Identifiers_will_be_validated(bool expectedResult, string identifierType, params string[] identifiers)
    {
        // Arrange
        var orderValidator = new DefaultIdentifierValidator(NullLogger<DefaultIdentifierValidator>.Instance);
        var order = new Order(testAccountId, identifiers.Select(x => new Identifier(identifierType, x)));
        order.Profile = new("Default");
        
        // Act
        var result = await orderValidator.ValidateIdentifiersAsync(
            new(order.Identifiers, _profileConfiguration, order), 
            CancellationToken.None
        );

        Assert.Equal(identifiers.Length, result.Count);
        foreach(var identifier in order.Identifiers)
        {
            Assert.Equal(expectedResult, result[identifier].IsValid);
        }
    }
}