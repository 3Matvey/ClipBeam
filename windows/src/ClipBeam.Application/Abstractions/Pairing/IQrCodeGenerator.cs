using System;

namespace ClipBeam.Application.Abstractions.Pairing
{
    /// <summary>
    /// Renders a string payload (e.g. URI or JSON) into a QR code image.
    /// </summary>
    public interface IQrCodeGenerator
    {
        /// <summary>
        /// Renders the provided payload into an image file format (PNG/WebP) and returns the bytes.
        /// </summary>
        /// <param name="payload">String payload to encode into the QR code (URI, JSON, etc.).</param>
        /// <returns>
        /// Byte array containing the encoded image (commonly PNG or WebP). The exact format depends on the implementation
        /// and should be documented by the concrete implementation.
        /// </returns>
        byte[] Render(string payload);
    }
}
