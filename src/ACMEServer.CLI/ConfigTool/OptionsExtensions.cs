using ACMEServer.Storage.FileSystem.Configuration;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.CLI.ConfigTool;

internal static class OptionsExtensions
{
    extension(ACMEServerOptions options)
    {
        public Status CAAStatus => options.CAAIdentities.Length == 0
            ? Status.Recommended
            : Status.AllGood;
    }

    extension(FileStoreOptions options)
    {
        public Status Status => string.IsNullOrWhiteSpace(options.BasePath)
            ? Status.NeedsAttention
            : Status.AllGood;
    }

    extension(ConfigRoot.ProfileOptions options)
    {
        public Status Status => options.Items.Count == 0
            ? Status.NeedsAttention
            : Status.AllGood;
    }

    extension(ADCSOptions options)
    {
        public Status Status =>
            string.IsNullOrWhiteSpace(options.TemplateName) || string.IsNullOrWhiteSpace(options.CAServer)
                ? Status.NeedsAttention
                : Status.AllGood;
    }
}
