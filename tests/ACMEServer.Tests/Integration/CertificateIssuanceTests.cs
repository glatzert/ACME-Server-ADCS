using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;

namespace Th11s.AcmeServer.Tests.Integration;

public class CertificateIssuanceTests
    : IClassFixture<DefaultWebApplicationFactory>
{
    private readonly DefaultWebApplicationFactory _factory;

    public CertificateIssuanceTests(DefaultWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Order_And_Get_Certificate()
    {
        var httpClient = _factory.CreateClient();

        var acme = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, httpClient));
        await acme.GetDirectory(true);
        var account = await acme.NewAccount("test@example.com", true);
        acme.SetAccountUri(account.Location);

        var order = await acme.NewOrder(["example.com"]);
        var authz = await order.Authorizations();
        var httpChallenge = await authz.First().Http();

        await httpChallenge.Validate();
        await Task.Delay(1500); // This delay is longer than the tests worker delay;

        var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
        var certRequest = await order.Generate(new CsrInfo
        {
            CommonName = "example.com"
        }, privateKey);
        await Task.Delay(1500); // This delay is longer than the tests worker delay;

        var cert = await order.Download();

        Assert.NotNull(cert);
    }
}
