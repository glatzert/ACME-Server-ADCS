using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest;

public class CSRValidator(
    IOptionsSnapshot<ProfileConfiguration> profileConfigurationOptions,
    ILogger<CSRValidator> logger
    ) : ICSRValidator
{
    private readonly IOptionsSnapshot<ProfileConfiguration> _profileConfigurationOptions = profileConfigurationOptions;
    private readonly ILogger<CSRValidator> _logger = logger;

    public Task<AcmeValidationResult> ValidateCsrAsync(Order order, CancellationToken cancellationToken)
    {
        CSRValidationContext validationContext;
        try
        {
            var profileConfiguration = _profileConfigurationOptions.Get(order.Profile.Value);
            validationContext = new CSRValidationContext(order, profileConfiguration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validation of CSR failed with exception.");
            return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CSR could not be read.")));
        }

        try
        {
            var publicKeyValidator = new ExpectedPublicKeyValidator();
            if (!publicKeyValidator.IsValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid public key.");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "Public Key Invalid.")));
            }

            var subjectValidator = new SubjectValidator();
            if (!subjectValidator.IsValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid CN.");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CN Invalid.")));
            }

            var sanValidator = new AlternativeNameValidator(_logger);
            if (!sanValidator.AreAllAlternateNamesValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid SAN.");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "SAN Invalid.")));
            }

            // ACME states that all identifiers must be present in either CN or SAN.
            if (!validationContext.AreAllIdentifiersUsed())
            {
                _logger.LogDebug("CSR validation failed. Not all identifiers where present in either CN or SAN");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "Missing identifiers in CN or SAN.")));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Validation of CSR failed with exception.");
            return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CSR could not be read.")));
        }

        _logger.LogDebug("CSR Validation succeeded.");
        return Task.FromResult(AcmeValidationResult.Success());
    }
}


internal class CSRValidationContext
{
    public ProfileConfiguration ProfileConfiguration { get; }

    public CertificateRequest CertificateRequest { get; }

    public string? SubjectName { get; init; }
    public IReadOnlyList<string>? CommonNames { get; init; }

    public IReadOnlyList<AlternativeNames.GeneralName> AlternativeNames { get; init; }
    private Dictionary<AlternativeNames.GeneralName, bool> AlternativeNameValidationState { get; }

    public ICollection<Identifier> Identifiers => IdentifierUsageState.Keys;
    private IDictionary<Identifier, bool> IdentifierUsageState { get; }

    public string[] ExpectedPublicKeys { get; private set; } = [];

    internal CSRValidationContext(Order order, ProfileConfiguration profileConfiguration)
    {
        if (string.IsNullOrWhiteSpace(order.CertificateSigningRequest))
        {
            throw AcmeErrors.BadCSR("CSR is empty or null.").AsException();
        }

        var certificateRequest = CertificateRequest.LoadSigningRequest(
            Base64UrlTextEncoder.Decode(order.CertificateSigningRequest),
            HashAlgorithmName.SHA256, // we'll not sign the request, so this is more a placeholder than anything else
            CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions // this enables loading of extensions, which is required for SAN validation
            );

        CertificateRequest = certificateRequest;

        ProfileConfiguration = profileConfiguration;

        SubjectName = certificateRequest.SubjectName.Name;

        CommonNames = certificateRequest.SubjectName.GetCommonNames();
        AlternativeNames = certificateRequest.CertificateExtensions.GetSubjectAlternativeNames();

        ExpectedPublicKeys = [.. order.Authorizations.Select(x => x.Identifier.GetExpectedPublicKey()!).Where(x => x is not null)];

        IdentifierUsageState = order.Identifiers.ToDictionary(x => x, x => false);
        AlternativeNameValidationState = AlternativeNames.ToDictionary(x => x, x => false);
    }

    /// <summary>
    /// Flags the given identifier as used in the CSR.
    /// </summary>
    /// <param name="identifier"></param>
    internal void SetIdentifierIsUsed(Identifier identifier)
        => IdentifierUsageState[identifier] = true;

    /// <summary>
    /// Checks if all identifiers have been used in the CSR.
    /// </summary>
    public bool AreAllIdentifiersUsed()
        => IdentifierUsageState.All(x => x.Value);

    /// <summary>
    /// Checks if all subject alternative names have been validated.
    /// </summary>
    public bool AreAllAlternativeNamesValidated()
        => AlternativeNameValidationState.All(x => x.Value);

    internal void SetAlternateNameValid(AlternativeNames.GeneralName subjectAlternativeName)
        => AlternativeNameValidationState[subjectAlternativeName] = true;
}


internal static class X509Extensions
{
    internal static string[] GetCommonNames(this X500DistinguishedName subject)
    {
        return [.. subject.Name.Split(',', StringSplitOptions.TrimEntries) // split CN=abc,OU=def,XY=foo into parts
            .Select(x => x.Split('=', 2, StringSplitOptions.TrimEntries)) // split each part into [CN, abc], [OU, def], [XY, foo]
            .Where(x => string.Equals("cn", x.First(), StringComparison.OrdinalIgnoreCase)) // Check for cn
            .Select(x => x.Last()) // take abc
            ];
    }


    internal static AlternativeNames.GeneralName[] GetSubjectAlternativeNames(this IEnumerable<X509Extension> extensions)
    {
        return [.. extensions.OfType<X509SubjectAlternativeNameExtension>()
            .Select(x => new AlternativeNameEnumerator(x.RawData))
            .SelectMany(x => x.EnumerateAllNames())
            ];
    }
}

