using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services;

namespace Th11s.AcmeServer.Tests.Services;

public class OrderValidatorTests
{
    [Theory,
        InlineData([true, "host"]),
        InlineData([true, "example.com"]),
        InlineData([true, "some.example.com"]),
        InlineData([true, "host1.example.com", "host2.example.com"]),
        InlineData([true, "*.example.com"]),
        InlineData([false, "~Invalid~"]),
        InlineData([false, "~💻~"]),
    ]
    public async Task Valid_DNS_names_will_yield_valid(bool expectedResult, params string[] dnsIdentifiers)
    {
        // Arrange
        var orderValidator = new DefaultOrderValidator();
        var order = new Order("accountId", dnsIdentifiers.Select(x => new Identifier(IdentifierTypes.DNS, x)));
        
        // Act
        var result = await orderValidator.ValidateOrderAsync(order, CancellationToken.None);
        // Assert
        Assert.Equal(expectedResult, result.IsValid);
    }

    [Theory,
        InlineData([true, "127.0.0.1"]),
        InlineData([true, "::1"]),
        InlineData([true, "2001:db8:122:344::1"]),
        InlineData([true, "2001:db8:122:344::192.0.2.33"]),
        InlineData([false, "Invalid"]),
    ]
    public async Task Valid_IP_addresses_will_yield_valid(bool expectedResult, params string[] ipIdentifiers)
    {
        // Arrange
        var orderValidator = new DefaultOrderValidator();
        var order = new Order("accountId", ipIdentifiers.Select(x => new Identifier(IdentifierTypes.IP, x)));

        // Act
        var result = await orderValidator.ValidateOrderAsync(order, CancellationToken.None);
        // Assert
        Assert.Equal(expectedResult, result.IsValid);
    }
}