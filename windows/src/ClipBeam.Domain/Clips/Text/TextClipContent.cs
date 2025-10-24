namespace ClipBeam.Domain.Clips.Text
{
    public sealed class TextClipContent : ClipContent
    {
        public string TextNfcLf { get; }
        private readonly ReadOnlyMemory<byte> _raw;

        public override ReadOnlyMemory<byte> Raw => _raw;
        public override ContentType Type => ContentType.Text;

        private TextClipContent(string normalizedText)
        {
            TextNfcLf = normalizedText;
            _raw = TextNormalization.ToUtf8Bytes(normalizedText);
        }

        public static TextClipContent FromRaw(string? text) =>
            new(TextNormalization.ToNfcLf(text ?? string.Empty));
    }
}