using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services;

public class DefaultProfileProvider(
    ProfileNamesCollection profiles,
    IOptionsSnapshot<ProfileConfiguration> profileDescriptors
    ) : IProfileProvider
{
    private readonly ProfileNamesCollection _profiles = profiles;
    private readonly IOptionsSnapshot<ProfileConfiguration> _profileDescriptors = profileDescriptors;

    public ProfileConfiguration GetProfileConfiguration(ProfileName profileName)
        => _profileDescriptors.Get(profileName.Value);

    public IEnumerable<ProfileName> GetProfileNames()
        => _profiles.GetAllNames();

    public bool TryGetProfileConfiguration(ProfileName profileName, [NotNullWhen(true)] out ProfileConfiguration? profileConfiguration)
    {
        profileConfiguration = null;
        if(_profiles.GetAllNames().Contains(profileName))
        {
            profileConfiguration = _profileDescriptors.Get(profileName.Value);
            return true;
        }

        return false;
    }
}
