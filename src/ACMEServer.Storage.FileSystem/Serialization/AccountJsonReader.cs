using System.Text.Json;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using static ACMEServer.Storage.FileSystem.FileSystemConstants;

namespace ACMEServer.Storage.FileSystem.Serialization;

internal static class AccountJsonReader
{
    extension(ref Utf8JsonReader reader)
    {
        public Account GetAccount()
        {
            var peekReader = reader;

            var serializationVersion = reader.PeekSerializationVersion();

            if (serializationVersion == 1)
            {
                return reader.GetAccountV1([]);
            }
            if (serializationVersion == 2)
            {
                return reader.GetAccountV2();
            }

            throw new JsonException($"Unsupported Account serialization version: {serializationVersion}");
        }

        public Account GetAccountV1(Dictionary<string, object> references)
        {
            string? refIndex = null;

            AccountId? accountId = null;
            AccountStatus? status = null;
            Jwk? jwk = null;

            List<string> contacts = [];
            DateTimeOffset? tosAccepted = null;
            AcmeJwsToken? externalAccountBinding = null;

            long version = default;

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    throw new JsonException("Account cannot be null.");
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

                    case nameof(Account.AccountId):
                        reader.Read();
                        accountId = reader.GetAccountId();
                        break;

                    case nameof(Account.Status):
                        reader.Read();
                        status = reader.GetEnumFromString<AccountStatus>();
                        break;

                    case nameof(Account.Jwk):
                        jwk = reader.GetJwkV1(references);
                        break;

                    case nameof(Account.Contacts):
                        contacts = reader.GetWrappedList(
                                (ref reader, references) =>
                                {
                                    return reader.GetString()!;
                                },
                                references
                            ) ?? [];
                        break;

                    case nameof(Account.TOSAccepted):
                        reader.Read();
                        tosAccepted = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(Account.ExternalAccountBinding):
                        externalAccountBinding = reader.GetAcmeJwsTokenV1(references);
                        break;

                    case nameof(Account.Version):
                        reader.Read();
                        version = reader.GetInt64();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Account(
                accountId ?? throw new JsonException("Missing required property: accountId"),
                status ?? throw new JsonException("Missing required property: status"),
                jwk ?? throw new JsonException("Missing required property: jwk"),
                contacts,
                tosAccepted,
                externalAccountBinding,
                version
            );

            if (refIndex is not null)
            {
                references[refIndex] = result;
            }

            return result;
        }

        public Account GetAccountV2()
        {
            // Initialize all variables
            AccountId? accountId = null;
            AccountStatus? status = null;
            string? jwkJson = null;

            List<string>? contacts = null;
            DateTimeOffset? tosAccepted = null;
            AcmeJwsToken? externalAccountBinding = null;

            long version = default;


            reader.Read();
            reader.AssumeTokenIsObjectStart();
            while(reader.Read() && !reader.TokenIsObjectEnd)
            {
                if(!reader.TokenIsPropertyName)
                {
                    throw new JsonException("Expected property name.");
                }

                string propertyName = reader.GetString()!;
                switch (propertyName)
                {
                    case SerializationVersionPropertyName:
                        reader.Read();
                        int serializationVersion = reader.GetInt32();
                        if (serializationVersion != 2)
                        {
                            throw new JsonException($"Unsupported Account serialization version: {serializationVersion}");
                        }
                        break;

                    case nameof(Account.AccountId):
                        reader.Read();
                        accountId = reader.GetAccountId();
                        break;

                    case nameof(Account.Status):
                        reader.Read();
                        status = reader.GetEnumFromString<AccountStatus>();
                        break;

                    case nameof(Account.Jwk):
                        reader.Read();
                        jwkJson = reader.GetString();
                        break;

                    case nameof(Account.Contacts):
                        reader.Read();
                        contacts = reader.GetStringList() ?? [];
                        break;

                    case nameof(Account.TOSAccepted):
                        reader.Read();
                        tosAccepted = reader.GetOptionalDateTimeOffset();
                        break;

                    case nameof(Account.ExternalAccountBinding):
                        externalAccountBinding = reader.GetAcmeJwsTokenV2();
                        break;

                    case nameof(Account.Version):
                        reader.Read();
                        version = reader.GetInt64();
                        break;

                    default:
                        throw new JsonException($"Unexpected property name: {propertyName}");
                }
            }

            return new Account(
                accountId ?? throw new JsonException($"Missing required property: {nameof(Account.AccountId)}"),
                status ?? throw new JsonException($"Missing required property: {nameof(Account.Status)}"),
                new(jwkJson ?? throw new JsonException($"Missing required property: {nameof(Account.Jwk)}")),
                contacts ?? throw new JsonException($"Missing required property: {nameof(Account.Contacts)}"),
                tosAccepted,
                externalAccountBinding,
                version
            );
        }


        private Jwk GetJwkV1(Dictionary<string, object> references)
        {
            string? refIndex = null;
            string? json = null;

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsObjectEnd)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    throw new JsonException("Jwk cannot be null.");
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

                    case nameof(Jwk.Json):
                        reader.Read();
                        json = reader.GetString();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
            var result = new Jwk(
                json ?? throw new JsonException("Missing required property: json")
            );

            if (refIndex is not null)
            {
                references[refIndex] = result;
            }
            return result;
        }


        private AcmeJwsToken? GetAcmeJwsTokenV1(Dictionary<string, object> references)
        {
            string? refIndex = null;

            string? @protected = null;
            string? payload = null;
            string? signature = null;

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

                    case nameof(AcmeJwsToken.Protected):
                        reader.Read();
                        @protected = reader.GetString();
                        break;

                    case nameof(AcmeJwsToken.Payload):
                        reader.Read();
                        payload = reader.GetString();
                        break;

                    case nameof(AcmeJwsToken.Signature):
                        reader.Read();
                        signature = reader.GetString();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
            if (@protected is null || signature is null)
            {
                throw new JsonException("Missing required properties for AcmeJwsToken.");
            }
            var result = new AcmeJwsToken(
                @protected,
                payload,
                signature
            );

            if (refIndex is not null)
            {
                references[refIndex] = result;
            }

            return result;
        }

        private AcmeJwsToken? GetAcmeJwsTokenV2()
        {
            var peekReader = reader;
            if(peekReader.Read() && peekReader.TokenType == JsonTokenType.Null)
            {
                reader.Read();
                return null;
            }

            string? @protected = null;
            string? payload = null;
            string? signature = null;

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
                    case nameof(AcmeJwsToken.Protected):
                        reader.Read();
                        @protected = reader.GetString();
                        break;

                    case nameof(AcmeJwsToken.Payload):
                        reader.Read();
                        payload = reader.GetString();
                        break;

                    case nameof(AcmeJwsToken.Signature):
                        reader.Read();
                        signature = reader.GetString();
                        break;

                    default:
                        throw new JsonException($"Unexpected property name: {propertyName}");
                }
            }

            var result = new AcmeJwsToken(
                @protected ?? throw new JsonException($"Missing required property: {nameof(AcmeJwsToken.Protected)}"),
                payload,
                signature ?? throw new JsonException($"Missing required property: {nameof(AcmeJwsToken.Signature)}")
            );

            return result;
        }
    }
}