using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Extensions;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services
{
    public class DefaultIssuanceProfileSelector(
        IOptions<HashSet<ProfileName>> profiles, 
        IOptionsSnapshot<ProfileConfiguration> profileDescriptors,
        ILogger<DefaultIssuanceProfileSelector> logger
        ) : IIssuanceProfileSelector
    {
        private readonly IOptions<HashSet<ProfileName>> _profiles = profiles;
        private readonly IOptionsSnapshot<ProfileConfiguration> _profileDescriptors = profileDescriptors;
        private readonly ILogger<DefaultIssuanceProfileSelector> _logger = logger;

        public Task<ProfileName> SelectProfile(Order order, bool hasExternalAccountBinding, ProfileName profileName, CancellationToken cancellationToken)
        {
            ProfileConfiguration[] candidates =
                profileName == ProfileName.None
                    ? [.. GetCandidates(order.Identifiers)]
                    : [.. GetCandidate(profileName, order.Identifiers)];

            candidates = candidates
                .Where(x => !x.RequireExternalAccountBinding || hasExternalAccountBinding)
                .ToArray();

            if (candidates.Length == 0)
            {
                _logger.LogInformation("No issuance profile found for order {orderId} with identifiers {identifiers}", order.OrderId, order.Identifiers.AsLogString());
                throw AcmeErrors.NoIssuanceProfile().AsException();
            }

            var result = candidates
                // Ordering by the number of supported identifiers, so we'll get the most specific one first
                .OrderBy(x => x.SupportedIdentifiers.Length)
                .First();
            
            _logger.LogInformation("Selected profile {profileName} for order {orderId} with identifiers {identifiers}", result.Name, order.OrderId, order.Identifiers.AsLogString());
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
