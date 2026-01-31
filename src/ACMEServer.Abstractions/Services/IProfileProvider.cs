using System.Diagnostics.CodeAnalysis;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services;

public interface IProfileProvider
{
    public IEnumerable<ProfileName> GetProfileNames();

    ProfileConfiguration GetProfileConfiguration(ProfileName profileName);

    bool TryGetProfileConfiguration(ProfileName profileName, [NotNullWhen(true)] out ProfileConfiguration? profileConfiguration);
}
