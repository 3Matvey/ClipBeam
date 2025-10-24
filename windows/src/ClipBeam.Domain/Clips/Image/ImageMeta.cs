using ClipBeam.Domain.Shared;

namespace ClipBeam.Domain.Clips.Image
{
    public sealed class ImageMeta
    {
        public ImageFormat Format { get; }
        public int Width { get; }
        public int Height { get; }
        public ExifOrientation Orientation { get; }
        public string Mime { get; }

        public ImageMeta(ImageFormat format, int width, int height, ExifOrientation orientation, string mime)
        {
            if (format == ImageFormat.Unspecified)
                throw new DomainException("Image format required.");
      
            if (width <= 0)
                throw new DomainException($"{nameof(width)} must be > 0");

            if (height <= 0)
                throw new DomainException($"{nameof(height)} must be > 0");

            if (string.IsNullOrWhiteSpace(mime))
                throw new DomainException($"{nameof(mime)} must be non-empty.");

            Mime = mime;

            bool ok = format switch
            {
                ImageFormat.Png => Mime.Contains("png", StringComparison.OrdinalIgnoreCase),
                ImageFormat.Jpeg => Mime.Contains("jpeg", StringComparison.OrdinalIgnoreCase) || Mime.Contains("jpg", StringComparison.OrdinalIgnoreCase),
                ImageFormat.Webp => Mime.Contains("webp", StringComparison.OrdinalIgnoreCase),
                _ => false
            };

            if (!ok) throw new DomainException("MIME doesn't match image format.");

            Format = format; Width = width; Height = height; Orientation = orientation;
        }
    }
}
