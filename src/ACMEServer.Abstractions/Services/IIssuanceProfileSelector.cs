using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services
{
    public interface IIssuanceProfileSelector
    {
        Task<ProfileName> SelectProfile(IEnumerable<Identifier> identifiers, ProfileName profileName, CancellationToken cancellationToken);
    }
}
