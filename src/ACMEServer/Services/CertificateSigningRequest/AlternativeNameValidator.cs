using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest;

internal class AlternativeNameValidator(ILogger logger)
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// All SANs must have a matching identifier in the order. If not, the order is invalid.
    /// This method returns false, if any SAN does not have a matching identifier.
    /// </summary>
    internal bool AreAllAlternateNamesValid(CSRValidationContext validationContext)
    {
        // No alternative names might be useless, but is valid.
        if (validationContext.AlternativeNames == null)
        {
            return true;
        }

        var identifierLookup = validationContext.Identifiers
            .ToLookup(x => x.Type);

        foreach (var subjectAlternativeName in validationContext.AlternativeNames)
        {
            Identifier[] matchedIdentifiers = [];

            if (subjectAlternativeName is AlternativeNames.DnsName dnsName)
            {
                matchedIdentifiers = [.. identifierLookup[IdentifierTypes.DNS]
                    .Where(x => x.Value.Equals(dnsName.Value, StringComparison.OrdinalIgnoreCase))
                    ];
            }

            if (subjectAlternativeName is AlternativeNames.IPAddress ipAddress)
            {
                matchedIdentifiers = [.. identifierLookup[IdentifierTypes.IP]
                    .Where(x => IPAddress.Parse(x.Value).Equals(ipAddress.Value))
                    ];
            }

            if (subjectAlternativeName is AlternativeNames.PermanentIdentifier pe)
            {
                matchedIdentifiers = [.. identifierLookup[IdentifierTypes.PermanentIdentifier]
                    .Where(x => x.Value == pe.Value)
                    ];
            }

            if (matchedIdentifiers?.Length > 0)
            {
                for (int i = 0; i < matchedIdentifiers.Length; i++)
                {
                    validationContext.SetIdentifierIsUsed(matchedIdentifiers[i]);
                }

                validationContext.SetAlternateNameValid(subjectAlternativeName);
            }
        }

        ValidateAlternativeNamesViaOptions(validationContext);

        return validationContext.AreAllAlternativeNamesValid();
    }

    private void ValidateAlternativeNamesViaOptions(CSRValidationContext validationContext)
    {
        var parameters = validationContext.ProfileConfiguration.CSRValidation;

        var notYetValidatedNames = validationContext.AlternativeNames
            .Where(x => !validationContext.IsAlternativeNameValid(x))
            .ToArray();

        if (notYetValidatedNames.Length == 0)
        {
            return;
        }

        _logger.LogDebug("Validating {Count} alternative names against profile configuration.", notYetValidatedNames.Length);
        foreach (var alternativeName in notYetValidatedNames)
        {
            var isValidSAN = alternativeName switch
            {
                AlternativeNames.DnsName dnsName => ValidateDnsName(validationContext, dnsName, parameters.AllowedSANValues),
                AlternativeNames.IPAddress ipAddress => ValidateIPAddress(validationContext, ipAddress, parameters.AllowedSANValues),
                AlternativeNames.Uri uri => ValidateUniformResourceIdentifier(validationContext, uri, parameters.AllowedSANValues),
                _ => HandleNotImplemented(alternativeName)
            };

            if (isValidSAN)
            {
                validationContext.SetAlternateNameValid(alternativeName);
            }
        }
    }

    private bool HandleNotImplemented(AlternativeNames.GeneralName alternativeName)
    {
        _logger.LogWarning("Validation for alternative name type {AlternativeNameType} is not implemented.", alternativeName.GetType().Name);
        return false;
    }


    private bool ValidateUniformResourceIdentifier(CSRValidationContext validationContext, AlternativeNames.Uri uri, CSRSANParameters allowedSANValues)
    {
        if (allowedSANValues.URIRegex is string uriRegex)
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(uriRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (regex.IsMatch(uri.Value))
                {
                    _logger.LogInformation("Validated URI {Uri} against regex {UriRegex} from profile configuration.", uri.Value, uriRegex);
                    validationContext.SetAlternateNameValid(uri);
                    return true;
                }
            }
            catch (System.Text.RegularExpressions.RegexParseException ex)
            {
                _logger.LogError(ex, "Failed to parse URI regex: {UriRegex}", uriRegex);
            }
        }

        return false;
    }


    private bool ValidateIPAddress(CSRValidationContext validationContext, AlternativeNames.IPAddress ipAddress, CSRSANParameters allowedSANValues)
    {
        if (allowedSANValues.IPNetworks.Length > 0)
        {
            var ipAddresses = validationContext.AlternativeNames
                .OfType<AlternativeNames.IPAddress>()
                .ToArray();

            foreach (var allowedIpNetwork in allowedSANValues.IPNetworks)
            {
                try
                {
                    var network = IPNetwork.Parse(allowedIpNetwork);

                    if (network.Contains(ipAddress.Value))
                    {
                        _logger.LogInformation("Validated IPAddress {IPAddress} against allowed network {AllowedIPNetwork} from profile configuration.", ipAddress.ToString(), allowedIpNetwork);
                        validationContext.SetAlternateNameValid(ipAddress);
                    }

                }
                catch (FormatException)
                {
                    _logger.LogWarning("Invalid IP network format: {IpNetwork}", allowedIpNetwork);
                }
            }
        }

        return false;
    }

    private bool ValidateDnsName(CSRValidationContext validationContext, AlternativeNames.DnsName dnsName, CSRSANParameters allowedSANValues)
    {
        if (allowedSANValues.DNSNameRegex is string dnsNameRegex)
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(dnsNameRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (regex.IsMatch(dnsName.Value))
                {
                    _logger.LogInformation("Validated DNS name {DnsName} against regex {DnsNameRegex} from profile configuration.", dnsName.Value, dnsNameRegex);
                    validationContext.SetAlternateNameValid(dnsName);
                    return true;
                }
            }
            catch (System.Text.RegularExpressions.RegexParseException ex)
            {
                _logger.LogError(ex, "Failed to parse DNS name regex: {DnsNameRegex}", dnsNameRegex);
            }
        }

        return false;
    }
}
