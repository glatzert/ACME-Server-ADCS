using System.Collections.Immutable;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class ProfileNamesCollection
    {
        private HashSet<ProfileName> _profiles = [];

        public bool Add(ProfileName profile)
            => _profiles.Add(profile);

        public ISet<ProfileName> GetAllNames() => _profiles.ToImmutableHashSet();
    }
}
