using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services.Asn1;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CsrValidation;

internal class CsrValidationContext
{
    private Dictionary<AlternativeNames.GeneralName, bool> AlternativeNameValidationState { get; }
    private Dictionary<Identifier, bool> IdentifierUsageState { get; }
    private Dictionary<string, bool> CommonNameValidationState { get; }
    private Dictionary<string, bool> ExpectedPublicKeyUsage { get; }


    internal CsrValidationContext(
        IEnumerable<Identifier> identifiers, 
        IEnumerable<AlternativeNames.GeneralName> alternativeNames, 
        IEnumerable<string> expectedPublicKeys,
        X500DistinguishedName subjectName)
    {
        IdentifierUsageState = identifiers.ToDictionary(x => x, x => false);
        AlternativeNameValidationState = alternativeNames.ToDictionary(x => x, x => false);
        ExpectedPublicKeyUsage = expectedPublicKeys.ToDictionary(x => x, x => false);
        CommonNameValidationState = subjectName.GetCommonNames().ToDictionary(x => x, x => false);
    }


    /// <summary>
    /// Checks if all identifiers have been used in the CSR.
    /// </summary>
    public bool AreAllIdentifiersUsed()
        => IdentifierUsageState.All(x => x.Value);

    /// <summary>
    /// Checks if all subject alternative names have been validated.
    /// </summary>
    public bool AreAllAlternativeNamesValid()
        => AlternativeNameValidationState.All(x => x.Value);

    /// <summary>
    /// Checks if the expected public key has been used in the CSR.
    /// </summary>
    public bool IsExpectedPublicKeyUsed()
        => ExpectedPublicKeyUsage.All(x => x.Value);

    /// <summary>
    /// Checks if all common names have been validated.
    /// </summary>
    public bool AreAllCommonNamesValid()
        => CommonNameValidationState.All(x => x.Value);



    internal bool IsIdentifierUsed(Identifier identifier)
        => IdentifierUsageState.TryGetValue(identifier, out bool isUsed) && isUsed;

    internal bool IsAlternativeNameValid(AlternativeNames.GeneralName subjectAlternativeName)
        => AlternativeNameValidationState.TryGetValue(subjectAlternativeName, out bool isValid) && isValid;

    internal bool IsCommonNameValid(string commonName)
        => CommonNameValidationState.TryGetValue(commonName, out bool isValid) && isValid;


    internal void SetIdentifierIsUsed(Identifier identifier)
        => IdentifierUsageState[identifier] = true;

    internal void SetAlternateNameValid(AlternativeNames.GeneralName subjectAlternativeName)
        => AlternativeNameValidationState[subjectAlternativeName] = true;

    internal void SetPublicKeyUsed(string publicKey)
        => ExpectedPublicKeyUsage[publicKey] = true;

    internal void SetCommonNameValid(string commonName)
        => CommonNameValidationState[commonName] = true;
}

