using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services.Asn1;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CsrValidation;

internal class CsrValidationContext
{
    public IReadOnlyCollection<AlternativeNames.GeneralName> AlternativeNames => AlternativeNameValidationState.Keys;
    private Dictionary<AlternativeNames.GeneralName, bool> AlternativeNameValidationState { get; }


    private Dictionary<Identifier, bool> IdentifierUsageState { get; }


    public IReadOnlyCollection<string> CommonNames => CommonNameValidationState.Keys;
    private Dictionary<string, bool> CommonNameValidationState { get; }


    public IReadOnlyCollection<string> ExpectedPublicKeys => ExpectedPublicKeyUsage.Keys;
    private Dictionary<string, bool> ExpectedPublicKeyUsage { get; }


    internal CsrValidationContext(
        IEnumerable<Identifier> identifiers, 
        IEnumerable<AlternativeNames.GeneralName> alternativeNames, 
        IEnumerable<string> expectedPublicKeys,
        X500DistinguishedName subjectName)
    {
        IdentifierUsageState = identifiers.ToDictionary(x => x, x => false);
        AlternativeNameValidationState = AlternativeNames.ToDictionary(x => x, x => false);
        CommonNameValidationState = subjectName.GetCommonNames().ToDictionary(x => x, x => false);
    }


    /// <summary>
    /// Checks if all identifiers have been used in the CSR.
    /// </summary>
    public bool AreAllIdentifiersUsed()
        => IdentifierUsageState.All(x => x.Value);

    /// <summary>
    /// Flags the given identifier as used in the CSR.
    /// </summary>
    /// <param name="identifier"></param>
    internal void SetIdentifierIsUsed(Identifier identifier)
        => IdentifierUsageState[identifier] = true;
    

    /// <summary>
    /// Returns the validation state of a specific subject alternative name.
    /// </summary>
    public bool IsAlternativeNameValid(AlternativeNames.GeneralName subjectAlternativeName)
        => AlternativeNameValidationState.TryGetValue(subjectAlternativeName, out bool isValid) && isValid;

    /// <summary>
    /// Checks if all subject alternative names have been validated.
    /// </summary>
    public bool AreAllAlternativeNamesValid()
        => AlternativeNameValidationState.All(x => x.Value);

    internal void SetAlternateNameValid(AlternativeNames.GeneralName subjectAlternativeName)
        => AlternativeNameValidationState[subjectAlternativeName] = true;


    public bool IsExpectedPublicKeyUsed()
        => ExpectedPublicKeyUsage.All(x => x.Value);
    internal void SetPublicKeyUsed(string publicKey)
        => ExpectedPublicKeyUsage[publicKey] = true;


    public bool AreAllCommonNamesValid()
        => CommonNameValidationState.All(x => x.Value);

    internal void SetCommonNameValid(string commonName)
        => CommonNameValidationState[commonName] = true;
}

