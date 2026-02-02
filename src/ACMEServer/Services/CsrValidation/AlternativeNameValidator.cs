using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CsrValidation;

internal class AlternativeNameValidator(ILogger logger)
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// All SANs must have a matching identifier in the order. If not, the order is invalid.
    /// </summary>
    internal void ValidateAlternativeNamesAndIdentifierUsage(
        CsrValidationContext validationContext,
        ProfileConfiguration profileConfiguration,
        IReadOnlyCollection<AlternativeNames.GeneralName> alternativeNames,
        IReadOnlyCollection<Identifier> identifiers)
    {
        // Short circuit everything if there are no alternative names.
        if (alternativeNames.Count == 0)
        {
            return;
        }

        ValidateWithIdentifiers(validationContext, alternativeNames, identifiers);

        if (validationContext.AreAllAlternativeNamesValid())
        {
            _logger.AllSansValidThroughIdentifiers();
            return;
        }


        // If not all subject alternative names are valid, yet, we'll attempt validation via profile configuration.
        if (profileConfiguration.CSRValidation is not null)
        {
            _logger.ValidatingSansViaProfileConfig();
            ValidateWithCsrParameters(validationContext, alternativeNames, profileConfiguration.CSRValidation);
        }
    }

    /// <summary>
    /// This will validate the SANs against the identifiers in the order. Each SAN that matches an identifier will be marked as valid.
    /// An identifier that matches a SAN will be marked as used.
    /// </summary>
    internal void ValidateWithIdentifiers(
        CsrValidationContext validationContext,
        IReadOnlyCollection<AlternativeNames.GeneralName> alternativeNames,
        IReadOnlyCollection<Identifier> identifiers)
    {
        var identifierLookup = identifiers.ToLookup(x => x.Type);

        foreach (var subjectAlternativeName in alternativeNames)
        {
            _logger.ValidatingSanAgainstIdentifiers(subjectAlternativeName.ToString());

            Identifier[] matchedIdentifiers = [];

            // Matches DNS type identifiers
            if (subjectAlternativeName is AlternativeNames.DnsName dnsName)
            {
                matchedIdentifiers = [.. identifierLookup[IdentifierTypes.DNS]
                    .Where(x => x.Value.Equals(dnsName.Value, StringComparison.OrdinalIgnoreCase))
                    ];
            }

            // Matches IP type identifiers
            else if (subjectAlternativeName is AlternativeNames.IPAddress ipAddress)
            {
                matchedIdentifiers = [.. identifierLookup[IdentifierTypes.IP]
                    .Where(x => IPAddress.Parse(x.Value).Equals(ipAddress.Value))
                    ];
            }

            // Matches permantent-identifier type identifiers
            else if (subjectAlternativeName is AlternativeNames.PermanentIdentifier pe)
            {
                matchedIdentifiers = [.. identifierLookup[IdentifierTypes.PermanentIdentifier]
                    .Where(x => x.Value == pe.Value)
                    ];
            }


            if (matchedIdentifiers.Length > 0)
            {
                for (int i = 0; i < matchedIdentifiers.Length; i++)
                {
                    _logger.SanMatchedIdentifier(subjectAlternativeName.ToString(), matchedIdentifiers[i]);
                    validationContext.SetIdentifierIsUsed(matchedIdentifiers[i]);
                }

                _logger.SanSetToValid(subjectAlternativeName.ToString());
                validationContext.SetAlternateNameValid(subjectAlternativeName);
            }
        }
    }


    /// <summary>
    /// This will validate the SANs against the profile configuration. Each SAN that matches the profile configuration will be marked as valid.
    /// This is not conforming to the ACME specification, but allows for more flexible validation.
    /// </summary>
    internal void ValidateWithCsrParameters(
        CsrValidationContext validationContext, 
        IReadOnlyCollection<AlternativeNames.GeneralName> alternativeNames, 
        CSRValidationParameters parameters)
    {
        var notYetValidatedNames = alternativeNames
            .Where(x => !validationContext.IsAlternativeNameValid(x))
            .ToArray();

        if (notYetValidatedNames.Length == 0)
        {
            return;
        }

        _logger.ValidatingAlternativeNamesAgainstProfile(notYetValidatedNames.Length);
        foreach (var alternativeName in notYetValidatedNames)
        {
            _logger.ValidatingAlternativeName(alternativeName.ToString());

            if (alternativeName is AlternativeNames.DnsName dnsName)
            {
                ValidateStringValue(validationContext, dnsName, parameters.SANValidationParameters?.DnsName);
            }
            else if (alternativeName is AlternativeNames.RegisteredId registeredId)
            {
                ValidateStringValue(validationContext, registeredId, parameters.SANValidationParameters?.RegisteredId);
            }
            else if (alternativeName is AlternativeNames.Rfc822Name rfc822Name)
            {
                ValidateStringValue(validationContext, rfc822Name, parameters.SANValidationParameters?.Rfc822Name);
            }
            else if (alternativeName is AlternativeNames.Uri uri)
            {
                ValidateStringValue(validationContext, uri, parameters.SANValidationParameters?.URI);
            }
            else if (alternativeName is AlternativeNames.IPAddress ipAddress)
            {
                ValidateIPAddress(validationContext, ipAddress, parameters.SANValidationParameters?.IPAddress);
            }
            else if (alternativeName is AlternativeNames.OtherName otherName)
            {
                ValidateOtherName(validationContext, otherName, parameters.SANValidationParameters?.OtherName);
            }
            else
            {
                _logger.AlternativeNameTypeNotImplemented(alternativeName.GetType().Name);
            }
            ;
        }
    }



    private void ValidateStringValue<T>(
        CsrValidationContext validationContext,
        T generalName,
        StringValueSANParameters? parameters)
        where T : AlternativeNames.GeneralName, AlternativeNames.IStringBasedName
    {
        if (parameters == null)
        {
            _logger.NoValidationParametersConfigured(typeof(T).Name);
            return;
        }

        if (parameters.ValidationRegex is null)
        {
            _logger.NoValidationRegexConfigured(typeof(T).Name);
            return;
        }

        Regex? validationRegex;
        try
        {
            validationRegex = new Regex(parameters.ValidationRegex);
        }
        catch (RegexParseException ex)
        {
            _logger.FailedToParseRegex(parameters.ValidationRegex, typeof(T).Name, ex);
            return;
        }

        var isMatch = validationRegex.IsMatch(generalName.GetStringRepresentation());
        _logger.ValidatingAgainstRegex(
            generalName.GetStringRepresentation(), parameters.ValidationRegex, isMatch);

        if (isMatch)
        {
            validationContext.SetAlternateNameValid(generalName);
        }
    }


    private void ValidateIPAddress(
        CsrValidationContext validationContext,
        AlternativeNames.IPAddress ipAddress,
        IPAddressSANParameters? parameters)
    {
        if (parameters == null)
        {
            _logger.NoValidationParametersConfigured("IPAddress");
            return;
        }

        if (parameters.ValidNetworks is null or { Length: 0 })
        {
            _logger.NoValidNetworksConfigured();
            return;
        }

        foreach (var allowedIpNetwork in parameters.ValidNetworks)
        {
            IPNetwork? network;

            try
            {
                network = IPNetwork.Parse(allowedIpNetwork);
            }
            catch (FormatException)
            {
                _logger.InvalidIpNetworkFormat(allowedIpNetwork);
                continue;
            }

            var isInNetwork = network.Value.Contains(ipAddress.Value);

            _logger.ValidatingIpAddressAgainstNetwork(
                ipAddress.ToString(), allowedIpNetwork, isInNetwork);

            if (isInNetwork)
            {
                validationContext.SetAlternateNameValid(ipAddress);
            }
        }
    }


    private void ValidateOtherName(
        CsrValidationContext validationContext,
        AlternativeNames.OtherName otherName,
        OtherNameSANParameters? parameters)
    {
        if (parameters == null)
        {
            _logger.NoValidationParametersConfigured("OtherName");
            return;
        }

        _logger.ValidatingOtherName(otherName.TypeId);

        if (otherName is AlternativeNames.PermanentIdentifier permanentIdentifier)
        {
            ValidatePermanentIdentifier(validationContext, permanentIdentifier, parameters.PermanentIdentifier);
        }
        else if (otherName is AlternativeNames.HardwareModuleName hardwareModuleName)
        {
            ValidateHardwareModuleName(validationContext, hardwareModuleName, parameters.HardwareModuleName);
        }
        else if (otherName is AlternativeNames.PrincipalName principalName)
        {
            ValidateStringValue(validationContext, principalName, parameters.PrincipalName);
        }
        else
        {
            ValidateUnknownOtherNameType(validationContext, otherName, parameters);
        }
    }


    private void ValidatePermanentIdentifier(CsrValidationContext validationContext, AlternativeNames.PermanentIdentifier permanentIdentifier, PermanentIdentifierSANParameters? parameters)
    {
        if (parameters is null)
        {
            _logger.NoValidationParametersConfigured("PermanentIdentifier");
            return;
        }

        // Were taking a regex that checks for an empty string, so we can safely replace it later.
        Regex valueRegex = new("^$");
        Regex assignerRegex = new("^$");

        try
        {
            if (parameters.ValidValueRegex is string validValueRegex)
            {
                valueRegex = new Regex(validValueRegex);
            }
        }
        catch (RegexParseException ex)
        {
            _logger.FailedToParsePermanentIdentifierValueRegex(ex);
        }

        try
        {
            if (parameters.ValidAssignerRegex is string validAssignerRegex)
            {
                assignerRegex = new Regex(validAssignerRegex);
            }
        }
        catch (RegexParseException ex)
        {
            _logger.FailedToParsePermanentIdentifierAssignerRegex(ex);
        }


        var isValidValue = valueRegex.IsMatch(permanentIdentifier.Value ?? "");
        var isValidAssigner = permanentIdentifier.Assigner is null || assignerRegex.IsMatch(permanentIdentifier.Assigner);

        _logger.ValidatedPermanentIdentifierValue(permanentIdentifier.Value, parameters.ValidValueRegex, isValidValue);

        if (permanentIdentifier.Assigner is not null)
        {
            _logger.ValidatedPermanentIdentifierAssigner(permanentIdentifier.Assigner, parameters.ValidAssignerRegex, isValidAssigner);
        }
        else
        {
            _logger.PermanentIdentifierAssignerIsNull();
        }


        if (isValidValue && isValidAssigner)
        {
            validationContext.SetAlternateNameValid(permanentIdentifier);
        }
    }

    private void ValidateHardwareModuleName(CsrValidationContext validationContext, AlternativeNames.HardwareModuleName hardwareModuleName, HardwareModuleNameSANParameters? parameters)
    {
        if (parameters is null)
        {
            _logger.NoValidationParametersConfigured("HardwareModuleName");
            return;
        }

        if (parameters.ValidTypeRegex is null)
        {
            _logger.NoValidTypeRegexConfigured();
            return;
        }


        Regex? validTypeRegex;
        try
        {
            validTypeRegex = new Regex(parameters.ValidTypeRegex);
        }
        catch (RegexParseException ex)
        {
            _logger.FailedToParseHardwareModuleTypeRegex(parameters.ValidTypeRegex, ex);
            return;
        }

        var isMatch = validTypeRegex.IsMatch(hardwareModuleName.HardwareType);

        _logger.ValidatingHardwareModuleName(
            hardwareModuleName.TypeId, parameters.ValidTypeRegex, isMatch);
        _logger.NoValidationForHardwareModuleSerialNumber();

        if (isMatch)
        {
            validationContext.SetAlternateNameValid(hardwareModuleName);
        }
    }


    private void ValidateUnknownOtherNameType(CsrValidationContext validationContext, AlternativeNames.OtherName otherName, OtherNameSANParameters parameters)
    {
        if (parameters.IgnoredTypes is null or { Length: 0 })
        {
            _logger.NoParametersForOtherNameIgnoredTypes();
            return;
        }

        var isIgnored = parameters.IgnoredTypes.Contains(otherName.TypeId);
        _logger.OtherNameIsIgnored(otherName.TypeId, isIgnored);

        if (isIgnored)
        {
            validationContext.SetAlternateNameValid(otherName);
        }
    }
}
