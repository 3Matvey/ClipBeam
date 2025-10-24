namespace ClipBeam.Domain.Clips
{
    public readonly record struct Hash(HashAlgo Algo, ReadOnlyMemory<byte> Value)
    {
        public int ByteLength => Value.Length;
        public override string ToString() => $"{Algo}:{Convert.ToHexString(Value.Span)}";
    }
}
