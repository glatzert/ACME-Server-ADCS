using ACMEServer.Tests.Utils.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;

namespace Th11s.AcmeServer.Tests.Services;

public class IdentifierValidatorTests
{
    private static readonly AccountId testAccountId = new("accountId");

    private static readonly FakeOptionSnapshot<ProfileConfiguration> _options = new FakeProfileConfiguration(
        new ProfileConfiguration
        {
            Name = "Default",
            ADCSOptions = new() { CAServer = "localhost\\CA1", TemplateName = "Template" },
            SupportedIdentifiers = [ IdentifierTypes.DNS, IdentifierTypes.IP, IdentifierTypes.PermanentIdentifier, IdentifierTypes.HardwareModule ],
            IdentifierValidation = new()
            {
                DNS = new()
                {
                    AllowedDNSNames = ["host", "example.com"],
                },

                IP = new () 
                {
                    AllowedIPNetworks = ["127.0.0.0/8", "2001:db8:122:344::/64", "::1/128"]
                },

                PermanentIdentifier = new()
                {
                    ValidationRegex = "^[\\da-f]{8}(-[\\da-f]{4}){3}-[\\da-f]{12}$"
                },
            }
        }
    );

    // TODO: All tests share the same code, so they could be refactored to reduce duplication.

    [Theory,
        InlineData([true, "host"]),
        InlineData([true, "example.com"]),
        InlineData([true, "some.example.com"]),
        InlineData([true, "host1.example.com", "host2.example.com"]),
        InlineData([true, "*.example.com"]),
        InlineData([false, "host.test.com"]),
        InlineData([false, "~Invalid~"]),
        InlineData([false, "~💻~"]),
    ]
    public async Task DNS_Names_will_be_validated(bool expectedResult, params string[] dnsIdentifiers)
    {
        // Arrange
        var orderValidator = new DefaultIdentifierValidator(new FakeCAAEvaluator(), _options, NullLogger<DefaultIdentifierValidator>.Instance);
        var order = new Order(testAccountId, dnsIdentifiers.Select(x => new Identifier(IdentifierTypes.DNS, x)));
        order.Profile = new("Default");
        
        // Act
        var result = await orderValidator.ValidateIdentifiersAsync(
            new(order.Identifiers, _options.Get("Default"), order), 
            CancellationToken.None
        );

        // Assert
        Assert.Equal(expectedResult, result[order.Identifiers.First()].IsValid);
    }



    [Theory,
        InlineData([true, "127.0.0.1"]),
        InlineData([true, "::1"]),
        InlineData([true, "2001:db8:122:344::1"]),
        InlineData([true, "2001:db8:122:344::192.0.2.33"]),
        InlineData([false, "2002:db8:122:344::1"]),
        InlineData([false, "Invalid"]),
    ]
    public async Task IP_addresses_will_be_validated(bool expectedResult, params string[] ipIdentifiers)
    {
        // Arrange
        var orderValidator = new DefaultIdentifierValidator(new FakeCAAEvaluator(), _options, NullLogger<DefaultIdentifierValidator>.Instance);
        var order = new Order(testAccountId, ipIdentifiers.Select(x => new Identifier(IdentifierTypes.IP, x)));
        order.Profile = new("Default");

        // Act
        var result = await orderValidator.ValidateIdentifiersAsync(
            new(order.Identifiers, _options.Get("Default"), order),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(expectedResult, result[order.Identifiers.First()].IsValid);
    }
    
    
    [Theory,
        InlineData([false, "INVALID"]),
    ]
    public async Task Emails_will_be_validated(bool expectedResult, params string[] addresses)
    {
        // Arrange
        var orderValidator = new DefaultIdentifierValidator(new FakeCAAEvaluator(), _options, NullLogger<DefaultIdentifierValidator>.Instance);
        var order = new Order(testAccountId, addresses.Select(x => new Identifier(IdentifierTypes.Email, x)));
        order.Profile = new("Default");

        // Act
        var result = await orderValidator.ValidateIdentifiersAsync(
            new(order.Identifiers, _options.Get("Default"), order),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(expectedResult, result[order.Identifiers.First()].IsValid);
    }
    
    
    [Theory,
        InlineData([true, "12345678-9abc-def0-1234-56189abcdef0"]),
        InlineData([false, "INVALID"]),
    ]
    public async Task Permanent_Identifiers_will_be_validated(bool expectedResult, params string[] permanentIds)
    {
        // Arrange
        var orderValidator = new DefaultIdentifierValidator(new FakeCAAEvaluator(), _options, NullLogger<DefaultIdentifierValidator>.Instance);
        var order = new Order(testAccountId, permanentIds.Select(x => new Identifier(IdentifierTypes.PermanentIdentifier, x)));
        order.Profile = new("Default");

        // Act
        var result = await orderValidator.ValidateIdentifiersAsync(
            new(order.Identifiers, _options.Get("Default"), order),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(expectedResult, result[order.Identifiers.First()].IsValid);
    }
    
    
    [Theory,
        InlineData([false, "INVALID"]),
    ]
    public async Task Hardware_Modules_will_be_validated(bool expectedResult, params string[] permanentIds)
    {
        // Arrange
        var orderValidator = new DefaultIdentifierValidator(new FakeCAAEvaluator(), _options, NullLogger<DefaultIdentifierValidator>.Instance);
        var order = new Order(testAccountId, permanentIds.Select(x => new Identifier(IdentifierTypes.PermanentIdentifier, x)));
        order.Profile = new("Default");

        // Act
        var result = await orderValidator.ValidateIdentifiersAsync(
            new(order.Identifiers, _options.Get("Default"), order),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(expectedResult, result[order.Identifiers.First()].IsValid);
    }
}