using System.Diagnostics.CodeAnalysis;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.Tests.Utils.Fakes
{
    public class FakeProfileProvider : IProfileProvider
    {
        private readonly Dictionary<ProfileName, ProfileConfiguration> _configurations;

        public FakeProfileProvider(Dictionary<ProfileName, ProfileConfiguration> configurations)
        {
            _configurations = configurations;
        }

        public ProfileConfiguration GetProfileConfiguration(ProfileName profileName)
            => _configurations[profileName];

        public IEnumerable<ProfileName> GetProfileNames()
            => _configurations.Keys;

        public bool TryGetProfileConfiguration(ProfileName profileName, [NotNullWhen(true)] out ProfileConfiguration? profileConfiguration)
            => _configurations.TryGetValue(profileName, out profileConfiguration);
    }
}
