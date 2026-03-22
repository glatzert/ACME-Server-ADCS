
using Th11s.ACMEServer.CLI.CertificateIssuance;
using Th11s.ACMEServer.CLI.ConfigTool;

if (args.Length >= 1 && args[0] == "--config-tool")
{
    var configCreationTool = new ConfigCLI();
    await configCreationTool.RunAsync();
    return;
}

if (args.Length >= 1 && args[0] == "--test-issuance")
{
    var issuanceTestTool = new IssuanceTestCLI();
    await issuanceTestTool.RunAsync();
    return;
}
