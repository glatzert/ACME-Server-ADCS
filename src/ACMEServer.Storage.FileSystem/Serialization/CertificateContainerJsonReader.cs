using System.Text.Json;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;
using static ACMEServer.Storage.FileSystem.FileSystemConstants;

namespace ACMEServer.Storage.FileSystem.Serialization;

internal static class CertificateContainerJsonReader
{
    extension(ref Utf8JsonReader reader)
    {
        public CertificateContainer GetCertificateContainer()
        {
            CertificateId? certificateId = null;
            AccountId? accountId = null;
            OrderId? orderId = null;

            byte[]? x509Certificates = null;
            RevokationStatus? revokationStatus = null;

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
                        int serializationVersion = reader.GetInt32();
                        if (serializationVersion != 1)
                        {
                            throw new JsonException($"Unsupported Account serialization version: {serializationVersion}");
                        }
                        break;

                    case nameof(CertificateContainer.CertificateId):
                        reader.Read();
                        certificateId = reader.GetCertificateId();
                        break;

                    case nameof(CertificateContainer.AccountId):
                        reader.Read();
                        accountId = reader.GetAccountId();
                        break;

                    case nameof(CertificateContainer.OrderId):
                        reader.Read();
                        orderId = reader.GetOrderId();
                        break;

                    // The name differed from the property at some point in time, so we support both
                    case nameof(System.Security.Cryptography.X509Certificates.X509Certificate):
                    case nameof(CertificateContainer.X509Certificates):
                        reader.Read();
                        x509Certificates = reader.GetBytesFromBase64();
                        break;

                    case nameof(CertificateContainer.RevokationStatus):
                        reader.Read();
                        revokationStatus = reader.GetEnumFromString<RevokationStatus>();
                        break;

                    case nameof(CertificateContainer.Version):
                        reader.Read();
                        version = reader.GetInt64();
                        break;

                    case "$id":
                        reader.Skip();
                        break;

                    default:
                        throw new JsonException($"Unexpected property: {propertyName}");
                }
            }

            var result = new CertificateContainer(
                certificateId ?? throw new JsonException("Missing required property: certificateId"),
                accountId ?? throw new JsonException("Missing required property: accountId"),
                orderId ?? throw new JsonException("Missing required property: orderId"),
                x509Certificates ?? throw new JsonException("Missing required property: x509Certificates"),
                // RevokationStatus was never properly written, so we default to NotRevoked
                revokationStatus ?? RevokationStatus.NotRevoked,
                version ?? 0
            );

            return result;
        }
    }
}
