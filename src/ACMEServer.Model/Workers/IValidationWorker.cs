namespace Th11s.ACMEServer.Model.Workers
{
    public interface IValidationWorker
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
