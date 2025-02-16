using System.Text;
using Th11s.ACMEServer.Services;

namespace ACMEServer.ADCS.IntegrationTests.EAB
{
    internal class FakeExternalAccountBindingClient : IExternalAccountBindingClient
    {
        public Task<byte[]> GetEABHMACfromKidAsync(string kid, CancellationToken ct)
        {
            return Task.FromResult(
                Encoding.UTF8.GetBytes(EABACMEADCSWebApplicationFactory.EABKey)
            );
        }
    }
}
