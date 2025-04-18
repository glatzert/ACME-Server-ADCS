namespace Th11s.ACMEServer.Services;

public interface IExternalAccountBindingClient
{
    Task<byte[]> GetEABHMACfromKidAsync(string kid, CancellationToken ct);
    
    Task SignalEABFailure(string kid) => Task.CompletedTask;
    Task SingalEABSucces(string kid) => Task.CompletedTask;
}
