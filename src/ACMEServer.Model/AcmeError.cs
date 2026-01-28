using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.Model;

[DebuggerDisplay("Detail = {Detail}")]
public class AcmeError
{
    private string? _type;
    private string? _detail;

    public AcmeError(string type, string detail, IEnumerable<AcmeError>? subErrors = null)
    {
        Type = type;

        if (!type.Contains(':'))
            Type = "urn:ietf:params:acme:error:" + type;

        Detail = detail;
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
    public List<AcmeError>? SubErrors { get; }

    public Dictionary<string, object> AdditionalFields { get; } = [];

    [DisallowNull]
    public Identifier? Identifier { 
        get => AdditionalFields.TryGetValue(nameof(Identifier), out var value) && value is Identifier identifier
            ? identifier
            : null;

        set => AdditionalFields[nameof(Identifier)] = value;
    }

    [DisallowNull]
    public int? HttpStatusCode { 
        get => AdditionalFields.TryGetValue(nameof(HttpStatusCode), out var value) && value is int httpStatusCode
            ? httpStatusCode
            : null;

        set
        {
            if (value.HasValue)
            {
                AdditionalFields[nameof(HttpStatusCode)] = value;
            }
        }
    }

    public AcmeErrorException AsException()
        => new(this);
}