using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;
using Certify.ACME.Anvil.Acme.Resource;
using Microsoft.Extensions.DependencyModel;
using System.Security.Cryptography.X509Certificates;

namespace Th11s.ACMEServer.Tests.Integration;

public class CertificateIssuanceTests
    : IClassFixture<DefaultWebApplicationFactory>
{
    private readonly DefaultWebApplicationFactory _factory;

    public CertificateIssuanceTests(DefaultWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_DNS_Order_And_Get_Certificate()
    {
        var httpClient = _factory.CreateClient();

        var acmeContext = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, httpClient));
        await acmeContext.GetDirectory(true);

        var accountContext = await acmeContext.NewAccount("test@example.com", true);
        acmeContext.SetAccountUri(accountContext.Location);
        
        var account = await accountContext.Resource();

        Assert.Equal(AccountStatus.Valid, account.Status);
        Assert.True(account.TermsOfServiceAgreed);
        Assert.Equal("mailto:test@example.com", account.Contact.FirstOrDefault());


        var orderContext = await acmeContext.NewOrder(["example.com"]);

        var order = await orderContext.Resource();
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.NotNull(order.Expires);
        Assert.Null(order.NotBefore);
        Assert.Null(order.NotAfter);
        Assert.Null(order.Error);
        Assert.Single(order.Identifiers);
        Assert.Equal(IdentifierType.Dns, order.Identifiers.FirstOrDefault()?.Type);
        Assert.Equal("example.com", order.Identifiers.FirstOrDefault()?.Value);

        Assert.Single(order.Authorizations);

        var authzContexts = await orderContext.Authorizations();
        var authzContext = authzContexts.First();

        var authz = await authzContext.Resource();
        Assert.Equal(AuthorizationStatus.Pending, authz.Status);
        Assert.Equal(IdentifierType.Dns, authz.Identifier.Type);
        Assert.Equal("example.com", authz.Identifier.Value);
        Assert.NotNull(authz.Expires);

        Assert.Equal(3, authz.Challenges.Count);
        Assert.Contains(ChallengeTypes.Http01, authz.Challenges.Select(c => c.Type));
        Assert.Contains(ChallengeTypes.Dns01, authz.Challenges.Select(c => c.Type));
        Assert.Contains(ChallengeTypes.TlsAlpn01, authz.Challenges.Select(c => c.Type));

        var httpChallengeContext = await authzContext.Http();

        await httpChallengeContext.Validate();
        await Task.Delay(1500, TestContext.Current.CancellationToken); // This delay is longer than the tests worker delay;

        var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
        var certRequest = await orderContext.Generate(new CsrInfo
        {
            CommonName = "example.com"
        }, privateKey);

        await Task.Delay(1500, TestContext.Current.CancellationToken); // This delay is longer than the tests worker delay;

        order = await orderContext.Resource();
        var certChain = await orderContext.Download();
        Assert.NotNull(certChain);

        var certificate = X509Certificate2.CreateFromPem(certChain.ToPem());
        Assert.Equal(certificate.NotAfter, order.Expires);
    }

    [Fact]
    public async Task Invalid_Identifier_Will_Not_Be_Issued()
    {
        var httpClient = _factory.CreateClient();

        var acmeContext = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, httpClient));
        await acmeContext.GetDirectory(true);

        var accountContext = await acmeContext.NewAccount("test@example.com", true);
        
        var exception = await Assert.ThrowsAnyAsync<AcmeRequestException>(() => acmeContext.NewOrder(["invalid.com"]));
        Assert.Equal("urn:th11s:acme:error:noIssuanceProfile", exception.Error.Type);
    }
}
