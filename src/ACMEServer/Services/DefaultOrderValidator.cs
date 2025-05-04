using System.Net;
using System.Text.RegularExpressions;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services
{
    public class DefaultOrderValidator : IOrderValidator
    {
        public static readonly HashSet<string> ValidIdentifierTypes = [
            IdentifierTypes.DNS,                  // RFC 8555 https://www.rfc-editor.org/rfc/rfc8555#section-9.7.7
            IdentifierTypes.IP,                   // RFC 8738 https://www.rfc-editor.org/rfc/rfc8738
            // "email",             // RFC 8823 https://www.rfc-editor.org/rfc/rfc8823
            // "permanent-identifier", // https://www.ietf.org/archive/id/draft-acme-device-attest-03.html
            // "hardware-module",      // https://www.ietf.org/archive/id/draft-acme-device-attest-03.html
        ];

        public async Task<AcmeValidationResult> ValidateOrderAsync(Order order, CancellationToken cancellationToken)
        {
            var identifierValidationResult = await ValidateIdentifiersAsync(order.Identifiers, cancellationToken);

            if(identifierValidationResult.Values.Any(x => !x.IsValid))
            {
                var subErrors = identifierValidationResult.Values
                    .Where(x => !x.IsValid)
                    .Select(x => x.Error!);

                return AcmeValidationResult.Failed(AcmeErrors.Compound(subErrors));
            }

            return AcmeValidationResult.Success();
        }

        private Task<IDictionary<Identifier, AcmeValidationResult>> ValidateIdentifiersAsync(List<Identifier> identifiers, CancellationToken cancellationToken)
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
                    result[identifier] = IsValidHostname(identifier.Value)
                        ? AcmeValidationResult.Success()
                        : AcmeValidationResult.Failed(AcmeErrors.MalformedRequest($"The identifier value {identifier.Value} is not a valid DNS identifier."));
                }
                else if (identifier.Type == IdentifierTypes.IP)
                {
                    result[identifier] = IsValidIPAddress(identifier.Value)
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
                    result[identifier] = IsValidPersistentIdentifier(identifier.Value)
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

            return Task.FromResult((IDictionary<Identifier, AcmeValidationResult>)result);
        }


        private static bool IsValidHostname(string? hostname)
        {
            // RFC 1035 Section 2.3.1 https://datatracker.ietf.org/doc/html/rfc1035#section-2.3.1
            const string dnsLabelRegex = @"^(?!-)[A-Za-z0-9-]{1,63}(?<!-)$";

            return !string.IsNullOrEmpty(hostname) &&
                   hostname.Length <= 255 &&
                   hostname.Split('.')
                        .Select((part, idx) => (part, idx))
                        .All(x => 
                            Regex.IsMatch(x.part, dnsLabelRegex) || 
                            (x.idx == 0 && x.part == "*"));
        }


        private static bool IsValidIPAddress(string? ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out _);
        }


        private static bool IsValidEmailAddress(string? emailAddress)
        {
            throw new NotImplementedException();
        }


        private static bool IsValidPersistentIdentifier(string? persistentIdentifier)
        {
            throw new NotImplementedException();
        }


        private static bool IsValidHardwareModule(string? hardwareModule)
        {
            throw new NotImplementedException();
        }
    }
}
