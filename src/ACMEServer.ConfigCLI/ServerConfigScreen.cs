using Th11s.ACMEServer.Configuration;

namespace Th11s.ACMEServer.ConfigCLI;

internal class ServerConfigScreen(ConfigCLI parent, ACMEServerOptions options) : CLIScreen(parent)
{
    private readonly ACMEServerOptions _options = options;

    protected override string? ScreenTitle => "Server Configuration";
    protected override string? ScreenDescription => "Configure the server settings below.";

    protected override List<CLIAction> Actions =>
    [
        // Define actions for server configuration here
        new CLIAction('C', "Modify CAA Identities", ModifyCAAIdentities,
            () => _options.CAAStatus
        ),

        new CLIAction('R', "Toggle revokation support", ToggleRevokationSupport),

        new CLIAction('W', "Set Website Url", ModifyWebsiteUrl),
        new CLIAction('T', "Terms of service", ModifyTOS),

        new CLIAction('E', "Configure external account binding",
            () => Parent.PushScreen(new EABConfigScreen(Parent, _options))),

        new CLIAction('B', "Back to Main Menu", Parent.PopScreen)
    ];

    private void ToggleRevokationSupport()
    {
        _options.SupportsRevokation = !_options.SupportsRevokation;
    }

    private void ModifyTOS()
    {
        var requireAgreement = CLIPrompt.Bool("Require agreement to terms of service?");
        if (requireAgreement)
        {
            var tosUrl = CLIPrompt.String("Enter the URL for the terms of service");
            var lastUpdateInput = CLIPrompt.String("Enter the last update date (yyyy-MM-dd) or leave blank for none");
            DateTime? lastUpdate = null;
            if (DateTime.TryParse(lastUpdateInput, out var parsedDate))
            {
                lastUpdate = parsedDate;
            }

            var tosOptions = new TermsOfServiceOptions
            {
                RequireAgreement = true,
                Url = tosUrl,
                LastUpdate = lastUpdate ?? DateTime.UtcNow,
            };

            _options.TOS = tosOptions;

        }
        else
        {
            _options.TOS = new() { RequireAgreement = false };
        }
    }

    private void ModifyWebsiteUrl()
    {
        var url = CLIPrompt.String("Enter the website URL for the ACME server information page (leave blank to unset)");
        _options.WebsiteUrl = url.TrimOrNull();
    }

    private void ModifyCAAIdentities()
    {
        var caaIdentities = CLIPrompt.StringList("Enter CAA identities", [.. _options.CAAIdentities]);
        _options.CAAIdentities = [.. caaIdentities];
    }

    protected override List<ConfigInfo> GetConfigInfo()
    => [
        new(
            "CAA Identities",
            $"{Environment.NewLine}{_options.CAAIdentities.JoinOr()}",
            _options.CAAStatus
        ),
        new(
            "Revokation supported",
            _options.SupportsRevokation ? "enabled" : "disabled",
            Status.None
        ),
        new(
            "Terms of service",
            _options.TOS.RequireAgreement
                ? $"required ({_options.TOS.Url ?? "no url"}, {_options.TOS.LastUpdate?.ToString() ?? "no date"})"
                : "not required",
            Status.None
        ),
        new(
            "External account binding",
            _options.ExternalAccountBinding is null ? "not configured" : "configured",
            Status.None
        )
    ];
}
