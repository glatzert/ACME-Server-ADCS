using Th11s.ACMEServer.CLI;
using Th11s.ACMEServer.CLI.ConfigTool;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.CLI.ConfigTool;

internal class ProfilesConfigScreen(ConfigCLI parent, ConfigRoot.ProfileOptions options) : CLIScreen(parent)
{
    private readonly ConfigRoot.ProfileOptions _options = options;
    private ProfileConfiguration? _currentProfile;

    protected override string? ScreenTitle => "Profiles Configuration";
    protected override string? ScreenDescription => "Configure the profiles below.";
    protected override List<CLIAction> Actions
    {
        get
        {
            List<CLIAction> result = [];

            if (_currentProfile is null)
            {
                var profileList = _options.Items.Values.ToList();

                result.Add(new(
                    '+',
                    "Add new profile",
                    AddProfile,
                    () => profileList.Count > 0 ? Status.None : Status.NeedsAttention
                ));

                
                for (int i = 0; i < profileList.Count; i++)
                {
                    int idx = i; // capture for lambda
                    result.Add(
                        new CLIAction(
                            (char)('1' + i),
                            $"Edit profile '{profileList[i].Name}'",
                            () => _currentProfile = profileList[idx]
                        )
                    );
                }

                result.Add(
                    new('B', "Back to Main Menu", Parent.PopScreen)
                );
            }
            else
            {
                result.AddRange([
                    new (
                        'A',
                        "Select ADCS server and template",
                        ModifiyADCSOptions,
                        () => _currentProfile.ADCSOptions.Status
                    ),

                    new (
                        'S',
                        "Select supported identifiers",
                        SelectIdentifiers,
                        () => _currentProfile.SupportedIdentifiers.Length > 0 ? Status.AllGood : Status.NeedsAttention
                    ),

                    new (
                        'X',
                        "Remove current profile",
                        RemoveProfile
                    )
                ]);

                result.Add(
                    new('B', "Back to Profile selection", () => _currentProfile = null)
                );
            }

            return result;
        }
    }

    private void RemoveProfile()
    {
        throw new NotImplementedException();
    }

    private void AddProfile()
    {
        var newProfileName = CLIPrompt.String("Set profile name");
        if(string.IsNullOrWhiteSpace(newProfileName))
        {
            return;
        }

        _options.Items.Add(
            newProfileName,
            new()
            {
                ADCSOptions = new()
                {
                    CAServer = "",
                    TemplateName = ""
                },

                SupportedIdentifiers = ["dns"],
            }
        );

        _currentProfile = _options.Items[newProfileName];
    }

    private void SelectIdentifiers()
    {
        if(_currentProfile is null)
        {
            return;
        }

        var supportedIdentifiers = CLIPrompt.MultiSelect(
            "Choose supported identifiers",
            [IdentifierTypes.DNS, IdentifierTypes.IP, IdentifierTypes.PermanentIdentifier, IdentifierTypes.HardwareModule],
            i => i
        );

        _currentProfile.SupportedIdentifiers = [.. supportedIdentifiers];
    }

    private void ModifiyADCSOptions()
    {
        if (_currentProfile is null)
        {
            return;
        }

        var (caconfig, template) = CLIPrompt.PromptCAConfigAndTemplate();
        if (!string.IsNullOrWhiteSpace(caconfig) && !string.IsNullOrWhiteSpace(template))
        {
            _currentProfile.ADCSOptions.CAServer = caconfig;
            _currentProfile.ADCSOptions.TemplateName = template;
            return;
        }
    }

    protected override List<ConfigInfo> GetConfigInfo()
    {
        if (_currentProfile is null)
        {
            return [];
        }
        else
        {
            return [
                new(
                    "ADCS Configuration",
                    $"{_currentProfile.ADCSOptions.CAServer} - {_currentProfile.ADCSOptions.TemplateName}",
                    _currentProfile.ADCSOptions.Status
                ),
                new(
                    "Supported Identifiers",
                    _currentProfile.SupportedIdentifiers.JoinOr(),
                    _currentProfile.SupportedIdentifiers.Length > 0 ? Status.AllGood : Status.NeedsAttention
                )
            ];
        }
    }
}
