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
    public async Task DNS_Names_will_be_validated(bool expectedResult, params string[] dnsIdentifiers)
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
    public async Task IP_addresses_will_be_validated(bool expectedResult, params string[] ipIdentifiers)
    {
        // Arrange
        var orderValidator = new DefaultOrderValidator();
        var order = new Order("accountId", ipIdentifiers.Select(x => new Identifier(IdentifierTypes.IP, x)));

        // Act
        var result = await orderValidator.ValidateOrderAsync(order, CancellationToken.None);
        // Assert
        Assert.Equal(expectedResult, result.IsValid);
    }
    
    
    [Theory,
        InlineData([false, "INVALID"]),
    ]
    public async Task Emails_will_be_validated(bool expectedResult, params string[] permanentIds)
    {
        // Arrange
        var orderValidator = new DefaultOrderValidator();
        var order = new Order("accountId", permanentIds.Select(x => new Identifier(IdentifierTypes.Email, x)));

        // Act
        var result = await orderValidator.ValidateOrderAsync(order, CancellationToken.None);
        // Assert
        Assert.Equal(expectedResult, result.IsValid);
    }
    
    
    [Theory,
        InlineData([false, "INVALID"]),
    ]
    public async Task Permanent_Identifiers_will_be_validated(bool expectedResult, params string[] permanentIds)
    {
        // Arrange
        var orderValidator = new DefaultOrderValidator();
        var order = new Order("accountId", permanentIds.Select(x => new Identifier(IdentifierTypes.PermanentIdentifier, x)));

        // Act
        var result = await orderValidator.ValidateOrderAsync(order, CancellationToken.None);
        // Assert
        Assert.Equal(expectedResult, result.IsValid);
    }
    
    
    [Theory,
        InlineData([false, "INVALID"]),
    ]
    public async Task Hardware_Modules_will_be_validated(bool expectedResult, params string[] permanentIds)
    {
        // Arrange
        var orderValidator = new DefaultOrderValidator();
        var order = new Order("accountId", permanentIds.Select(x => new Identifier(IdentifierTypes.PermanentIdentifier, x)));

        // Act
        var result = await orderValidator.ValidateOrderAsync(order, CancellationToken.None);
        // Assert
        Assert.Equal(expectedResult, result.IsValid);
    }
}