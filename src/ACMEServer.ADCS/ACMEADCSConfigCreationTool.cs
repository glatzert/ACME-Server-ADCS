namespace Th11s.ACMEServer.ADCS;

internal class ACMEADCSConfigCreationTool
{
    public Menu RootMenu { get; } = new()
    {
        Description = "",
        
        SubMenus = [
            
        ],

        Choice = [

        ]
    };

    public ACMEADCSConfigCreationTool()
    {
    }

    internal async Task RunAsync()
    {
        


        return;
    }
}

internal class Menu : IChoice
{
    public required string Description { get; init; }

    public List<Menu>? SubMenus { get; init; }
    public List<Choice>? Choice { get; init; }


    public IChoice Prompt()
    {
        
    }
}

public class Choice : IChoice
{
    public required string Description { get; init; }
}

public interface IChoice
{
    public string Description { get; }
}