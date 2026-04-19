using Microsoft.Extensions.Configuration;
using System.CommandLine;
using Th11s.ACMEServer.CLI.CertificateIssuance;
using Th11s.ACMEServer.CLI.ConfigTool;

namespace Th11s.ACMEServer.CLI;

// This needs to be a proper Program.cs, since Top-Level classes, will reside in global namespace, which then has ambigious Program classes
public partial class Program
{
    private static int Main(string[] args)
    {
        var rootCommand = new RootCommand("ACME Server CLI Tools");

        var configCommand = CreateConfigCommand();

        var issuanceTestCommand = new Command("test-issuance", "Launch the certificate issuance test tool for ACME Server");

        


        issuanceTestCommand.SetAction((pr, ct) =>
        {
            var issuanceTestCli = new IssuanceTestCLI();
            return issuanceTestCli.RunAsync();
        });

        rootCommand.Subcommands.Add(configCommand);
        rootCommand.Subcommands.Add(issuanceTestCommand);

        var parseResult = rootCommand.Parse(args);
        foreach (var parseError in parseResult.Errors)
        {
            Console.Error.WriteLine(parseError.Message);
        }

        return parseResult.Invoke();
    }

    private static Command CreateConfigCommand()
    {
        var configCommand = new Command("config-tool", "Launch the configuration tool for ACME Server");
        var configFileArgument = new Argument<FileInfo>("config file path")
        {
            Description = "Path to the ACME Server configuration file",
            DefaultValueFactory = (_) => new FileInfo(Path.Combine(AppContext.BaseDirectory, "appsettings.Production.json"))
        };

        var dnsHostNameOption = new Option<string>("--dnsHostName", "--dns-host-name")
        {
            Description = "DNS Hostname of the ACMEServer to be used as canonical hostname and CAA"
        };

        configCommand.Arguments.Add(configFileArgument);
        configCommand.Options.Add(dnsHostNameOption);

        configCommand.SetAction((pr, ct) =>
        {
            var targetFile = pr.GetValue(configFileArgument);

            var cliArgs = new ConfigArguments()
            {
                DnsHostName = pr.GetValue(dnsHostNameOption)
            };

            var configuration = null as IConfiguration;

            var configCli = new ConfigCLI(targetFile, new(cliArgs, configuration));
            return configCli.RunAsync();
        });

        return configCommand;
    }
}