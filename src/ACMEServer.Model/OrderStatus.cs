namespace TGIT.ACME.Protocol.Model
{
    public enum OrderStatus
    {
        /// <summary>
        /// The order waits for notification on authorization / challenge readiness.
        /// </summary>
        Pending,

        /// <summary>
        /// The order is ready to receive a CSR.
        /// </summary>
        Ready,

        /// <summary>
        /// The order processes the CSR.
        /// </summary>
        Processing,

        /// <summary>
        /// A certificate has been issued.
        /// </summary>
        Valid,

        /// <summary>
        /// The order got invalid. See Errors for reasons.
        /// </summary>
        Invalid
    }
}
