using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.Model;

public record IdentifierValidationContext(
    IEnumerable<Identifier> Identifiers,
    ProfileConfiguration ProfileConfig,
    Order Order
);
