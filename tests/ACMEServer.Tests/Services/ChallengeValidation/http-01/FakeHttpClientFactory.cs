namespace Th11s.ACMEServer.Tests.Services.ChallengeValidation.http_01;

internal class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _httpClient = new();

    public HttpClient CreateClient(string name)
    {
        return _httpClient;
    }
}