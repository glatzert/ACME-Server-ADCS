using System.Text.Json;
using Th11s.ACMEServer.Model.Primitives;

namespace ACMEServer.Storage.FileSystem.Serialization;

internal static class CommonJsonReader
{
    public delegate T GetListItem<T>(ref Utf8JsonReader reader, Dictionary<string, object> references);
    public delegate T GetListItem2<T>(ref Utf8JsonReader reader);

    extension(ref Utf8JsonReader reader)
    {
        public bool TokenIsObjectStart
            => reader.TokenType == JsonTokenType.StartObject;

        public bool TokenIsObjectEnd
            => reader.TokenType == JsonTokenType.EndObject;

        public bool TokenIsArrayStart
            => reader.TokenType == JsonTokenType.StartArray;

        public bool TokenIsArrayEnd
            => reader.TokenType == JsonTokenType.EndArray;

        public bool TokenIsPropertyName
            => reader.TokenType == JsonTokenType.PropertyName;


        public void AssumeTokenIsObjectStart()
        {
            if (!reader.TokenIsObjectStart)
            {
                throw new JsonException($"Expected start of object, but found {reader.TokenType}.");
            }
        }

        public void AssumeTokenIsObjectEnd()
        {
            if (!reader.TokenIsObjectEnd)
            {
                throw new JsonException($"Expected end of object, but found {reader.TokenType}.");
            }
        }


        public void AssumeTokenIsArrayStart()
        {
            if (!reader.TokenIsArrayStart)
            {
                throw new JsonException($"Expected start of array, but found {reader.TokenType}.");
            }
        }

        public void AssumeTokenIsArrayEnd()
        {
            if (!reader.TokenIsArrayEnd)
            {
                throw new JsonException($"Expected end of array, but found {reader.TokenType}.");
            }
        }


        public void AssumePropertyName(string expectedName)
        {
            if (!reader.TokenIsPropertyName || reader.GetString() != expectedName)
            {
                throw new JsonException($"Expected property name '{expectedName}', but found '{reader.GetString()}'.");
            }
        }


        public T GetEnumFromString<T>() where T : struct, Enum
            => Enum.Parse<T>(reader.GetString() ?? throw new JsonException($"Expected string value for {typeof(T).Name}."));


        public DateTimeOffset? GetOptionalDateTimeOffset()
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return reader.GetDateTimeOffset();
        }

        public AccountId GetAccountId()
            => new(reader.GetString() ?? throw new JsonException("Expected string value for AccountId"));

        public AuthorizationId GetAuthorizationId()
            => new(reader.GetString() ?? throw new JsonException("Expected string value for AuthorizationId"));

        public CertificateId? GetCertificateId()
            => reader.GetString() is string certId ? new(certId) : null;

        public ChallengeId GetChallengeId()
            => new(reader.GetString() ?? throw new JsonException("Expected string value for ChallengeId"));

        public OrderId GetOrderId()
            => new(reader.GetString() ?? throw new JsonException("Expected string value for OrderId"));

        public ProfileName GetProfileName()
            => new(reader.GetString() ?? throw new JsonException("Expected string value for ProfileName"));


        public int? PeekSerializationVersion()
        {
            var peekReader = reader;
            if (peekReader.Read() && peekReader.TokenIsObjectStart)
            {
                while (peekReader.Read() && !peekReader.TokenIsObjectEnd)
                {
                    if (!peekReader.TokenIsPropertyName)
                    {
                        throw new JsonException("Expected property name.");
                    }

                    string propertyName = peekReader.GetString()!;
                    if (propertyName == FileSystemConstants.SerializationVersionPropertyName)
                    {
                        peekReader.Read();
                        return peekReader.GetInt32();
                    }
                    else
                    {
                        peekReader.Skip();
                    }
                }
            }
            return null;
        }


        public List<T>? GetWrappedList<T>(GetListItem<T> itemReader, Dictionary<string, object> references)
        {
            var result = new List<T>();
            string? refIndex = null;

            var peekReader = reader;
            if (peekReader.Read() && peekReader.TokenIsArrayStart)
            {
                return reader.GetList(itemReader, references);
            }

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
                    throw new JsonException($"Unexpected token type: {reader.TokenType}");
                }

                var propertyName = reader.GetString();
                switch (propertyName)
                {
                    case "$id":
                        reader.Read();
                        refIndex = reader.GetString();
                        break;

                    case "$values":
                        result = reader.GetList(itemReader, references);
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            if (refIndex is not null)
            {
                references[refIndex] = result;
            }


            return result;
        }

        private List<T> GetList<T>(GetListItem<T> itemReader, Dictionary<string, object> references)
        {
            var result = new List<T>();

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsArrayEnd)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return [];
                }

                if (reader.TokenType == JsonTokenType.StartArray && isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }

                result.Add(itemReader(ref reader, references));
            }

            return result;
        }

        public List<T>? GetList<T>(GetListItem2<T> itemReader)
        {
            var peekReader = reader;
            if (peekReader.Read() && peekReader.TokenType == JsonTokenType.Null)
            {
                reader.Read();
                return null;
            }

            var result = new List<T>();
            reader.Read();
            reader.AssumeTokenIsArrayStart();

            peekReader = reader;
            while (peekReader.Read() && peekReader.TokenIsObjectStart)
            {
                result.Add(itemReader(ref reader));
                peekReader = reader;
            }

            reader.Read();
            reader.AssumeTokenIsArrayEnd();
            return result;
        }

        public List<string>? GetStringList()
        {
            var result = new List<string>();

            bool isFirstToken = true;
            while (reader.Read() && !reader.TokenIsArrayEnd)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return [];
                }

                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    if(isFirstToken)
                    {
                        isFirstToken = false;
                        continue;
                    }
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    result.Add(reader.GetString()!);
                }
                else
                {
                    throw new JsonException($"Expected string in array, but found {reader.TokenType}.");
                }
            }

            return result;
        }


    }
}
