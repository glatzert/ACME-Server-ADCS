using Th11s.ACMEServer.Configuration;

namespace Th11s.ACMEServer.ConfigCLI;

internal class EABConfigScreen(ConfigCLI parent, ACMEServerOptions options) : CLIScreen(parent)
{
    private readonly ACMEServerOptions _options = options;
    private ExternalAccountBindingOptions? _eabOptions = options.ExternalAccountBinding;

    protected override string? ScreenTitle => "External account binding";

    protected override string? ScreenDescription => "To use external account binding, it's neccessary to get the MAC Key from a remote endpoint. This can be configured here.";

    protected override List<CLIAction> Actions
    {
        get
        {
            List<CLIAction> result = [];
            if (_options.ExternalAccountBinding is ExternalAccountBindingOptions eab)
            {
                result.AddRange([
                    new CLIAction('D', "Disable external account binding", ToggleEAB),
                    new CLIAction('R', "Require external account binding", ToggleEABRequirement),
                    new CLIAction(
                        'M',
                        "Set MAC retrieval url",
                        SetMACRetrievalUrl,
                        () => string.IsNullOrWhiteSpace(eab.MACRetrievalUrl) ? Status.NeedsAttention : Status.AllGood),
                    new CLIAction('S', "Set success signal url", SetSuccessSignalUrl),
                    new CLIAction('F', "Set failure signal url", SetFailureSignalUrl)
                ]);
            }
            else
            {
                result.Add(
                    new CLIAction('E', "Enable external account binding", ToggleEAB)
                );
            }

            result.Add(new CLIAction('B', "Back to Main Menu", ApplyChanges));

            return result;
        }
    }

    private void ApplyChanges()
    {
        _options.ExternalAccountBinding = _eabOptions;
        Parent.PopScreen();
    }

    private void SetFailureSignalUrl()
    {
        var url = CLIPrompt.String("Type the url where a binding can be signaled as failure. Use {kid} as placeholder for the KID.");
        _eabOptions?.FailedSignalUrl = url;
    }

    private void SetSuccessSignalUrl()
    {
        var url = CLIPrompt.String("Type the url where a binding can be signaled as success. Use {kid} as placeholder for the KID.");
        _eabOptions?.SuccessSignalUrl = url;
    }

    private void SetMACRetrievalUrl()
    {
        var url = CLIPrompt.String("Type the url where a mac can be retrieved. Use {kid} as placeholder for the KID.");
        _eabOptions?.MACRetrievalUrl = url;
    }

    private void ToggleEABRequirement()
    {
        _eabOptions?.Required = !_eabOptions.Required;
    }

    private void ToggleEAB()
    {
        if (_eabOptions is null)
        {
            _eabOptions = new() { MACRetrievalUrl = "" };
        }
        else
        {
            _eabOptions = null;
        }
    }

    protected override List<ConfigInfo> GetConfigInfo() {
        List<ConfigInfo> subInfos = [];

        var eab = _options.ExternalAccountBinding;

        if (eab is not null)
        {
            subInfos.AddRange([
                new("Required", eab.Required ? "yes" : "no", Status.None),
                new("MAC retrieval url", eab.MACRetrievalUrl ?? "n/a", string.IsNullOrWhiteSpace(eab.MACRetrievalUrl) ? Status.NeedsAttention : Status.AllGood),
                new("Success signal url", eab.SuccessSignalUrl ?? "n/a", Status.None),
                new("Failure signal url", eab.FailedSignalUrl ?? "n/a", Status.None),
            ]);
        }

        var eabInfo = new ConfigInfo(
            "External account binding",
            eab is not null ? "enabled" : "disabled",
            eab is not null && string.IsNullOrWhiteSpace(eab.MACRetrievalUrl) ? Status.NeedsAttention : Status.AllGood
        )
        {
            SubInfo = subInfos
        };

        return [eabInfo];
    }
}
