﻿using Microsoft.IdentityModel.Tokens;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Th11s.ACMEServer.Model.Extensions;

namespace Th11s.ACMEServer.Model.JWS;

[Serializable]
public class AcmeJwsToken : ISerializable
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };


    [JsonPropertyName("protected")]
    public string Protected { get; }

    [JsonPropertyName("payload")]
    public string? Payload { get; }

    [JsonPropertyName("signature")]
    public string Signature { get; }


    [JsonIgnore]
    public AcmeJwsHeader AcmeHeader { get; }

    [JsonIgnore]
    public JsonDocument? AcmePayload { get; }

    [JsonIgnore]
    public byte[] SignatureBytes { get; }


    [JsonConstructor]
    public AcmeJwsToken(string @protected, string? payload, string signature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(@protected, nameof(@protected));
        ArgumentException.ThrowIfNullOrWhiteSpace(signature, nameof(signature));

        Protected = @protected;
        Payload = payload;
        Signature = signature;


        AcmeHeader = JsonSerializer.Deserialize<AcmeJwsHeader>(Base64UrlEncoder.Decode(Protected), _jsonOptions)
            ?? throw new InvalidOperationException("Header is null");

        AcmePayload = !string.IsNullOrWhiteSpace(Payload)
            ? JsonDocument.Parse(Base64UrlEncoder.Decode(Payload))
            : null;

        SignatureBytes = Base64UrlEncoder.DecodeBytes(Signature);
    }


    // --- Serialization Methods --- //

    protected AcmeJwsToken(SerializationInfo info, StreamingContext streamingContext)
    {
        if (info is null)
            throw new ArgumentNullException(nameof(info));

        Protected = info.GetRequiredString(nameof(Protected));
        Payload = info.GetString(nameof(Payload));
        Signature = info.GetRequiredString(nameof(Signature));


        AcmeHeader = JsonSerializer.Deserialize<AcmeJwsHeader>(Base64UrlEncoder.Decode(Protected), _jsonOptions)
            ?? throw new InvalidOperationException("Header is null");

        AcmePayload = !string.IsNullOrWhiteSpace(Payload)
            ? JsonDocument.Parse(Base64UrlEncoder.Decode(Payload))
            : null;

        SignatureBytes = Base64UrlEncoder.DecodeBytes(Signature);
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info is null)
            throw new ArgumentNullException(nameof(info));

        info.AddValue("SerializationVersion", 1);

        info.AddValue(nameof(Protected), Protected);
        info.AddValue(nameof(Payload), Payload);
        info.AddValue(nameof(Signature), Signature);
    }
}
