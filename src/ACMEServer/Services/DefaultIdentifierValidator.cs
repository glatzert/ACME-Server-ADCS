using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.RegularExpressions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.Services
{
    public class DefaultIdentifierValidator(
        ICAAEvaluator caaValidator,
        IOptionsSnapshot<ProfileConfiguration> options,
        ILogger<DefaultIdentifierValidator> logger
    ) : IIdentifierValidator
    {
        // TODO: This list should be syntesized from the ProfileConfiguration
        public static readonly HashSet<string> ValidIdentifierTypes = [
            IdentifierTypes.DNS,                  // RFC 8555 https://www.rfc-editor.org/rfc/rfc8555#section-9.7.7
            IdentifierTypes.IP,                   // RFC 8738 https://www.rfc-editor.org/rfc/rfc8738
            // "email",             // RFC 8823 https://www.rfc-editor.org/rfc/rfc8823
            IdentifierTypes.PermanentIdentifier, // https://www.ietf.org/archive/id/draft-acme-device-attest-03.html
            //IdentifierTypes.HardwareModule,      // https://www.ietf.org/archive/id/draft-acme-device-attest-03.html
        ];

        private readonly ICAAEvaluator _caaValidator = caaValidator;
        private readonly IOptionsSnapshot<ProfileConfiguration> _options = options;
        private readonly ILogger<DefaultIdentifierValidator> _logger = logger;

        //TODO: this method should be removed
        public async Task<AcmeValidationResult> ValidateOrderAsync(Order order, CancellationToken cancellationToken)
        {
            var profileConfig = _options.Get(order.Profile.Value);

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

        public async Task<IDictionary<Identifier, AcmeValidationResult>> ValidateIdentifiersAsync(
            IEnumerable<Identifier> identifiers, 
            ProfileConfiguration profileConfig, 
            CancellationToken cancellationToken)
        {
            var result = identifiers.ToDictionary(
                x => x,
                _ => AcmeValidationResult.Failed(AcmeErrors.MalformedRequest("Validation not yet performed."))
            );

            foreach (var identifier in identifiers)
            {
                if (!ValidIdentifierTypes.Contains(identifier.Type))
                {
                    result[identifier] = AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier type {identifier.Type} is not supported."));
                    continue;
                }

                if (identifier.Type == IdentifierTypes.DNS)
                {
                    if (!IsValidHostname(identifier.Value, profileConfig.IdentifierValidation.DNS))
                    {
                        result[identifier] = AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier value {identifier.Value} is not a valid DNS identifier."));
                        continue;
                    }

                    if (!await _caaValidator.IsCAAAllowingCertificateIssuance(identifier))
                    {
                        result[identifier] = AcmeValidationResult.Failed(AcmeErrors.CAA());
                        _logger.LogWarning("The identifier {identifier} was not valid due to CAA restrictions.", identifier.ToString());
                        continue;
                    }

                    result[identifier] = AcmeValidationResult.Success();
                }
                else if (identifier.Type == IdentifierTypes.IP)
                {
                    result[identifier] = IsValidIPAddress(identifier.Value, profileConfig.IdentifierValidation.IP)
                        ? AcmeValidationResult.Success()
                        : AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier value {identifier.Value} is not a valid IP identifier."));
                }
                else if (identifier.Type == IdentifierTypes.Email)
                {
                    result[identifier] = IsValidEmailAddress(identifier.Value)
                        ? AcmeValidationResult.Success()
                        : AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier value {identifier.Value} is not a valid email identifier."));
                }
                else if (identifier.Type == IdentifierTypes.PermanentIdentifier)
                {
                    result[identifier] = IsValidPermanentIdentifier(identifier.Value, profileConfig.IdentifierValidation.PermanentIdentifier)
                        ? AcmeValidationResult.Success()
                        : AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier value {identifier.Value} is not a valid permanent identifier."));
                }
                else if (identifier.Type == IdentifierTypes.HardwareModule)
                {
                    result[identifier] = IsValidHardwareModule(identifier.Value)
                        ? AcmeValidationResult.Success()
                        : AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier value {identifier.Value} is not a valid hardware-module identifier."));
                }
                else
                {
                    result[identifier] = AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier type {identifier.Type} cannot be validated."));
                }
            }

            return result;
        }


        private static bool IsValidHostname(string? hostname, DNSValidationParameters parameters)
        {
            if(hostname is null)
            {
                return false;
            }

            // RFC 1035 Section 2.3.1 https://datatracker.ietf.org/doc/html/rfc1035#section-2.3.1
            const string dnsLabelRegex = @"^(?!-)[A-Za-z0-9-]{1,63}(?<!-)$";

            var isValidRFC1035DnsName = !string.IsNullOrEmpty(hostname) &&
                   hostname.Length <= 255 &&
                   hostname.Split('.')
                        .Select((part, idx) => (part, idx))
                        .All(x => 
                            Regex.IsMatch(x.part, dnsLabelRegex) || 
                            (x.idx == 0 && x.part == "*"));

            var isAllowedName = parameters.AllowedDNSNames
                .Any(x => hostname.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));

            return isValidRFC1035DnsName && isAllowedName;
        }


        private bool IsValidIPAddress(string? address, IPValidationParameters parameters)
        {
            if (!IPAddress.TryParse(address, out var ipAddress))
            {
                return false;
            }

            foreach (var allowedNetwork in parameters.AllowedIPNetworks)
            {
                if (!IPNetwork.TryParse(allowedNetwork, out var network))
                {
                    _logger.LogWarning("The IP network {AllowedNetwork} is not a valid CIDR notation.", allowedNetwork);
                    continue;
                }

                if (network.Contains(ipAddress))
                {
                    return true;
                }
            }

            return false;
        }


        private static bool IsValidEmailAddress(string? emailAddress)
        {
            throw new NotImplementedException();
        }


        private static bool IsValidPermanentIdentifier(string? permanentIdentifier, PermanentIdentifierValidationParameters parameters)
        {
            //TODO: Additionally implement validation logic for permanent identifiers
            // https://www.rfc-editor.org/rfc/rfc4043#section-2
            
            return !string.IsNullOrEmpty(permanentIdentifier) &&
                Regex.IsMatch(permanentIdentifier, parameters.ValidationRegex!);
        }


        private static bool IsValidHardwareModule(string? hardwareModule)
        {
            //TODO: Implement validation logic for permanent identifiers
            // https://www.rfc-editor.org/rfc/rfc4108#section-3.1.2.1 ?
            return true;
        }
    }
}
