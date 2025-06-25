using Microsoft.Extensions.Logging;
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

        return validationContext.AreAllAlternativeNamesValidated();
    }

    private void ValidateAlternativeNamesViaOptions(CSRValidationContext validationContext)
    {
        if (validationContext.ProfileConfiguration.CSRValidation is not CSRValidationParameters parameters)
        { 
            return; 
        }


        if (parameters.AllowedSANValues.DNSNames.Length > 0)
        {
            var dnsNames = validationContext.AlternativeNames
                .OfType<AlternativeNames.DnsName>()
                .ToArray();

            foreach (var allowedDnsName in parameters.AllowedSANValues.DNSNames)
            {
                foreach(var dnsName in dnsNames)
                {
                    if (dnsName.Value.EndsWith(allowedDnsName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Validated DNS name {DnsName} against allowed value {AllowedDnsName} from profile configuration.", dnsName.Value, allowedDnsName);
                        validationContext.SetAlternateNameValid(dnsName);
                    }
                }
            }
        }

        if (parameters.AllowedSANValues.IPNetworks.Length > 0)
        {
            var ipAddresses = validationContext.AlternativeNames
                .OfType<AlternativeNames.IPAddress>()
                .ToArray();

            foreach (var allowedIpNetwork in parameters.AllowedSANValues.IPNetworks)
            {
                try
                {
                    var network = IPNetwork.Parse(allowedIpNetwork);
                    foreach (var ipAddress in ipAddresses)
                    {
                        if (network.Contains(ipAddress.Value))
                        {
                            _logger.LogInformation("Validated IPAddress {IPAddress} against allowed network {AllowedIPNetwork} from profile configuration.", ipAddress.ToString(), allowedIpNetwork);
                            validationContext.SetAlternateNameValid(ipAddress);
                        }
                    }
                }
                catch (FormatException)
                {
                    _logger.LogWarning("Invalid IP network format: {IpNetwork}", allowedIpNetwork);
                }
            }
        }
    }
}
