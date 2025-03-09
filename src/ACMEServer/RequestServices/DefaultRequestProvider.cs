using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.HttpModel.Services;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.RequestServices
{
    public class DefaultRequestProvider : IAcmeRequestProvider
    {
        private AcmeJwsToken? _request;
        private AcmeJwsHeader? _header;

        private Type? _payloadType;
        private object? _payload;


        public void Initialize(AcmeJwsToken rawPostRequest)
        {
            if (rawPostRequest is null)
                throw new ArgumentNullException(nameof(rawPostRequest));

            _request = rawPostRequest;
            _header = ReadHeader(_request);
        }

        public AcmeJwsHeader GetHeader()
        {
            if (_request is null || _header is null)
                throw new NotInitializedException();

            return _header;
        }

        public TPayload GetPayload<TPayload>()
            where TPayload : new()
        {
            if (_request is null)
                throw new NotInitializedException();

            if (_payload != null)
            {
                if (_payloadType != typeof(TPayload))
                    throw new InvalidOperationException("Cannot change types during request");

                return (TPayload)_payload;
            }

            _payloadType = typeof(TPayload);

            var payload = ReadPayload<TPayload>(_request);
            _payload = payload;

            return payload;
        }

        public AcmeJwsToken GetRequest()
        {
            if (_request is null)
                throw new NotInitializedException();

            return _request;
        }


        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private static AcmeJwsHeader ReadHeader(AcmeJwsToken rawRequest)
        {
            if (rawRequest is null)
                throw new ArgumentNullException(nameof(rawRequest));

            return rawRequest.AcmeHeader;
        }

        private static TPayload ReadPayload<TPayload>(AcmeJwsToken rawRequest)
            where TPayload : new()
        {
            if (rawRequest?.Payload is null)
                throw new ArgumentNullException(nameof(rawRequest));

            if (rawRequest.AcmePayload is null)
                return new();

            return rawRequest.AcmePayload.Deserialize<TPayload>(_jsonOptions);
        }
    }
}
