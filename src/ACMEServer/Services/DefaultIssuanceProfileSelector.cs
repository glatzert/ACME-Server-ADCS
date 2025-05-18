using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services
{
    public class DefaultIssuanceProfileSelector(
        IOptions<HashSet<ProfileName>> profiles, 
        IOptionsSnapshot<ProfileConfiguration> profileDescriptors
        ) : IIssuanceProfileSelector
    {
        private readonly IOptions<HashSet<ProfileName>> _profiles = profiles;
        private readonly IOptionsSnapshot<ProfileConfiguration> _profileDescriptors = profileDescriptors;

        public Task<ProfileName> SelectProfile(Order order, bool hasExternalAccountBinding, ProfileName profileName, CancellationToken cancellationToken)
        {
            ProfileConfiguration[] candidates =
                profileName == ProfileName.None
                    ? [.. GetCandidates(order.Identifiers)]
                    : [.. GetCandidate(profileName, order.Identifiers)];

            if (candidates.Length == 0)
            {
                throw AcmeErrors.NoIssuanceProfile().AsException();
            }

            var result = candidates
                // Filtering out profiles that require external account binding if the account doesn't have one
                .Where(x => !x.RequireExternalAccountBinding || hasExternalAccountBinding)
                // Ordering by the number of supported identifiers, so we'll get the most specific one first
                .OrderBy(x => x.SupportedIdentifiers.Length)
                .First();
            
            return Task.FromResult(new ProfileName(result.Name));
        }

        private IEnumerable<ProfileConfiguration> GetCandidates(IEnumerable<Identifier> identifiers)
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

        private IEnumerable<ProfileConfiguration> GetCandidate(ProfileName profileName, IEnumerable<Identifier> identifiers)
        {
            var profileDescriptor = _profileDescriptors.Get(profileName) 
                ?? throw AcmeErrors.UnsupportedProfile(profileName).AsException();

            if (!DoesSupportAllIdentifiers(profileDescriptor, identifiers))
            {
                throw AcmeErrors.InvalidProfile(profileName).AsException();
            }

            return [profileDescriptor];
        }


        private static bool DoesSupportAllIdentifiers(ProfileConfiguration profileDescriptor, IEnumerable<Identifier> identifiers)
        {
            return identifiers.All(i => profileDescriptor.SupportedIdentifiers.Contains(i.Type));
        }
    }
}
