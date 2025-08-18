using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services.Asn1;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CsrValidation;

internal class CommonNameValidator(ILogger logger)
{
    private readonly ILogger _logger = logger;

    public void ValidateCommonNames(
        CsrValidationContext validationContext,
        X500DistinguishedName subjectName,
        ICollection<Identifier> identifiers,
        ICollection<AlternativeNames.GeneralName> alternativeNames
        )
    {
        if (string.IsNullOrWhiteSpace(subjectName.Name))
        {
            _logger.LogInformation("Subject name is null, skipping common name validation.");
            return;
        }

        var commonNames = subjectName.GetCommonNames().ToArray();
        if (commonNames.Length == 0)
        {
            _logger.LogInformation("No common names found in subject name, skipping common name validation.");
            return;
        }

        foreach (var commonName in commonNames)
        {
            ValidateWithIdentifiers(validationContext, commonName, identifiers);

            if(!validationContext.IsCommonNameValid(commonName))
            {
                ValidateWithAlternativeNames(validationContext, commonName, alternativeNames);
            }
        }
    }


    private void ValidateWithIdentifiers(CsrValidationContext validationContext, string commonName, ICollection<Identifier> identifiers)
    {
        // if the common name matches any identifier value, we can consider it valid
        var matchingIdentifiers = identifiers
            .Where(x => x.Value.Equals(commonName, StringComparison.OrdinalIgnoreCase))
            .ToList();


        if (matchingIdentifiers.Count != 0)
        {
            foreach (var identifier in matchingIdentifiers)
            {
                _logger.LogInformation("Common name '{CommonName}' matches identifier '{Identifier}'.", commonName, identifier);
                validationContext.SetIdentifierIsUsed(identifier);
            }

            _logger.LogInformation("Common name '{CommonName}' is valid because it matches an identifier.", commonName);
            validationContext.SetCommonNameValid(commonName);
        }
    }

    private void ValidateWithAlternativeNames(CsrValidationContext validationContext, string commonName, ICollection<AlternativeNames.GeneralName> alternativeNames)
    {
        // if the common name matches any alternative name, we can consider it valid
        var doesMatchAlternativeName = alternativeNames
            .Where(validationContext.IsAlternativeNameValid)
            .OfType<AlternativeNames.IStringBasedName>()
            .Select(x => x.GetStringRepresentation())
            .Where(x => x.Equals(commonName, StringComparison.Ordinal))
            .Any();

        if (doesMatchAlternativeName)
        {
            _logger.LogInformation("Common name '{CommonName}' is valid because it matches an alternative name.", commonName);
            validationContext.SetCommonNameValid(commonName);
        }
    }
}
