using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services
{
    public interface IIssuanceProfileSelector
    {
        Task<ProfileConfiguration> SelectProfile(ProfileSelectorContext context, CancellationToken cancellationToken);
    }

    public record ProfileSelectorContext(
        Order Order,
        AccountInfo Account,
        ProfileName RequestedProfileName);

    public record AccountInfo(
        AccountId AccountId,
        bool HasExternalAccountBinding);
}
