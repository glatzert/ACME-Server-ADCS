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
        IIdentifierValidator identifierValidator,
        IOptions<HashSet<ProfileName>> profiles,
        IOptionsSnapshot<ProfileConfiguration> profileDescriptors,
        ILogger<DefaultIssuanceProfileSelector> logger
        ) : IIssuanceProfileSelector
    {
        private readonly IIdentifierValidator _identifierValidator = identifierValidator;
        private readonly IOptions<HashSet<ProfileName>> _profiles = profiles;
        private readonly IOptionsSnapshot<ProfileConfiguration> _profileDescriptors = profileDescriptors;
        private readonly ILogger<DefaultIssuanceProfileSelector> _logger = logger;

        public Task<ProfileName> SelectProfile(Order order, bool hasExternalAccountBinding, ProfileName profileName, CancellationToken cancellationToken)
        {
            ProfileConfiguration[] candidates =
                profileName == ProfileName.None
                    ? [.. (await GetCandidates(order.Identifiers))]
                    : [.. (await GetCandidate(profileName, order.Identifiers))];

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

        private async Task<IEnumerable<ProfileConfiguration>> GetCandidates(IEnumerable<Identifier> identifiers, ProfileName requestedProfile, CancellationToken ct)
        {
            var profileNames = requestedProfile == ProfileName.None
                ? _profiles.Value
                : [requestedProfile];

            var result = new List<ProfileConfiguration>();
            foreach (var profileName in profileNames)
            {
                var profileDescriptor = _profileDescriptors.Get(profileName);

                // Check if the profile exists and supports all identifiers.
                if (profileDescriptor == null || !identifiers.All(i => profileDescriptor.SupportedIdentifiers.Contains(i.Type)))
                {
                    if (requestedProfile == ProfileName.None)
                    {
                        throw AcmeErrors.InvalidProfile(requestedProfile).AsException();
                    }

                    continue;
                }

                // Validate identifiers against the profile's validation rules.
                var validationResult = await _identifierValidator.ValidateIdentifiersAsync(identifiers, profileDescriptor, ct);
                if (!validationResult.Values.All(v => v.IsValid))
                {
                    var invalidIdentifiers = validationResult.Where(x => !x.Value.IsValid);

                    var errors = string.Join(", ", invalidIdentifiers.Select(x => $"{x.Key}: {x.Value.Error}"));
                    _logger.LogDebug("Profile {profileName} was not considered due to invalid identifiers: {errors}", profileName, errors);

                    continue;
                }

                result.Add(profileDescriptor);
            }

            return result;
        }
    }
}
