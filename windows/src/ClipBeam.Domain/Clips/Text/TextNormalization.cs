using System.Text;

namespace ClipBeam.Domain.Clips.Text
{
    public static class TextNormalization
    {
        public static string ToNfcLf(string input) => input switch
        {
            null => string.Empty,
            _ => input.Normalize(NormalizationForm.FormC)
                    .Replace("\r\n", "\n").Replace("\r", "\n")
        };
     
        public static ReadOnlyMemory<byte> ToUtf8Bytes(string text) =>
            Encoding.UTF8.GetBytes(text);
    }
}
