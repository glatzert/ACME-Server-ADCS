using System.Globalization;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.HttpModel;

/// <summary>
/// Represents an ACME order
/// https://tools.ietf.org/html/rfc8555#section-7.1.3
/// </summary>
public class Order
{
    public Order(Model.Order model)
    {
        ArgumentNullException.ThrowIfNull(model);

        Status = EnumMappings.GetEnumString(model.Status);

        Expires = model.Expires?.ToString("o", CultureInfo.InvariantCulture);
        NotBefore = model.NotBefore?.ToString("o", CultureInfo.InvariantCulture);
        NotAfter = model.NotAfter?.ToString("o", CultureInfo.InvariantCulture);

        Identifiers = [.. model.Identifiers.Select(x => new Identifier(x))];

        Replaces = model.ReplacesCertificateId?.Value;

        if (model.Error != null)
            Error = new AcmeError(model.Error);
    }

    public string Status { get; }

    public List<Identifier> Identifiers { get; }

    public string? Expires { get; }
    public string? NotBefore { get; }
    public string? NotAfter { get; }

    public AcmeError? Error { get; }

    public required List<string> Authorizations { get; init; }

    /// <summary>
    /// An AKI that indicates, which certificate has been replaced (RFC 9773)
    /// </summary>
    public string? Replaces { get; init; }

    /// <summary>
    /// The finalization url of this order.
    /// </summary>
    public required string? Finalize { get; init; }

    /// <summary>
    /// The certificate retrieval url for this order
    /// </summary>
    public required string? Certificate { get; init; }
}
