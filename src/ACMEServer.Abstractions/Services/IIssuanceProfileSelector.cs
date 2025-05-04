using Th11s.ACMEServer.HttpModel;

namespace Th11s.ACMEServer.Services
{
    public interface IIssuanceProfileSelector
    {
        Task<ProfileDescriptor> SelectProfile(IEnumerable<Identifier> identifiers, string? profileName, CancellationToken cancellationToken);
    }
}
