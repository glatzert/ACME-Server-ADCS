using CERTENROLLLib;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.CertProvider.ADCS;

internal class CSRValidationContext(CX509CertificateRequestPkcs10 request, IEnumerable<Identifier> identifiers)
{
    public CX509CertificateRequestPkcs10 Request { get; } = request;

    public string? SubjectName { get; init; }
    public IReadOnlyList<string>? CommonNames { get; init; }

    public IReadOnlyList<CAlternativeName>? AlternativeNames { get; init; }

    public ICollection<Identifier> Identifiers => IdentifierValidationState.Keys;
    private IDictionary<Identifier, bool> IdentifierValidationState { get; } = identifiers.ToDictionary(x => x, x => false);


    internal static CSRValidationContext FromRequestAndOrder(CX509CertificateRequestPkcs10 request, Order order)
    {
        var (subjectName, commonNames) = TryParseSubject(request);
        var alternativeNames = CollectAlternateNames(request);

        var ctx = new CSRValidationContext(request, order.Identifiers)
        {
            SubjectName = subjectName,
            CommonNames = commonNames,

            AlternativeNames = alternativeNames
        };

        return ctx;
    }


    public void SetIdentifierToValid(Identifier identifier)
        => IdentifierValidationState[identifier] = true;

    public bool AreAllIdentifiersValid()
        => IdentifierValidationState.All(x => x.Value);

    private static (string? subjectName, List<string>? commonNames) TryParseSubject(CX509CertificateRequestPkcs10 request)
    {
        try
        {
            var subjectName = request.Subject.Name;

            if (subjectName == null)
                return (subjectName, null);

            var commonNames = subjectName.Split(',', StringSplitOptions.TrimEntries)
                .Select(x => x.Split('=', 2, StringSplitOptions.TrimEntries))
                .Where(x => string.Equals("cn", x.First(), StringComparison.OrdinalIgnoreCase)) // Check for cn=
                .Select(x => x.Last()) // take =value
                .ToList();

            return (subjectName, commonNames);
        }
        catch
        {
            return (null, null);
        }
    }

    private static List<CAlternativeName> CollectAlternateNames(CX509CertificateRequestPkcs10 request)
    {
        var subjectAlternateNames = new List<CAlternativeName>();

        var alternateNameExtensions = request.X509Extensions
            .OfType<CX509Extension>()
            .Where(x =>
                x.ObjectId.Name == CERTENROLL_OBJECTID.XCN_OID_SUBJECT_ALT_NAME2 ||
                x.ObjectId.Name == CERTENROLL_OBJECTID.XCN_OID_SUBJECT_ALT_NAME
            )
            .ToList();

        foreach (var x509Ext in alternateNameExtensions)
        {
            var x509extData = x509Ext.RawData[EncodingType.XCN_CRYPT_STRING_BASE64];
            var alternativeNames = new CX509ExtensionAlternativeNames();
            alternativeNames.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, x509extData);

            subjectAlternateNames.AddRange(alternativeNames.AlternativeNames.Cast<CAlternativeName>());
        }

        return subjectAlternateNames;
    }
}
