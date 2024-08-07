﻿using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.HttpModel.Services;
using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.RequestServices
{
    public class DefaultRequestProvider : IAcmeRequestProvider
    {
        private AcmeRawPostRequest? _request;
        private AcmeHeader? _header;

        private Type? _payloadType;
        private object? _payload;


        public void Initialize(AcmeRawPostRequest rawPostRequest)
        {
            if (rawPostRequest is null)
                throw new ArgumentNullException(nameof(rawPostRequest));

            _request = rawPostRequest;
            _header = ReadHeader(_request);
        }

        public AcmeHeader GetHeader()
        {
            if (_request is null || _header is null)
                throw new NotInitializedException();

            return _header;
        }

        public T GetPayload<T>()
        {
            if (_request is null)
                throw new NotInitializedException();

            if (_payload != null)
            {
                if (_payloadType != typeof(T))
                    throw new InvalidOperationException("Cannot change types during request");

                return (T)_payload;
            }

            _payloadType = typeof(T);

            var payload = ReadPayload<T>(_request);
            _payload = payload;

            return payload;
        }

        public AcmeRawPostRequest GetRequest()
        {
            if (_request is null)
                throw new NotInitializedException();

            return _request;
        }


        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private static AcmeHeader ReadHeader(AcmeRawPostRequest rawRequest)
        {
            if (rawRequest is null)
                throw new ArgumentNullException(nameof(rawRequest));

            var headerJson = Base64UrlEncoder.Decode(rawRequest.Header);
            var header = JsonSerializer.Deserialize<AcmeHeader>(headerJson, _jsonOptions);

            return header;
        }

        private static TPayload ReadPayload<TPayload>(AcmeRawPostRequest rawRequest)
        {
            if (rawRequest is null)
                throw new ArgumentNullException(nameof(rawRequest));

            var payloadJson = Base64UrlEncoder.Decode(rawRequest.Payload);
            var payload = JsonSerializer.Deserialize<TPayload>(payloadJson, _jsonOptions);

            return payload;
        }
    }
}
