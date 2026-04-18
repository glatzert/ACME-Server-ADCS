using ACMEServer.Storage.FileSystem.Configuration;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.CLI.ConfigTool;

internal static class OptionsExtensions
{
    extension(ACMEServerOptions options)
    {
        public Status CAAStatus => options.CAAIdentities?.Length >= 1
            ? Status.AllGood
            : Status.Recommended;

        public Status CanonicalHostNameStatus => !string.IsNullOrWhiteSpace(options.CanonicalHostname) 
            ? Status.AllGood
            : options.CAAIdentities?.Length >= 1
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
