﻿using System.Text.Json.Serialization;
using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.HttpModel.Requests.JWS
{
    public class EncodedJsonWebSignature
    {
        private string? _header;
        private string? _signature;

        [JsonPropertyName("protected")]
        public string Header
        {
            get => _header ?? throw new NotInitializedException();
            set => _header = value;
        }

        [JsonPropertyName("payload")]
        public string? Payload { get; set; }

        [JsonPropertyName("signature")]
        public string Signature
        {
            get => _signature ?? throw new NotInitializedException();
            set => _signature = value;
        }
    }
}
