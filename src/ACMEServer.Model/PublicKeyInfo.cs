namespace Th11s.ACMEServer.Model;

// TODO: check if we can make this non nullable.
public record PublicKeyInfo(string? KeyType, int? KeySize);
