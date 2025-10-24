namespace ClipBeam.Domain.Clips
{
    public abstract class ClipContent
    {
        public abstract ReadOnlyMemory<byte> Raw { get; }

        public abstract ContentType Type { get; }
    }
}