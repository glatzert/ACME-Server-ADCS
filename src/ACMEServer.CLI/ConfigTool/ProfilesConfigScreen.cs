using Th11s.ACMEServer.CLI;
using Th11s.ACMEServer.CLI.ConfigTool;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.CLI.ConfigTool;

internal class ProfilesConfigScreen(ConfigCLI parent, List<ProfileConfiguration> profiles) : CLIScreen(parent)
{
    private readonly List<ProfileConfiguration> _profiles = profiles;
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
                result.Add(new(
                    '+',
                    "Add new profile",
                    AddProfile,
                    () => _profiles.Count > 0 ? Status.None : Status.NeedsAttention
                ));

                
                for (int i = 0; i < _profiles.Count; i++)
                {
                    int idx = i; // capture for lambda
                    result.Add(
                        new CLIAction(
                            (char)('1' + i),
                            $"Edit profile '{_profiles[i].Name}'",
                            () => _currentProfile = _profiles[idx]
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
                        () => _currentProfile.CertificateServices?.Status ?? Status.NeedsAttention
                    ),

                    new (
                        'S',
                        "Select supported identifiers",
                        SelectIdentifiers,
                        () => _currentProfile.SupportedIdentifiers.Length > 0 ? Status.AllGood : Status.NeedsAttention
                    ),

                    // TODO: Implement challenge types configuration
                    //new (
                    //    'C',
                    //    "Set allowed challenge-types",
                    //    SelectChallengeTypes,
                    //    () => _currentProfile.AllowedChallengeTypes.Length > 0 ? Status.AllGood : Status.NeedsAttention
                    //),

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

        var newProfileConfiguration = new ProfileConfiguration()
        {
            Name = newProfileName,
            CertificateServices = [
                new (){
                    CAServer = "",
                    TemplateName = ""
                }
            ],

            SupportedIdentifiers = ["dns"],
        };

        _profiles.Add(newProfileConfiguration);
        _currentProfile = newProfileConfiguration;
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
            _currentProfile.CertificateServices![0].CAServer = caconfig;
            _currentProfile.CertificateServices![0].TemplateName = template;
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
                    $"{_currentProfile.CertificateServices![0].CAServer} - {_currentProfile.CertificateServices![0].TemplateName}",
                    _currentProfile.CertificateServices[0].Status
                ),
                new(
                    "Supported Identifiers",
                    _currentProfile.SupportedIdentifiers.JoinOr(),
                    _currentProfile.SupportedIdentifiers.Length > 0 ? Status.AllGood : Status.NeedsAttention
                ),
                new ConfigInfo(
                    "Allowed Challenge Types",
                    _currentProfile.AllowedChallengeTypes.Select(ct => $"{ct.Key}: {ct.Value.JoinOr()}").JoinOr("\n"),
                    Status.None
                )
            ];
        }
    }
}
