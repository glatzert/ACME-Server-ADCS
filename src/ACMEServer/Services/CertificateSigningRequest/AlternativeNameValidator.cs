using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;
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
                    _logger.LogDebug("Found matching identifier {Identifier} for SAN {SAN}.", matchedIdentifiers[i], subjectAlternativeName);
                    validationContext.SetIdentifierIsUsed(matchedIdentifiers[i]);
                }

                _logger.LogDebug("SAN {SAN} is valid, as it has a matching identifier.", subjectAlternativeName);
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
                AlternativeNames.DnsName dnsName => ValidateStringValue(validationContext, dnsName, parameters.SANValidationParameters.DnsName),
                AlternativeNames.RegisteredId registeredId => ValidateStringValue(validationContext, registeredId, parameters.SANValidationParameters.RegisteredId),
                AlternativeNames.Rfc822Name rfc822Name => ValidateStringValue(validationContext, rfc822Name, parameters.SANValidationParameters.Rfc822Name),
                AlternativeNames.Uri uri => ValidateStringValue(validationContext, uri, parameters.SANValidationParameters.URI),

                AlternativeNames.IPAddress ipAddress => ValidateIPAddress(validationContext, ipAddress, parameters.SANValidationParameters.IPAddress),

                AlternativeNames.OtherName otherName => ValidateOtherName(validationContext, otherName, parameters.SANValidationParameters.OtherName),

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


    private bool ValidateStringValue<T>(
        CSRValidationContext validationContext,
        T generalName,
        StringValueSANParameters parameters)
        where T : AlternativeNames.GeneralName, AlternativeNames.IStringConvertible
    {
        try
        {
            var validationRegex = parameters.CreateRegex();
            if (validationRegex == null)
            {
                _logger.LogDebug("No validation regex configured for {Type}. Skipping validation.", typeof(T).Name);
                return false;
            }

            var isMatch = validationRegex.IsMatch(generalName.AsString());
            _logger.LogDebug("Validating {value} against regex {ValueRegex} from profile configuration: {isMatch}.",
                generalName.AsString(), parameters.ValidationRegex, isMatch);

            if (isMatch)
            {
                validationContext.SetAlternateNameValid(generalName);
            }

            return isMatch;
        }
        catch (RegexParseException ex)
        {
            _logger.LogError(ex, "Failed to parse regex: {regex}", parameters.ValidationRegex);
        }

        return false;
    }


    private bool ValidateIPAddress(
        CSRValidationContext validationContext,
        AlternativeNames.IPAddress ipAddress,
        IPAddressSANParameters parameters)
    {
        if (parameters.ValidNetworks.Length == 0)
        {
            _logger.LogDebug("No valid networks configured for IPAddress validation. Skipping validation.");
            return false;
        }

        foreach (var allowedIpNetwork in parameters.ValidNetworks)
        {
            try
            {
                var network = IPNetwork.Parse(allowedIpNetwork);
                var isInNetwork = network.Contains(ipAddress.Value);

                _logger.LogDebug("Validating IPAddress {IPAddress} against allowed network {AllowedIPNetwork} from profile configuration: {isInNetwork}",
                    ipAddress.ToString(), allowedIpNetwork, isInNetwork);

                if (isInNetwork)
                {
                    validationContext.SetAlternateNameValid(ipAddress);
                }

                return isInNetwork;
            }
            catch (FormatException)
            {
                _logger.LogWarning("Invalid IP network format: {IpNetwork}", allowedIpNetwork);
            }
        }

        return false;
    }


    private bool ValidateOtherName(
        CSRValidationContext validationContext,
        AlternativeNames.OtherName otherName,
        OtherNameSANParameters parameters)
    {
        return otherName switch
        {
            AlternativeNames.PermanentIdentifier permanentIdentifier => ValidatePermanentIdentifier(validationContext, permanentIdentifier, parameters.PermanentIdentifier),
            AlternativeNames.HardwareModuleName hardwareModuleName => ValidateHardwareModuleName(validationContext, hardwareModuleName, parameters.HardwareModuleName),
            AlternativeNames.PrincipalName principalName => ValidateStringValue(validationContext, principalName, parameters.PrincipalName),

            _ => HandleUnknownOtherNames(validationContext, otherName, parameters)
        };


    }


    private bool ValidatePermanentIdentifier(CSRValidationContext validationContext, AlternativeNames.PermanentIdentifier permanentIdentifier, PermanentIdentifierSANParameters parameters)
    {
        try
        {
            var valueRegex = parameters.CreateValidValueRegex();
            if (valueRegex != null && valueRegex.IsMatch(permanentIdentifier.Value ?? ""))
            {
                _logger.LogInformation("Validated permanent identifier value {value} against regex {ValueRegex} from profile configuration.", permanentIdentifier.Value, parameters.ValidValueRegex);


                if (permanentIdentifier.Assigner is string assigner)
                {
                    var assignerRegex = parameters.CreateValidAssignerRegex();
                    if (assignerRegex != null && assignerRegex.IsMatch(assigner))
                    {
                        _logger.LogInformation("Validated assigner {assigner} against regex {AssignerRegex} from profile configuration.", assigner, parameters.ValidAssignerRegex);
                    }
                    else
                    {
                        _logger.LogWarning("Assigner {assigner} did not match the configured regex {AssignerRegex}.", assigner, parameters.ValidAssignerRegex);
                        return false;
                    }
                }

                validationContext.SetAlternateNameValid(permanentIdentifier);
                return true;
            }
        }
        catch (RegexParseException ex)
        {
            _logger.LogError(ex, "Failed to parse permanent identifier regex");
        }

        return false;
    }

    private bool ValidateHardwareModuleName(CSRValidationContext validationContext, AlternativeNames.HardwareModuleName hardwareModuleName, HardwareModuleNameSANParameters parameters)
    {
        try
        {
            var nameRegex = parameters.CreateValidTypeRegex();
            if (nameRegex != null && nameRegex.IsMatch(hardwareModuleName.TypeId))
            {
                _logger.LogInformation("Validated hardware module name {TypeId} against regex {TypeRegex} from profile configuration.", hardwareModuleName.TypeId, parameters.ValidTypeRegex);

                validationContext.SetAlternateNameValid(hardwareModuleName);
                return true;
            }
            else
            {
                _logger.LogWarning("Hardware module name {TypeId} did not match the configured regex {TypeRegex}.", hardwareModuleName.TypeId, parameters.ValidTypeRegex);
                return false;
            }
        }
        catch (RegexParseException ex)
        {
            _logger.LogError(ex, "Failed to parse hardware module type regex");
        }

        return false;
    }


    private bool HandleUnknownOtherNames(CSRValidationContext validationContext, AlternativeNames.OtherName otherName, OtherNameSANParameters parameters)
    {
        if (parameters.IgnoredTypes.Contains(otherName.TypeId))
        {
            _logger.LogDebug("Considering OtherName with type {TypeId} as valid, as it is configured to be ignored.", otherName.TypeId);

            validationContext.SetAlternateNameValid(otherName);
            return true;
        }

        _logger.LogWarning("Validation for OtherName with type {TypeId} is not possible and has not been ignored.", otherName.TypeId);
        return false;
    }
}
