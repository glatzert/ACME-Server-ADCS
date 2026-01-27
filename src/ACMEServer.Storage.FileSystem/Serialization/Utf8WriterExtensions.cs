using System.Text.Json;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using static ACMEServer.Storage.FileSystem.FileSystemConstants;

namespace ACMEServer.Storage.FileSystem.Serialization;

internal static class Utf8WriterExtensions
{
    extension(Utf8JsonWriter writer)
    {
        public void WriteAccount(Account account)
        {
            writer.WriteStartObject();
            {
                writer.WriteNumber(SerializationVersionPropertyName, 2);

                writer.WriteString(nameof(Account.AccountId), account.AccountId.Value);
                writer.WriteString(nameof(Account.Status), account.Status.ToString());

                writer.WriteString(nameof(Account.Jwk), account.Jwk.Json);

                writer.WriteArrayOrNull(nameof(Account.Contacts), account.Contacts, (writer, contact) =>
                {
                    writer.WriteStringValue(contact);
                });

                writer.WriteStringOrNull(nameof(Account.TOSAccepted), account.TOSAccepted);

                writer.WriteObjectOrNull(nameof(Account.ExternalAccountBinding), account.ExternalAccountBinding, (writer, eab) =>
                {
                    writer.WriteStartObject();
                    writer.WriteString(nameof(AcmeJwsToken.Protected), eab.Protected);
                    writer.WriteString(nameof(AcmeJwsToken.Payload), eab.Payload);
                    writer.WriteString(nameof(AcmeJwsToken.Signature), eab.Signature);
                    writer.WriteEndObject();
                });

                writer.WriteNumber(nameof(Account.Version), account.Version);
            }
            writer.WriteEndObject();
        }

        public void WriteCertificateContainer(CertificateContainer container)
        {
            writer.WriteStartObject();
            {
                writer.WriteNumber("SerializationVersion", 1);

                writer.WriteString(nameof(CertificateContainer.CertificateId), container.CertificateId.Value);
                writer.WriteString(nameof(CertificateContainer.OrderId), container.OrderId.Value);
                writer.WriteString(nameof(CertificateContainer.AccountId), container.AccountId.Value);

                writer.WriteBase64String(nameof(CertificateContainer.X509Certificates), container.X509Certificates);
                writer.WriteString(nameof(CertificateContainer.RevokationStatus), container.RevokationStatus.ToString());

                writer.WriteNumber(nameof(CertificateContainer.Version), container.Version);
            }
            writer.WriteEndObject();
        }

        public void WriteOrder(Order order)
        { 
            writer.WriteStartObject();
            {
                writer.WriteNumber(SerializationVersionPropertyName, 3);

                writer.WriteString(nameof(Order.OrderId), order.OrderId.Value);
                writer.WriteString(nameof(Order.AccountId), order.AccountId.Value);
                writer.WriteString(nameof(Order.Status), order.Status.ToString());

                writer.WriteArray(nameof(Order.Identifiers), order.Identifiers, (writer, identifier) =>
                {
                    writer.WriteIdentifier(identifier);
                });
                writer.WriteArray(nameof(Order.Authorizations), order.Authorizations, (writer, authorization) =>
                {
                    writer.WriteAuthorizaton(authorization);
                });

                writer.WriteStringOrNull(nameof(Order.NotBefore), order.NotBefore);
                writer.WriteStringOrNull(nameof(Order.NotAfter), order.NotAfter);
                writer.WriteStringOrNull(nameof(Order.Expires), order.Expires);

                writer.WriteString(nameof(Order.Profile), order.Profile.Value);

                writer.WriteStringOrNull(nameof(Order.CertificateSigningRequest), order.CertificateSigningRequest);
                writer.WriteStringOrNull(nameof(Order.CertificateId), order.CertificateId?.Value);

                writer.WriteObjectOrNull(nameof(Order.Error), order.Error, (writer, error) =>
                {
                    writer.WriteError(error);
                });

                writer.WriteNumber(nameof(Order.Version), order.Version);
            }
            writer.WriteEndObject();
        }

        private void WriteIdentifier(Identifier identifier)
        {
            writer.WriteStartObject();
            {
                writer.WriteNumber(SerializationVersionPropertyName, 3);

                writer.WriteString(nameof(Identifier.Type), identifier.Type);
                writer.WriteString(nameof(Identifier.Value), identifier.Value);

                writer.WriteObjectOrNull(nameof(Identifier.Metadata), identifier.Metadata, (writer, metadata) =>
                {
                    writer.WriteStartObject();
                    foreach (var kvp in metadata)
                    {
                        writer.WriteString(kvp.Key, kvp.Value);
                    }
                    writer.WriteEndObject();
                });
            }
            writer.WriteEndObject();
        }

        private void WriteIdentifier(string propertyName, Identifier identifier)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteIdentifier(identifier);
        }

        private void WriteAuthorizaton(Authorization authorization)
        {
            writer.WriteStartObject();
            {
                writer.WriteNumber(SerializationVersionPropertyName, 3);

                writer.WriteString(nameof(Authorization.AuthorizationId), authorization.AuthorizationId.Value);
                writer.WriteString(nameof(Authorization.Status), authorization.Status.ToString());

                writer.WriteIdentifier(nameof(Authorization.Identifier), authorization.Identifier);
                writer.WriteBoolean(nameof(Authorization.IsWildcard), authorization.IsWildcard);

                writer.WriteString(nameof(Authorization.Expires), authorization.Expires);
                writer.WriteArray(nameof(Authorization.Challenges), authorization.Challenges, (writer, challenge) =>
                {
                    writer.WriteChallenge(challenge);
                });
            }
            writer.WriteEndObject();
        }

        private void WriteChallenge(Challenge challenge)
        {
            writer.WriteStartObject();
            {
                writer.WriteNumber(SerializationVersionPropertyName, 3);

                writer.WriteString(nameof(Challenge.Type), challenge.Type);
                writer.WriteString(nameof(Challenge.ChallengeId), challenge.ChallengeId.Value);
                writer.WriteString(nameof(Challenge.Status), challenge.Status.ToString());

                writer.WriteStringOrNull(nameof(Challenge.Validated), challenge.Validated);
                writer.WriteObjectOrNull(nameof(Challenge.Error), challenge.Error, (writer, error) =>
                {
                    writer.WriteError(error);
                });

                // TODO: switch on Challenge type
                writer.WriteString(nameof(Challenge.Token), challenge.Token);
                writer.WriteStringOrNull(nameof(Challenge.Payload), challenge.Payload);

            }
            writer.WriteEndObject();
        }

        private void WriteError(AcmeError error)
        {
            writer.WriteStartObject();
            {
                writer.WriteNumber(SerializationVersionPropertyName, 3);

                writer.WriteString(nameof(AcmeError.Type), error.Type);
                writer.WriteString(nameof(AcmeError.Detail), error.Detail);

                writer.WriteObjectOrNull(nameof(AcmeError.Identifier), error.Identifier, (writer, identifier) =>
                {
                    writer.WriteIdentifier(identifier);
                });

                writer.WriteArrayOrNull(nameof(AcmeError.SubErrors), error.SubErrors, (writer, subError) =>
                {
                    writer.WriteError(subError);
                });

                if (error.AdditionalFields.Count > 0) {
                    writer.WriteStartObject(nameof(AcmeError.AdditionalFields));
                    foreach (var additionalField in error.AdditionalFields)
                    {
                        if (additionalField.Value is IEnumerable<string> values)
                        {
                            writer.WriteArray(additionalField.Key, values, (writer, value) =>
                            {
                                writer.WriteStringValue(value);
                            });
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unsupported additional field type: {additionalField.Value.GetType().FullName}");
                        }
                    }
                    writer.WriteEndObject();
                }
                
                if(error.HttpStatusCode.HasValue)
                    writer.WriteNumber(nameof(AcmeError.HttpStatusCode), error.HttpStatusCode.Value);
            }
            writer.WriteEndObject();
        }


        private void WriteArray<T>(string propertyName, IEnumerable<T>? items, Action<Utf8JsonWriter, T> writeItem)
        {
            writer.WriteStartArray(propertyName);
            if (items is not null)
            {
                foreach (var item in items)
                {
                    writeItem(writer, item);
                }
            }
            writer.WriteEndArray();
        }

        private void WriteArrayOrNull<T>(string propertyName, IEnumerable<T>? items, Action<Utf8JsonWriter, T> writeItem)
        {
            if (items is null)
            {
                writer.WriteNull(propertyName);
            }
            else
            {
                writer.WriteArray(propertyName, items, writeItem);
            }
        }

        private void WriteObjectOrNull<T>(string propertyName, T? item, Action<Utf8JsonWriter, T> writeItem) 
            where T : class
        {
            if (item is null)
            {
                writer.WriteNull(propertyName);
            }
            else
            {
                writer.WritePropertyName(propertyName);
                writeItem(writer, item);
            }
        }

        
        private void WriteStringOrNull(string propertyName, string? value) 
        {
            if (value is null)
            {
                writer.WriteNull(propertyName);
            }
            else
            {
                writer.WriteString(propertyName, value);
            }
        }

        private void WriteStringOrNull(string propertyName, DateTimeOffset? value) 
        {
            if (value is null)
            {
                writer.WriteNull(propertyName);
            }
            else
            {
                writer.WriteString(propertyName, value.Value);
            }
        }
    }
}
