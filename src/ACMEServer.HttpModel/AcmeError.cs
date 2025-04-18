﻿using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.HttpModel;

/// <summary>
/// Represents an error object for ACME operations.
/// https://tools.ietf.org/html/rfc8555#section-6.7
/// </summary>
public class AcmeError
{
    public AcmeError(Model.AcmeError model)
    {
        ArgumentNullException.ThrowIfNull(model);

        Type = model.Type;
        Detail = model.Detail;

        if (model.Identifier != null)
        {
            Identifier = new Identifier(model.Identifier);
        }

        Subproblems = model.SubErrors?
            .Select(x => new AcmeError(x))
            .ToList();
    }

    public AcmeError(AcmeException ex)
        : this($"{ex.UrnBase}:{ex.ErrorType}", ex.Message)
    { }

    public AcmeError(string type, string detail)
    {
        Type = type;
        Detail = detail;
    }

    public string Type { get; set; }
    public string Detail { get; set; }

    public List<AcmeError>? Subproblems { get; set; }
    public Identifier? Identifier { get; set; }
}
