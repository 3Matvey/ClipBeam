namespace ClipBeam.Application.Abstractions.Pairing
{
    /// <summary>
    /// Issues and validates short-lived pairing tokens used during device pairing flows.
    /// </summary>
    public interface IPairingTokenService
    {
        /// <summary>
        /// Issues a fresh pairing token.
        /// </summary>
        /// <param name="ct">Cancellation token to cancel the issuance operation.</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        ///   <item>
        ///     <description><see cref="string"/> <c>tokenId</c>: An opaque token identifier used to reference the token on the server.</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="byte[]"/> <c>tokenRaw</c>: The raw token bytes that should be embedded into the pairing payload (e.g., for QR).</description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="DateTime"/> <c>expiresUtc</c>: UTC expiry time after which the token is invalid.</description>
        ///   </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// Tokens are short-lived and intended for single-use pairing flows. Persist only the tokenId and metadata
        /// required to validate later calls to <see cref="ValidateAsync(string, ReadOnlyMemory{byte}, CancellationToken)"/>.
        /// </remarks>
        Task<(string tokenId, byte[] tokenRaw, DateTime expiresUtc)> IssueAsync(CancellationToken ct);

        /// <summary>
        /// Validates a pairing token presented by a peer.
        /// </summary>
        /// <param name="tokenId">The server-side token identifier previously issued.</param>
        /// <param name="tokenRaw">The raw token bytes provided by the peer to validate.</param>
        /// <param name="ct">Cancellation token to cancel the validation operation.</param>
        /// <returns>True if the token is valid and not expired/used; otherwise false.</returns>
        Task<bool> ValidateAsync(string tokenId, ReadOnlyMemory<byte> tokenRaw, CancellationToken ct);
    }
}
