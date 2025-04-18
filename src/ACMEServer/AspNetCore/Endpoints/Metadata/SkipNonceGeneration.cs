namespace Th11s.ACMEServer.AspNetCore.Endpoints.Metadata;

/// <summary>
/// Adding this to router metadata will prevent the nonce header from being added to the response.
/// </summary>
public class SkipNonceGeneration { }
