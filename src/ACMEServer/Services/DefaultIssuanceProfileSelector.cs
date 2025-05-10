using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Th11s.ACMEServer.Model;
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
            ProfileDescriptor[] candidates = 
                profileName == ProfileName.None
                    ? [.. GetCandidate(profileName, identifiers)]
                    : [.. GetCandidates(identifiers)];

            if (candidates.Length == 0)
            {
                throw AcmeErrors.NoIssuanceProfile().AsException();
            }

            // TODO: Rank the candidates
            return Task.FromResult(new ProfileName(candidates[0].Name));
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
                ?? throw AcmeErrors.UnsupportedProfile(profileName).AsException();

            if (DoesSupportAllIdentifiers(profileDescriptor, identifiers))
            {
                throw AcmeErrors.InvalidProfile(profileName).AsException();
            }

            return [profileDescriptor];
        }


        private static bool DoesSupportAllIdentifiers(ProfileDescriptor profileDescriptor, IEnumerable<Identifier> identifiers)
        {
            return identifiers.All(i => profileDescriptor.SupportedIdentifiers.Contains(i.Type));
        }
    }
}
