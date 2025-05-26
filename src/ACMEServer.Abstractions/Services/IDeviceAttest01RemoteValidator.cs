namespace Th11s.ACMEServer.Services
{
    public interface IDeviceAttest01RemoteValidator
    {
        /// <summary>
        /// Validates the device attestation challenge.
        /// </summary>
        /// <param name="url">The remote url to use as http request target.</param>
        /// <param name="payload">The payload for the request - supposedly the POST body.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task<bool> ValidateAsync(string url, Dictionary<string, object?> payload, CancellationToken cancellationToken);
    }
}
