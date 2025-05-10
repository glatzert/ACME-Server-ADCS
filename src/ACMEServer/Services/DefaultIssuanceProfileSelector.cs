using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services
{
    public class DefaultIssuanceProfileSelector(
        IOptions<HashSet<ProfileName>> profiles, 
        IOptionsSnapshot<ProfileDescriptor> profileDescriptors
        ) : IIssuanceProfileSelector
    {
        private readonly IOptions<HashSet<ProfileName>> _profiles = profiles;
        private readonly IOptionsSnapshot<ProfileDescriptor> _profileDescriptors = profileDescriptors;

        public Task<ProfileName> SelectProfile(IEnumerable<Identifier> identifiers, ProfileName profileName, CancellationToken cancellationToken)
        {
            var profiles = _profiles.Value;

            var candidates = 
                profileName == ProfileName.None
                    ? GetCandidate(profileName, identifiers).ToArray()
                    : GetCandidates(identifiers).ToArray();

            if (candidates.Count() == 0)
            {
                throw Model.AcmeErrors.NoIssuanceProfile().AsException();
            }

            // TODO: Rank the candidates
            return Task.FromResult(new ProfileName(candidates.First().Name));
        }

        private IEnumerable<ProfileDescriptor> GetCandidates(IEnumerable<Identifier> identifiers)
        {
            var profileNames = _profiles.Value;

            foreach(var profileName in profileNames)
            {
                var profileDescriptor = _profileDescriptors.Get(profileName);
                
                if (DoesSupportAllIdentifiers(profileDescriptor, identifiers))
                {
                    yield return profileDescriptor;
                }
            }
        }

        private IEnumerable<ProfileDescriptor> GetCandidate(ProfileName profileName, IEnumerable<Identifier> identifiers)
        {
            var profileDescriptor = _profileDescriptors.Get(profileName) 
                ?? throw Model.AcmeErrors.UnsupportedProfile(profileName).AsException();

            if (DoesSupportAllIdentifiers(profileDescriptor, identifiers))
            {
                throw Model.AcmeErrors.InvalidProfile(profileName).AsException();
            }

            return [profileDescriptor];
        }


        private bool DoesSupportAllIdentifiers(ProfileDescriptor profileDescriptor, IEnumerable<Identifier> identifiers)
        {
            return identifiers.All(i => profileDescriptor.SupportedIdentifiers.Contains(i.Type));
        }
    }
}
