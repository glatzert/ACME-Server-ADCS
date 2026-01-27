using System.Diagnostics;
using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.Model;

[DebuggerDisplay("Detail = {Detail}")]
public class AcmeError
{
    private string? _type;
    private string? _detail;

    public AcmeError(string type, string detail, Identifier? identifier = null, IEnumerable<AcmeError>? subErrors = null)
    {
        Type = type;

        if (!type.Contains(':'))
            Type = "urn:ietf:params:acme:error:" + type;

        Detail = detail;
        Identifier = identifier;
        SubErrors = subErrors?.ToList();
    }

    public string Type
    {
        get => _type ?? throw new NotInitializedException();
        private set => _type = value;
    }

    public string Detail
    {
        get => _detail ?? throw new NotInitializedException();
        set => _detail = value;
    }

    public Dictionary<string, object> AdditionalFields { get; } = [];

    // Move to additional fields
    public Identifier? Identifier { get; }

    public List<AcmeError>? SubErrors { get; }

    // Move to additional fields
    public int? HttpStatusCode { get; init; }


    // --- Serialization Methods --- //


    public AcmeErrorException AsException()
        => new(this);
}