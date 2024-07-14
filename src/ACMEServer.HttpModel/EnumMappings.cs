using System;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.HttpModel
{
    /// <summary>
    /// Defines response texts for enum fields.
    /// </summary>
    public static class EnumMappings
    {
        public static string GetEnumString(AccountStatus status)
            => status switch
            {
                AccountStatus.Valid => "valid",
                AccountStatus.Deactivated => "deactivated",
                AccountStatus.Revoked => "revoked",
                _ => throw new InvalidOperationException("Unknown AccountStatus")
            };

        internal static string GetEnumString(AuthorizationStatus status)
            => status switch
            {
                AuthorizationStatus.Pending => "pending",
                AuthorizationStatus.Valid => "valid",
                AuthorizationStatus.Invalid => "invalid",
                AuthorizationStatus.Revoked => "revoked",
                AuthorizationStatus.Deactivated => "deactivated",
                AuthorizationStatus.Expired => "expired",
                _ => throw new InvalidOperationException("Unknown AuthorizationStatus")
            };

        internal static string GetEnumString(ChallengeStatus status)
            => status switch
            {
                ChallengeStatus.Pending => "pending",
                ChallengeStatus.Processing => "processing",
                ChallengeStatus.Valid => "valid",
                ChallengeStatus.Invalid => "invalid",
                _ => throw new InvalidOperationException("Unknown ChallengeStatus")
            };

        internal static string GetEnumString(OrderStatus status)
            => status switch
            {
                OrderStatus.Pending => "pending",
                OrderStatus.Ready => "ready",
                OrderStatus.Processing => "processing",
                OrderStatus.Valid => "valid",
                OrderStatus.Invalid => "invalid",
                _ => throw new InvalidOperationException("Unknown OrderStatus")
            };
    }
}
