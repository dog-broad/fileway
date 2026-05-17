namespace Fileway.Shared.Formats;

public sealed record MagicSignature
{
    public int Offset { get; init; }
    public required byte[] Bytes { get; init; }
    public byte[]? Mask { get; init; }
}
