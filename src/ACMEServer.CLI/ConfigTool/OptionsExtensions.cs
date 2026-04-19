using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Primitives;
using System.Text;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.CLI.ConfigTool;

internal static class OptionsExtensions
{
    extension(ACMEServerOptions options)
    {
        public ConfigInfo GetConfigInfo()
        {
            var tosValue = new StringBuilder();

            tosValue.Append(options.TOS.RequireAgreement ? "agreement required" : "optional");

            tosValue.Append("; Url: ");
            tosValue.Append(!string.IsNullOrWhiteSpace(options.TOS.Url) ? options.TOS.Url : "n/a");

            return new(
                "Server configuration",
                "",
                Status.None
            )
            {
                SubInfo = [
                    new(
                        "Canonical Hostname",
                        options.CanonicalHostname ?? options.CAAIdentities?.FirstOrDefault() ?? "n/a",
                        options.CanonicalHostNameStatus
                    ),

                    new(
                        "CAA Identities",
                        "",
                        options.CAAStatus
                    ) {
                        SubInfo = options.CAAIdentities != null && options.CAAIdentities.Count > 0
                            ? [..options.CAAIdentities.Select(caa => new ConfigInfo(Value: caa))]
                            : [ new ConfigInfo(Value: "none") ]
                    },

                    new(
                        "Revokation support enabled",
                        $"{options.SupportsRevokation}",
                        Status.None
                    ),

                    new(
                        "Terms of service",
                        tosValue.ToString(),
                        !string.IsNullOrWhiteSpace(options.TOS.Url) ? Status.AllGood : Status.Recommended
                    ),
                    new(
                        "External account binding",
                        options.ExternalAccountBinding is null ? "disabled" : "enabled",
                        Status.None
                    ) {
                        SubInfo = options.ExternalAccountBinding is ExternalAccountBindingOptions eab 
                            ? [
                                new("Required", $"{eab.Required}"),
                                new("MAC Retrieval Url", eab.MACRetrievalUrl)
                            ]
                            : null
                    }
                ]
            };
        }

        public void SetCanonicalHostName(string? hostname) {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                options.CanonicalHostname = null;
                return;
            }

            options.CanonicalHostname = hostname;
            options.CAAIdentities ??= [];

            if (options.CAAIdentities.Count == 0)
            {
                options.CAAIdentities.Add(hostname);
            }
        }

        public Status CAAStatus => options.CAAIdentities?.Count >= 1
            ? Status.AllGood
            : Status.Recommended;

        public Status CanonicalHostNameStatus => !string.IsNullOrWhiteSpace(options.CanonicalHostname) 
            ? Status.AllGood
            : options.CAAIdentities?.Count >= 1
                ? Status.Recommended
                : Status.NeedsAttention;
    }

    extension(FileStoreOptions options)
    {
        public Status Status => !string.IsNullOrWhiteSpace(options.BasePath)
            ? Status.AllGood
            : Status.NeedsAttention;
    }

    extension(IList<ProfileConfiguration> options)
    {
        public Status Status => options.Count != 0
            ? Status.AllGood
            : Status.NeedsAttention;
    }

    extension(ADCSOptions options)
    {
        public Status Status =>
            !string.IsNullOrWhiteSpace(options.CAServer) && !string.IsNullOrWhiteSpace(options.TemplateName)
                ? Status.AllGood
                : Status.NeedsAttention;
    }

    extension(IList<ADCSOptions> options)
    {
        public Status Status =>
            options.Count != 0
                ? options.All(opt => !string.IsNullOrWhiteSpace(opt.CAServer) && !string.IsNullOrWhiteSpace(opt.TemplateName))
                    ? Status.AllGood
                    : Status.NeedsAttention
                : Status.NeedsAttention;
    }
}
