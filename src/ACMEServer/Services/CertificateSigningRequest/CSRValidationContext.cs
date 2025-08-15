using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Services.X509.AlternativeNames;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest;

internal class CSRValidationContext
{
    public ProfileConfiguration ProfileConfiguration { get; }

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
    /// Returns the validation state of a specific subject alternative name.
    /// </summary>
    public bool IsAlternativeNameValid(GeneralName subjectAlternativeName)
        => AlternativeNameValidationState.TryGetValue(subjectAlternativeName, out bool isValid) && isValid;

    /// <summary>
    /// Checks if all subject alternative names have been validated.
    /// </summary>
    public bool AreAllAlternativeNamesValid()
        => AlternativeNameValidationState.All(x => x.Value);

    internal void SetAlternateNameValid(AlternativeNames.GeneralName subjectAlternativeName)
        => AlternativeNameValidationState[subjectAlternativeName] = true;
}

