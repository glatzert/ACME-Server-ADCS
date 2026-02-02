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
        IProfileProvider profileProvider,
        ILogger<DefaultIssuanceProfileSelector> logger
        ) : IIssuanceProfileSelector
    {
        private readonly IIdentifierValidator _identifierValidator = identifierValidator;
        private readonly IProfileProvider _profileProvider = profileProvider;
        private readonly ILogger<DefaultIssuanceProfileSelector> _logger = logger;

        public async Task<ProfileConfiguration> SelectProfile(ProfileSelectorContext context, CancellationToken cancellationToken)
        {
            var candidates = await GetCandidatesAsync(context, cancellationToken);

            if (candidates.Count == 0)
            {
                _logger.NoIssuanceProfileFound(context.Order.OrderId, context.Order.Identifiers.AsLogString());
                if (context.RequestedProfileName != ProfileName.None)
                {
                    throw AcmeErrors.InvalidProfile(context.RequestedProfileName).AsException();
                }

                throw AcmeErrors.NoIssuanceProfile().AsException();
            }

            var result = candidates
                // Ordering by the number of supported identifiers, so we'll get the most specific one first
                .OrderBy(x => x.SupportedIdentifiers.Length)
                .First();

            _logger.ProfileSelected(result.ProfileName, context.Order.OrderId, context.Order.Identifiers.AsLogString());
            return result;
        }

        private async Task<List<ProfileConfiguration>> GetCandidatesAsync(ProfileSelectorContext context, CancellationToken ct)
        {
            var identifiers = context.Order.Identifiers;
            var requestedSpecificProfile = context.RequestedProfileName != ProfileName.None;

            var profileNames = requestedSpecificProfile ? [context.RequestedProfileName] : _profileProvider.GetProfileNames();

            var result = new List<ProfileConfiguration>();
            foreach (var profileName in profileNames)
            {
                // this might only occur, if the client requested an non-existing profile
                if (!_profileProvider.TryGetProfileConfiguration(profileName, out var profileDescriptor))
                {
                    return [];
                }

                if (profileDescriptor.RequireExternalAccountBinding && !context.Account.HasExternalAccountBinding)
                {
                    continue;
                }

                var supportsAllIdentifiers = identifiers.All(i => profileDescriptor.SupportedIdentifiers.Contains(i.Type));
                if (!supportsAllIdentifiers)
                {
                    continue;
                }

                // Validate identifiers against the profile's validation rules.
                var validationResult = await _identifierValidator.ValidateIdentifiersAsync(new(identifiers, profileDescriptor, context.Order), ct);
                if (!validationResult.Values.All(v => v.IsValid))
                {
                    var invalidIdentifiers = validationResult.Where(x => !x.Value.IsValid);

                    var errors = string.Join(", ", invalidIdentifiers.Select(x => $"{x.Key}: {x.Value.Error}"));
                    _logger.ProfileNotConsideredDueToInvalidIdentifiers(profileName, errors);

                    continue;
                }

                result.Add(profileDescriptor);
            }

            return result;
        }
    }
}
