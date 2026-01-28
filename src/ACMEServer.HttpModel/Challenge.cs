using System.Globalization;

namespace Th11s.ACMEServer.HttpModel;

/// <summary>
/// Represents an ACME challenge
/// https://tools.ietf.org/html/rfc8555#section-7.1.5
/// </summary>
public class Challenge
{
    public static Challenge FromModel(Model.Challenge model, string challengeUrl)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(challengeUrl);

        return model switch
        {
            Model.TokenChallenge tokenChallenge => new TokenChallenge()
                {
                    Type = tokenChallenge.Type,
                    Token = tokenChallenge.Token,

                    Status = EnumMappings.GetEnumString(tokenChallenge.Status),
                    Url = challengeUrl,

                    Validated = tokenChallenge.Validated?.ToString("o", CultureInfo.InvariantCulture),
                    Error = tokenChallenge.Error != null ? new AcmeError(tokenChallenge.Error) : null,
                },

            _ => throw new NotSupportedException($"Challenge type '{model.GetType().FullName}' is not supported.")
        };
    }


    public required string Type { get; init; }

    public required string Status { get; init; }

    public string? Validated { get; init; }
    public AcmeError? Error { get; init; }

    public required string Url { get; init; }
}

public class TokenChallenge : Challenge
{   
    public required string Token { get; init; }
}
