﻿using System.Text.Json.Serialization;
using Th11s.ACMEServer.HttpModel.Converters;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.HttpModel.Requests.JWS;

public class JOSEHeader
{
    public string? Nonce { get; set; }
    public string? Url { get; set; }

    public string? Alg { get; set; }
    public string? Kid { get; set; }

    [JsonConverter(typeof(JwkConverter))]
    public Jwk? Jwk { get; set; }
}
