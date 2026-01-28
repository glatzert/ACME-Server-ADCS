using System.Text.Json;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;
using static ACMEServer.Storage.FileSystem.FileSystemConstants;

namespace ACMEServer.Storage.FileSystem.Serialization;

internal static class OrderJsonReader
{
    extension(ref Utf8JsonReader reader)
    {
        public Order GetOrder()
        {
            var serializationVersion = reader.PeekSerializationVersion();
            
            return serializationVersion switch
            {
                2 => reader.GetOrderV2([]),
                3 => reader.GetOrderV3(),
                _ => throw new JsonException($"Unsupported Order serialization version: {serializationVersion}"),
            };
        }

        public Order GetOrderV2(Dictionary<string, object> references)
        {
            string? refIndex = null;

            OrderId? orderId = null;
            AccountId? accountId = null;

            OrderStatus? status = null;

            List<Identifier>? identifiers = null;
            List<Authorization>? authorizations = null;

            DateTimeOffset? notBefore = null;
            DateTimeOffset? notAfter = null;
            DateTimeOffset? expires = null;

            ProfileName? profile = null;

            string? certificateSigningRequest = null;
            CertificateId? certificateId = null;

            string? expectedPublicKey = null;

            AcmeError? error = null;
            long? version = null;


            bool isFirstToken = true;
            while (reader.Read())
            {
                if (reader.TokenIsObjectStart && isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }

                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case "$id":
                        reader.Read();
                        refIndex = reader.GetString();
                        break;

                    case nameof(Order.OrderId):
                        reader.Read();
                        orderId = reader.GetOrderId();
                        break;

                    case nameof(Order.AccountId):
                        reader.Read();
                        accountId = reader.GetAccountId();
                        break;

                    case nameof(Order.Status):
                        reader.Read();
                        status = reader.GetEnumFromString<OrderStatus>();
                        break;

                    case nameof(Order.Identifiers):
                        var identifierWithMetadata = reader.GetWrappedList(
                                (ref reader, references) => reader.GetIdentifierV2(references),
                                references
                            ) ?? [];

                        identifiers = [.. identifierWithMetadata.Select(iwm => iwm.Identifier)];
                        expectedPublicKey = identifierWithMetadata
                            .Select(iwm => iwm.ExpectedPublicKey)
                            .SingleOrDefault(epk => !string.IsNullOrEmpty(epk));

                        break;

                    case nameof(Order.Authorizations):
                        authorizations = reader.GetWrappedList(
                                (ref reader, references) => reader.GetAuthorizationV2(references),
                                references
                            ) ?? [];
                        break;

                    case nameof(Order.NotBefore):
                        reader.Read();
                        notBefore = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(Order.NotAfter):
                        reader.Read();
                        notAfter = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(Order.Expires):
                        reader.Read();
                        expires = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(Order.Profile):
                        reader.Read();
                        profile = reader.GetProfileName();
                        break;

                    case nameof(Order.CertificateSigningRequest):
                        reader.Read();
                        certificateSigningRequest = reader.GetString();
                        break;

                    case nameof(Order.CertificateId):
                        reader.Read();
                        certificateId = reader.GetCertificateId();
                        break;

                    case nameof(Order.Error):
                        error = reader.GetAcmeErrorV2(references);
                        break;

                    case nameof(Order.Version):
                        reader.Read();
                        version = reader.GetInt64();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Order(
                orderId ?? throw new JsonException("Missing orderId."),
                accountId ?? throw new JsonException("Missing accountId."),
                status ?? throw new JsonException("Missing status."),
                identifiers ?? throw new JsonException("Missing identifiers."),
                authorizations ?? throw new JsonException("Missing authorizations."),
                notBefore,
                notAfter,
                expires,
                profile ?? ProfileName.None,
                certificateSigningRequest,
                certificateId,
                expectedPublicKey,
                error,
                version ?? 0);

            if (refIndex is not null)
            {
                references[refIndex] = result;
            }

            return result;
        }


        public (Identifier Identifier, string? ExpectedPublicKey) GetIdentifierV2(Dictionary<string, object> references)
        {
            string? refIndex = null;

            string? type = null;
            string? value = null;
            Dictionary<string, string> metadata = [];

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (reader.TokenIsObjectStart && isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }

                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case "$ref":
                        reader.Read();
                        var referencedIndex = reader.GetString()!;
                        reader.Read();
                        return ((Identifier)references[referencedIndex]!, null);

                    case "$id":
                        reader.Read();
                        refIndex = reader.GetString();
                        break;

                    case nameof(Identifier.Value):
                        reader.Read();
                        value = reader.GetString();
                        break;

                    case nameof(Identifier.Type):
                        reader.Read();
                        type = reader.GetString();
                        break;

                    case "Metadata":
                        metadata = reader.GetMetadataV2(references);
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Identifier(
                type ?? throw new JsonException("Expected string value for Identifier type"),
                value ?? throw new JsonException("Expected string value for Identifier value")
            );

            if (refIndex is not null)
            {
                references[refIndex] = result;
            }

            var expectedPublicKey = metadata?.GetValueOrDefault("expected-public-key");
            return (result, expectedPublicKey);
        }

        public Authorization GetAuthorizationV2(Dictionary<string, object> references)
        {
            string? refIndex = null;

            AuthorizationId? authorizationId = null;
            AuthorizationStatus status = default;

            Identifier? identifier = null;
            bool isWildcard = false;
            DateTimeOffset? expires = default;

            List<Challenge> challenges = [];

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (reader.TokenIsObjectStart && isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }

                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case "$id":
                        reader.Read();
                        refIndex = reader.GetString();
                        break;

                    case nameof(Authorization.AuthorizationId):
                        reader.Read();
                        authorizationId = reader.GetAuthorizationId();
                        break;

                    case nameof(Authorization.Status):
                        reader.Read();
                        status = reader.GetEnumFromString<AuthorizationStatus>();
                        break;

                    case nameof(Authorization.Identifier):
                        identifier = reader.GetIdentifierV2(references).Identifier;
                        break;

                    case nameof(Authorization.IsWildcard):
                        reader.Read();
                        isWildcard = reader.GetBoolean();
                        break;

                    case nameof(Authorization.Expires):
                        reader.Read();
                        expires = reader.GetDateTimeOffset();
                        break;

                    case nameof(Authorization.Challenges):
                        challenges =
                            reader.GetWrappedList<Challenge>(
                                (ref reader, references) => reader.GetChallengeV2(references),
                                references
                            ) ?? [];
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Authorization(
                authorizationId ?? throw new JsonException("Missing AuthorizationId."),
                status,
                identifier ?? throw new JsonException("Missing Identifier."),
                isWildcard,
                expires ?? throw new JsonException("Missing Expires."),
                challenges);

            if (refIndex is not null)
            {
                references[refIndex] = result;
            }

            return result;
        }

        public TokenChallenge GetChallengeV2(Dictionary<string, object> references)
        {
            string? refIndex = null;

            ChallengeId? challengeId = null;
            ChallengeStatus status = default;
            string? type = null;
            string? token = null;
            string? payload = null;
            DateTimeOffset? validated = null;
            AcmeError? error = null;

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (reader.TokenIsObjectStart && isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }

                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case "$id":
                        reader.Read();
                        refIndex = reader.GetString();
                        break;

                    case nameof(TokenChallenge.ChallengeId):
                        reader.Read();
                        challengeId = reader.GetChallengeId();
                        break;

                    case nameof(TokenChallenge.Status):
                        reader.Read();
                        status = reader.GetEnumFromString<ChallengeStatus>();
                        break;

                    case nameof(TokenChallenge.Type):
                        reader.Read();
                        type = reader.GetString();
                        break;

                    case nameof(TokenChallenge.Token):
                        reader.Read();
                        token = reader.GetString();
                        break;

                    case nameof(DeviceAttestChallenge.Payload):
                        reader.Read();
                        payload = reader.GetString();
                        break;

                    case nameof(TokenChallenge.Validated):
                        reader.Read();
                        validated = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(TokenChallenge.Error):
                        error = reader.GetAcmeErrorV2(references);
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = type == ChallengeTypes.DeviceAttest01
                ? new DeviceAttestChallenge(
                    challengeId ?? throw new JsonException("Missing ChallengeId."),
                    status,
                    type ?? throw new JsonException("Missing Type."),
                    token ?? throw new JsonException("Missing Token."),
                    payload,
                    validated,
                    error)
                : new TokenChallenge(
                    challengeId ?? throw new JsonException("Missing ChallengeId."),
                    status,
                    type ?? throw new JsonException("Missing Type."),
                    token ?? throw new JsonException("Missing Token."),
                    validated,
                    error);

            if (refIndex is not null)
            {
                references[refIndex] = result;
            }

            return result;
        }


        public Dictionary<string, string>? GetMetadataV2(Dictionary<string, object> references)
        {
            string? refIndex = null;
            var result = new Dictionary<string, string>();

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return null;
                }

                if (reader.TokenIsObjectStart && isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }

                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case "$id":
                        reader.Read();
                        refIndex = reader.GetString();
                        break;

                    default:
                        reader.Read();
                        string value = reader.GetString()!;

                        if (!string.IsNullOrEmpty(value))
                        {
                            result.Add(propertyName, value);
                        }
                        break;
                }
            }

            if (refIndex is not null)
            {
                references[refIndex] = result;
            }

            return result;
        }


        public AcmeError? GetAcmeErrorV2(Dictionary<string, object> references)
        {
            string? refIndex = null;

            string? type = null;
            string? detail = null;

            Identifier? identifier = null;
            List<AcmeError>? subErrors = null;
            int? httpStatusCode = null;

            Dictionary<string, object> additionalFields = [];

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return null;
                }

                if (reader.TokenIsObjectStart && isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }

                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case "$id":
                        reader.Read();
                        refIndex = reader.GetString();
                        break;

                    case nameof(AcmeError.Type):
                        reader.Read();
                        type = reader.GetString();
                        break;

                    case nameof(AcmeError.Detail):
                        reader.Read();
                        detail = reader.GetString();
                        break;

                    case nameof(AcmeError.Identifier):
                        var peekReader = reader;
                        peekReader.Read();
                        if(peekReader.TokenType == JsonTokenType.Null)
                        {
                            reader.Read();
                            identifier = null;
                            break;
                        }

                        identifier = reader.GetIdentifierV2(references).Identifier;
                        break;

                    case nameof(AcmeError.SubErrors):
                        subErrors = reader.GetWrappedList(
                            (ref reader, references) => reader.GetAcmeErrorV2(references)!,
                            references);
                        break;

                    case nameof(AcmeError.HttpStatusCode):
                        reader.Read();
                        httpStatusCode = reader.GetInt32();
                        break;

                    case nameof(AcmeError.AdditionalFields):
                        additionalFields = reader.GetAdditionalErrorFieldsV2();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new AcmeError(
                type ?? throw new JsonException("Missing required property: type"),
                detail ?? throw new JsonException("Missing required property: detail"),
                subErrors
            )
            {
                HttpStatusCode = httpStatusCode,
            };

            if (identifier is not null)
            {
                result.Identifier = identifier;
            }

            foreach(var (key, value) in additionalFields)
            {
                result.AdditionalFields[key] = value;
            }

            if (refIndex is not null)
            {
                references[refIndex] = result;
            }

            return result;
        }

        public Dictionary<string, object> GetAdditionalErrorFieldsV2()
        {
            var result = new Dictionary<string, object>();

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return result;
                }

                if (reader.TokenIsObjectStart && isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }

                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case "algorithms":
                        var algorithms = reader.GetWrappedList(
                            (ref reader, references) => reader.GetString()!,
                            []
                        ) ?? [];
                        result[propertyName] = algorithms;
                        break;

                    default:
                        reader.Skip();
                        break;
                }

            }

            return result;
        }



        public Order GetOrderV3()
        {
            OrderId? orderId = null;
            AccountId? accountId = null;

            OrderStatus? status = null;

            List<Identifier>? identifiers = null;
            List<Authorization>? authorizations = null;

            DateTimeOffset? notBefore = null;
            DateTimeOffset? notAfter = null;
            DateTimeOffset? expires = null;

            ProfileName? profile = null;

            string? certificateSigningRequest = null;
            CertificateId? certificateId = null;

            string? expectedPublicKey = null;

            AcmeError? error = null;
            long? version = null;


            reader.Read();
            reader.AssumeTokenIsObjectStart();
            
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                
                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case SerializationVersionPropertyName:
                        reader.Read();
                        var serializationVersion = reader.GetInt32();
                        if (serializationVersion != 3)
                        {
                            throw new JsonException($"Unexpected serialization version {serializationVersion} when deserializing Order V3.");
                        }
                        break;

                    case nameof(Order.OrderId):
                        reader.Read();
                        orderId = reader.GetOrderId();
                        break;

                    case nameof(Order.AccountId):
                        reader.Read();
                        accountId = reader.GetAccountId();
                        break;

                    case nameof(Order.Status):
                        reader.Read();
                        status = reader.GetEnumFromString<OrderStatus>();
                        break;

                    case nameof(Order.Identifiers):
                        identifiers = reader.GetList((ref reader) => reader.GetIdentifierV3());
                        break;

                    case nameof(Order.Authorizations):
                        authorizations = reader.GetList((ref reader) => reader.GetAuthorizationV3());
                        break;

                    case nameof(Order.NotBefore):
                        reader.Read();
                        notBefore = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(Order.NotAfter):
                        reader.Read();
                        notAfter = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(Order.Expires):
                        reader.Read();
                        expires = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(Order.Profile):
                        reader.Read();
                        profile = reader.GetProfileName();
                        break;

                    case nameof(Order.CertificateSigningRequest):
                        reader.Read();
                        certificateSigningRequest = reader.GetString();
                        break;

                    case nameof(Order.CertificateId):
                        reader.Read();
                        certificateId = reader.GetCertificateId();
                        break;

                    case nameof(Order.ExpectedPublicKey):
                        reader.Read();
                        expectedPublicKey = reader.GetString();
                        break;

                    case nameof(Order.Error):
                        error = reader.GetAcmeErrorV3();
                        break;

                    case nameof(Order.Version):
                        reader.Read();
                        version = reader.GetInt64();
                        break;

                    default:
                        throw new JsonException($"Unexpected property when deserializing Order V3: {propertyName}");
                }
            }

            var result = new Order(
                orderId ?? throw new JsonException($"Missing required property: {nameof(Order.OrderId)}"),
                accountId ?? throw new JsonException($"Missing required property: {nameof(Order.AccountId)}"),
                status ?? throw new JsonException($"Missing required property: {nameof(Order.Status)}"),
                identifiers ?? throw new JsonException($"Missing required property: {nameof(Order.Identifiers)}"),
                authorizations ?? throw new JsonException($"Missing required property: {nameof(Order.Authorizations)}"),
                notBefore,
                notAfter,
                expires,
                profile ?? throw new JsonException($"Missing required property: {nameof(Order.Profile)}"),
                certificateSigningRequest,
                certificateId,
                expectedPublicKey,
                error,
                version ?? 0);

            return result;
        }

        public Identifier GetIdentifierV3()
        {
            string? type = null;
            string? value = null;
            Dictionary<string, string> metadata = [];


            reader.Read();
            reader.AssumeTokenIsObjectStart();

            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case SerializationVersionPropertyName:
                        reader.Read();
                        var serializationVersion = reader.GetInt32();
                        if (serializationVersion != 3)
                        {
                            throw new JsonException($"Unexpected serialization version {serializationVersion} when deserializing Order V3.");
                        }
                        break;

                    case nameof(Identifier.Value):
                        reader.Read();
                        value = reader.GetString();
                        break;

                    case nameof(Identifier.Type):
                        reader.Read();
                        type = reader.GetString();
                        break;

                    default:
                        throw new JsonException($"Unexpected property when deserializing Identifier V3: {propertyName}");
                }
            }

            var result = new Identifier(
                type ?? throw new JsonException($"Missing required property: {nameof(Identifier.Type)}"),
                value ?? throw new JsonException($"Missing required property: {nameof(Identifier.Value)}")
            );

            return result;
        }

        public Authorization GetAuthorizationV3()
        {
            AuthorizationId? authorizationId = null;
            AuthorizationStatus? status = null;

            Identifier? identifier = null;
            bool isWildcard = false;
            DateTimeOffset? expires = default;

            List<Challenge>? challenges = null;


            reader.Read();
            reader.AssumeTokenIsObjectStart();

            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case SerializationVersionPropertyName:
                        reader.Read();
                        var serializationVersion = reader.GetInt32();
                        if (serializationVersion != 3)
                        {
                            throw new JsonException($"Unexpected serialization version {serializationVersion} when deserializing Order V3.");
                        }
                        break;

                    case nameof(Authorization.AuthorizationId):
                        reader.Read();
                        authorizationId = reader.GetAuthorizationId();
                        break;

                    case nameof(Authorization.Status):
                        reader.Read();
                        status = reader.GetEnumFromString<AuthorizationStatus>();
                        break;

                    case nameof(Authorization.Identifier):
                        identifier = reader.GetIdentifierV3();
                        break;

                    case nameof(Authorization.IsWildcard):
                        reader.Read();
                        isWildcard = reader.GetBoolean();
                        break;

                    case nameof(Authorization.Expires):
                        reader.Read();
                        expires = reader.GetDateTimeOffset();
                        break;

                    case nameof(Authorization.Challenges):
                        challenges = reader.GetList((ref reader) => reader.GetChallengeV3());
                        break;

                    default:
                        throw new JsonException($"Unexpected property when deserializing Authorization V3: {propertyName}");
                }
            }

            var result = new Authorization(
                authorizationId ?? throw new JsonException($"Missing required property: {nameof(Authorization.AuthorizationId)}"),
                status ?? throw new JsonException($"Missing required property: {nameof(Authorization.Status)}"),
                identifier ?? throw new JsonException($"Missing required property: {nameof(Authorization.Identifier)}"),
                isWildcard,
                expires ?? throw new JsonException($"Missing required property: {nameof(Authorization.Expires)}"),
                challenges ?? throw new JsonException($"Missing required property: {nameof(Authorization.Challenges)}")
            );

            return result;
        }

        public Challenge GetChallengeV3()
        {
            var challengeType = reader.PeekChallengeType();
            switch (challengeType)
            {
                case ChallengeTypes.Http01:
                case ChallengeTypes.Dns01:
                case ChallengeTypes.TlsAlpn01:
                    return reader.GetTokenChallengeV3();

                case ChallengeTypes.DeviceAttest01:
                    return reader.GetDeviceAttestChallengeV3();

                case null:
                    throw new JsonException($"Missing required property: {nameof(Challenge.Type)}");

                default:
                    throw new JsonException($"Unsupported challenge type: {challengeType}");
            }
        }

        private string? PeekChallengeType()
        {
            var peekReader = reader;

            peekReader.Read();
            peekReader.AssumeTokenIsObjectStart();
            while (peekReader.Read() && !peekReader.TokenIsObjectEnd)
            {
                if (!peekReader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = peekReader.GetString()!;
                if (propertyName == nameof(TokenChallenge.Type))
                {
                    peekReader.Read();
                    return peekReader.GetString();
                }
                else
                {
                    peekReader.Skip();
                }
            }

            return null;
        }

        private TokenChallenge GetTokenChallengeV3()
        {
            ChallengeId? challengeId = null;
            ChallengeStatus? status = null;
            string? type = null;
            string? token = null;
            string? payload = null;
            DateTimeOffset? validated = null;
            AcmeError? error = null;


            reader.Read();
            reader.AssumeTokenIsObjectStart();

            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case SerializationVersionPropertyName:
                        reader.Read();
                        var serializationVersion = reader.GetInt32();
                        if (serializationVersion != 3)
                        {
                            throw new JsonException($"Unexpected serialization version {serializationVersion} when deserializing Order V3.");
                        }
                        break;

                    case nameof(TokenChallenge.ChallengeId):
                        reader.Read();
                        challengeId = reader.GetChallengeId();
                        break;

                    case nameof(TokenChallenge.Status):
                        reader.Read();
                        status = reader.GetEnumFromString<ChallengeStatus>();
                        break;

                    case nameof(TokenChallenge.Type):
                        reader.Read();
                        type = reader.GetString();
                        break;

                    case nameof(TokenChallenge.Token):
                        reader.Read();
                        token = reader.GetString();
                        break;

                    case nameof(TokenChallenge.Validated):
                        reader.Read();
                        validated = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(TokenChallenge.Error):
                        error = reader.GetAcmeErrorV3();
                        break;

                    default:
                        throw new JsonException($"Unexpected property when deserializing Challenge V3: {propertyName}");
                }
            }

            var result = new TokenChallenge(
                challengeId ?? throw new JsonException($"Missing required property: {nameof(TokenChallenge.ChallengeId)}"),
                status ?? throw new JsonException($"Missing required property: {nameof(TokenChallenge.Status)}"),
                type ?? throw new JsonException($"Missing required property: {nameof(TokenChallenge.Type)}"),
                token ?? throw new JsonException($"Missing required property: {nameof(TokenChallenge.Token)}"),
                validated,
                error);

            return result;
        }
        
        private TokenChallenge GetDeviceAttestChallengeV3()
        {
            ChallengeId? challengeId = null;
            ChallengeStatus? status = null;
            string? type = null;
            string? token = null;
            string? payload = null;
            DateTimeOffset? validated = null;
            AcmeError? error = null;


            reader.Read();
            reader.AssumeTokenIsObjectStart();

            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case SerializationVersionPropertyName:
                        reader.Read();
                        var serializationVersion = reader.GetInt32();
                        if (serializationVersion != 3)
                        {
                            throw new JsonException($"Unexpected serialization version {serializationVersion} when deserializing Order V3.");
                        }
                        break;

                    case nameof(TokenChallenge.ChallengeId):
                        reader.Read();
                        challengeId = reader.GetChallengeId();
                        break;

                    case nameof(TokenChallenge.Status):
                        reader.Read();
                        status = reader.GetEnumFromString<ChallengeStatus>();
                        break;

                    case nameof(TokenChallenge.Type):
                        reader.Read();
                        type = reader.GetString();
                        break;

                    case nameof(TokenChallenge.Token):
                        reader.Read();
                        token = reader.GetString();
                        break;

                    case nameof(DeviceAttestChallenge.Payload):
                        reader.Read();
                        payload = reader.GetString();
                        break;

                    case nameof(TokenChallenge.Validated):
                        reader.Read();
                        validated = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(TokenChallenge.Error):
                        error = reader.GetAcmeErrorV3();
                        break;

                    default:
                        throw new JsonException($"Unexpected property when deserializing Challenge V3: {propertyName}");
                }
            }

            var result = new DeviceAttestChallenge(
                challengeId ?? throw new JsonException($"Missing required property: {nameof(TokenChallenge.ChallengeId)}"),
                status ?? throw new JsonException($"Missing required property: {nameof(TokenChallenge.Status)}"),
                type ?? throw new JsonException($"Missing required property: {nameof(TokenChallenge.Type)}"),
                token ?? throw new JsonException($"Missing required property: {nameof(TokenChallenge.Token)}"),
                payload,
                validated,
                error);

            return result;
        }


        public AcmeError? GetAcmeErrorV3()
        {
            string? type = null;
            string? detail = null;

            List<AcmeError>? subErrors = null;
            
            Dictionary<string, object> additionalFields = [];

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return null;
                }

                if (reader.TokenIsObjectStart && isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }

                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case nameof(AcmeError.Type):
                        reader.Read();
                        type = reader.GetString();
                        break;

                    case nameof(AcmeError.Detail):
                        reader.Read();
                        detail = reader.GetString();
                        break;

                    case nameof(AcmeError.Identifier):
                        additionalFields[nameof(AcmeError.Identifier)] = reader.GetIdentifierV3();
                        break;

                    case nameof(AcmeError.SubErrors):
                        subErrors = reader.GetList((ref reader) => reader.GetAcmeErrorV3()!);
                        break;

                    case nameof(AcmeError.HttpStatusCode):
                        reader.Read();
                        additionalFields[nameof(AcmeError.HttpStatusCode)] = reader.GetInt32();
                        break;

                    case nameof(AcmeError.AdditionalFields):
                        additionalFields = reader.GetAdditionalErrorFieldsV3();
                        break;

                    case "Algorithms":
                        additionalFields[propertyName] = reader.GetStringList()!;
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new AcmeError(
                type ?? throw new JsonException("Missing required property: type"),
                detail ?? throw new JsonException("Missing required property: detail"),
                subErrors
            );
           
            foreach (var (key, value) in additionalFields)
            {
                result.AdditionalFields[key] = value;
            }

            return result;
        }

        public Dictionary<string, object> GetAdditionalErrorFieldsV3()
        {
            var result = new Dictionary<string, object>();

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return result;
                }

                if (reader.TokenIsObjectStart && isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }

                if (!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case "algorithms":
                        var algorithms = reader.GetWrappedList(
                            (ref reader, references) => reader.GetString()!,
                            []
                        ) ?? [];
                        result[propertyName] = algorithms;
                        break;

                    default:
                        reader.Skip();
                        break;
                }

            }

            return result;
        }
    }
}
