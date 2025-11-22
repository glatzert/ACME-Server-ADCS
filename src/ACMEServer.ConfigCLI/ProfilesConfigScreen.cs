using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.ConfigCLI;

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

        try
        {
            var templates = ActiveDirectoryUtility.GetEnrollmentServiceCollection()
                .SelectMany(ca => ca.CertificateTemplates.Select(t => (CA: ca, Template: t)))
                .ToList();

            var selection = CLIPrompt.Select("Choose CA and Template", templates, t => $"{t.CA.Name} - {t.Template}");
            if (selection.CA is null)
            {
                return;
            }

            _currentProfile.ADCSOptions.CAServer = selection.CA.ConfigurationString;
            _currentProfile.ADCSOptions.TemplateName = selection.Template;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving CA information: {ex.Message}");

            _currentProfile.ADCSOptions.CAServer = CLIPrompt.String("Enter CA Server (e.g., 'server\\CAName')");
            _currentProfile.ADCSOptions.TemplateName = CLIPrompt.String("Enter Template Name");
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
