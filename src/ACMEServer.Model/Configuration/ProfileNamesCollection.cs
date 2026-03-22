using System.Collections;
using System.Collections.Immutable;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class ProfileNamesCollection : IEnumerable<ProfileName>
    {
        private readonly HashSet<ProfileName> _profiles = [];

        public bool Add(ProfileName profile)
            => _profiles.Add(profile);

        public int Count => _profiles.Count;

        public ISet<ProfileName> GetAllNames() => _profiles.ToImmutableHashSet();

        public IEnumerator<ProfileName> GetEnumerator()
        {
            return ((IEnumerable<ProfileName>)_profiles).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_profiles).GetEnumerator();
        }
    }
}
