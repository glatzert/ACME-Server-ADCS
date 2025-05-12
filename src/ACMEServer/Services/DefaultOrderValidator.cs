using Microsoft.Extensions.Options;
using System.Net;
using System.Text.RegularExpressions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.Services
{
    public class DefaultOrderValidator(IOptionsSnapshot<ProfileConfiguration> options) : IOrderValidator
    {
        // TODO: This list should be syntesized from the ProfileConfiguration
        public static readonly HashSet<string> ValidIdentifierTypes = [
            IdentifierTypes.DNS,                  // RFC 8555 https://www.rfc-editor.org/rfc/rfc8555#section-9.7.7
            IdentifierTypes.IP,                   // RFC 8738 https://www.rfc-editor.org/rfc/rfc8738
            // "email",             // RFC 8823 https://www.rfc-editor.org/rfc/rfc8823
            // "permanent-identifier", // https://www.ietf.org/archive/id/draft-acme-device-attest-03.html
            // "hardware-module",      // https://www.ietf.org/archive/id/draft-acme-device-attest-03.html
        ];
        private readonly IOptionsSnapshot<ProfileConfiguration> _options = options;

        public async Task<AcmeValidationResult> ValidateOrderAsync(Order order, CancellationToken cancellationToken)
        {
            var profileConfig = _options.Get(order.Profile);

            var identifierValidationResult = await ValidateIdentifiersAsync(order.Identifiers, profileConfig, cancellationToken);

            if(identifierValidationResult.Values.Any(x => !x.IsValid))
            {
                var subErrors = identifierValidationResult.Values
                    .Where(x => !x.IsValid)
                    .Select(x => x.Error!);

                return AcmeValidationResult.Failed(AcmeErrors.Compound(subErrors));
            }

            return AcmeValidationResult.Success();
        }

        private Task<IDictionary<Identifier, AcmeValidationResult>> ValidateIdentifiersAsync(
            List<Identifier> identifiers, 
            ProfileConfiguration profileConfig, 
            CancellationToken cancellationToken)
        {
            var result = new Dictionary<Identifier, AcmeValidationResult>();

            foreach (var identifier in identifiers)
            {
                if (!ValidIdentifierTypes.Contains(identifier.Type))
                {
                    result[identifier] = AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier type {identifier.Type} is not supported."));
                    continue;
                }

                if (identifier.Type == IdentifierTypes.DNS)
                {
                    result[identifier] = IsValidDNSIdentifier(identifier, profileConfig.IdentifierValidation.DNS)
                        ? AcmeValidationResult.Success()
                        : AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier value {identifier.Value} is not a valid DNS identifier."));
                }
                else if (identifier.Type == IdentifierTypes.IP)
                {
                    result[identifier] = IsValidIPIdentifier(identifier)
                        ? AcmeValidationResult.Success()
                        : AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier value {identifier.Value} is not a valid IP identifier."));
                }
                else
                {
                    result[identifier] = AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier type {identifier.Type} cannot be validated."));
                }
            }

            return Task.FromResult((IDictionary<Identifier, AcmeValidationResult>)result);
        }

        private static bool IsValidDNSIdentifier(Identifier identifier, DNSValidationParameters dnsParameters)
        {
            // RFC 1035 Section 2.3.1 https://datatracker.ietf.org/doc/html/rfc1035#section-2.3.1
            const string dnsLabelRegex = @"^(?!-)[A-Za-z0-9-]{1,63}(?<!-)$";

            var isValidRFC1035DnsName = !string.IsNullOrEmpty(identifier.Value) &&
                   identifier.Value.Length <= 255 &&
                   identifier.Value.Split('.').All(part => Regex.IsMatch(part, dnsLabelRegex));

            var isAllowedName = dnsParameters.AllowedDNSNames
                .Any(x => identifier.Value.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));

            return isValidRFC1035DnsName && isAllowedName;
        }

        private static bool IsValidIPIdentifier(Identifier identifier)
        {
            return IPAddress.TryParse(identifier.Value, out _);
        }
    }
}
