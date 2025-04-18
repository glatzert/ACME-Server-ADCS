namespace Th11s.ACMEServer.HttpModel;

/// <summary>
/// Defines an identifier as used in orders or authorizations
/// </summary>
public class Identifier
{
    public Identifier(Model.Identifier model)
    {
        ArgumentNullException.ThrowIfNull(model);

        Type = model.Type;
        Value = model.Value;
    }

    public string Type { get; }
    public string Value { get; }
}
