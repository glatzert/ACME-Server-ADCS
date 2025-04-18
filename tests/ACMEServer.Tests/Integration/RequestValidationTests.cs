using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Th11s.AcmeServer.Tests.AcmeClient;
using Th11s.ACMEServer.Model;
using HttpModel = Th11s.ACMEServer.HttpModel;

namespace Th11s.AcmeServer.Tests.Integration;

public class RequestValidationTests : IClassFixture<DefaultWebApplicationFactory>
{
    private readonly DefaultWebApplicationFactory _factory;

    public RequestValidationTests(DefaultWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpRequestMessage> CreateAcmeRequestMessage(
        HttpClient client, 
        Dictionary<string, object?> overrides,
        Func<HttpModel.Directory, string>? EndpointCallback = null,
        string directoryUrl = "/")
    {
        var directory = await client.GetFromJsonAsync<HttpModel.Directory>(directoryUrl)
            ?? throw new Exception("Directory is null - test cannot proceed");

        var requestUrl = EndpointCallback?.Invoke(directory!);
        if (requestUrl == null)
        {
            if (EndpointCallback != null)
            {
                throw new Exception("EndpointCallback returned null - test cannot proceed");
            }

            requestUrl = new Uri(client.BaseAddress!, "/test").ToString();
        }


            var nonceResponse = await client.GetAsync(directory?.NewNonce);
        var nonce = nonceResponse.Headers.GetValues("Replay-Nonce").FirstOrDefault()
            ?? throw new Exception("Nonce is null - test cannot proceed");
        

        var kid = (string?)null; // TODO: Find a way to use existing account here later

        // TODO: Create fixed RSA key for testing
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(RSA.Create(2048)));

        

        var httpRequestMessage = new HttpRequestMessage(
            HttpMethod.Post,
            requestUrl);
        httpRequestMessage.CreateAcmeMessage(jwk, nonce, kid, new object(), overrides);

        return httpRequestMessage;
    }


    [Fact]
    public async Task Empty_Request_Will_Be_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var requestMessage = new HttpRequestMessage(
            HttpMethod.Post,
            "/test");

        // Act
        var response = await client.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("urn:ietf:params:acme:error:malformed", responseContent.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Invalid_Nonce_Will_Be_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var requestMessage = await CreateAcmeRequestMessage(
            client,
            new()  { 
                { "nonce", "invalid" } 
            });
        
        // Act
        var response = await client.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("urn:ietf:params:acme:error:badNonce", responseContent.GetProperty("type").GetString());
    }

    [Theory,
        InlineData("invalid"),
        InlineData("ES384")]
    public async Task Invalid_Alg_Will_Be_Rejected(string alg)
    {
        // Arrange
        var client = _factory.CreateClient();
        var requestMessage = await CreateAcmeRequestMessage(
            client,
            new()  {
                { "alg", alg }
            });

        // Act
        var response = await client.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("urn:ietf:params:acme:error:badSignatureAlgorithm", responseContent.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Invalid_Url_Will_Be_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var requestMessage = await CreateAcmeRequestMessage(
            client,
            new()  {
                { "url", "http://localhost/invalid" }
            });
        // Act
        var response = await client.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("urn:ietf:params:acme:error:unauthorized", responseContent.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Provide_Both_Jwk_And_Kid_Will_Be_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var requestMessage = await CreateAcmeRequestMessage(
            client,
            new()  {
                { "kid", "testKid" }
            });
        // Act
        var response = await client.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("urn:ietf:params:acme:error:malformed", responseContent.GetProperty("type").GetString());
    }


    [Fact]
    public async Task Provide_Neither_Jwk_Nor_Kid_Will_Be_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var requestMessage = await CreateAcmeRequestMessage(
            client,
            new()  {
                { "jwk", null },
                { "kid", null }
            });
        // Act
        var response = await client.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("urn:ietf:params:acme:error:malformed", responseContent.GetProperty("type").GetString());
    }


    [Fact]
    public async Task Empty_Jwk_Will_Be_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var requestMessage = await CreateAcmeRequestMessage(
            client,
            new()  {
                { "jwk", new object() }
            });
        // Act
        var response = await client.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("urn:ietf:params:acme:error:malformed", responseContent.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Invalid_Signature_Will_Be_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var requestMessage = await CreateAcmeRequestMessage(
            client,
            new()  {
                { "signature", "invalid" }
            });

        // Act
        var response = await client.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("urn:ietf:params:acme:error:unauthorized", responseContent.GetProperty("type").GetString());
    }
}
