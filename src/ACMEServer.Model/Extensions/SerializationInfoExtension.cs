using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Th11s.ACMEServer.Model.Extensions
{
    public static class SerializationInfoExtension
    {
        public static string GetRequiredString(this SerializationInfo info, string name)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            var value = info.GetString(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Could not deserialize required value '{name}'");

            return value;
        }

        public static TEnum GetEnumFromString<TEnum>(this SerializationInfo info, string name)
            where TEnum : struct
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            var value = info.GetString(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Could not deserialize enum value '{name}'");

            return Enum.Parse<TEnum>(value);
        }

        [return: NotNull]
        public static T GetRequiredValue<T>(this SerializationInfo info, string name)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            var value = info.GetValue(name, typeof(T));
            if (value is null)
                throw new InvalidOperationException($"Could not deserialize required value '{name}'");

            return (T)value;
        }

        [return: MaybeNull]
        public static T GetValue<T>(this SerializationInfo info, string name)
        {
            ArgumentNullException.ThrowIfNull(info, nameof(info));

            return (T)info.GetValue(name, typeof(T))!;
        }

        [return: MaybeNull]
        public static T TryGetValue<T>(this SerializationInfo info, string name)
        {
            ArgumentNullException.ThrowIfNull(info, nameof(info));

            try
            {
                return (T)info.GetValue(name, typeof(T))!;
            }
            catch (SerializationException)
            {
                return default;
            }
        }
    }
}
