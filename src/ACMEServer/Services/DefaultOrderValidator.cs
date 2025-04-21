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
                    result[identifier] = IsValidDNSIdentifier(identifier)
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

        private static bool IsValidDNSIdentifier(Identifier identifier)
        {
            // RFC 1035 Section 2.3.1 https://datatracker.ietf.org/doc/html/rfc1035#section-2.3.1
            const string dnsLabelRegex = @"^(?!-)[A-Za-z0-9-]{1,63}(?<!-)$";

            return !string.IsNullOrEmpty(identifier.Value) &&
                   identifier.Value.Length <= 255 &&
                   identifier.Value.Split('.').All(part => Regex.IsMatch(part, dnsLabelRegex));
        }

        private static bool IsValidIPIdentifier(Identifier identifier)
        {
            return IPAddress.TryParse(identifier.Value, out _);
        }
    }
}
