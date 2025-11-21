namespace Th11s.ACMEServer.ConfigCLI;

internal class DNSConfigBuilder
{
    public string[] NameServers { get; private set; } = [];

    internal void SetNameServer(IEnumerable<string> nameServers)
        => NameServers = [.. nameServers];
}
