using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ACMEServer.Services.ChallengeValidation.Tests.http_01;

internal class HttpServer : IDisposable
{
    private readonly TaskCompletionSource _hasStarted = new();

    private readonly string _hostName;
    private readonly string _challengeContent;

    public bool HasServedHttpToken { get; private set; }
    public Task HasStarted { get; }

    public HttpServer(string hostName, string challengeContent)
    {
        _hostName = hostName;
        _challengeContent = challengeContent;

        HasStarted = _hasStarted.Task;
    }

    public async Task RunServer(CancellationToken cancellationToken)
    {
        var tokens = _challengeContent.Split(".", 2);

        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost
            .UseKestrel()
            .UseUrls($"http://{_hostName}:5000");

        var webApp = webAppBuilder.Build();
        webApp.MapGet($"/.well-known/acme-challenge/{tokens[0]}",
            () =>
            {
                HasServedHttpToken = true;
                return Results.Text(_challengeContent);
            }
        );
                
        try
        {
            await webApp.StartAsync();
            _hasStarted.SetResult();

            await Task.Delay(-1, cancellationToken);
        }
        catch(TaskCanceledException) { }
        finally
        {
            await webApp.StopAsync(CancellationToken.None);
            await webApp.DisposeAsync();
        }
    }

    public void Dispose()
    {
    }
}
