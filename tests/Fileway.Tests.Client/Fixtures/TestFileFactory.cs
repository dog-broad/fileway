using Fileway.Shared.Formats;
using Fileway.Shared.Processors;

namespace Fileway.Tests.Client.Fixtures;

/// <summary>
/// Builds InputFile instances for tests. Sets DetectedFormat, SizeBytes, and Index correctly.
/// </summary>
public static class TestFileFactory
{
    public static InputFile FromBytes(byte[] content, FileFormat format, int index = 0, string? filename = null)
    {
        var memory = new ReadOnlyMemory<byte>(content);
        return new InputFile
        {
            Content = memory,
            DetectedFormat = format,
            SizeBytes = content.Length,
            OriginalFilename = filename,
            Index = index
        };
    }

    public static InputFile FromText(string text, FileFormat format, int index = 0, string? filename = null)
        => FromBytes(System.Text.Encoding.UTF8.GetBytes(text), format, index, filename);

    public static InputFile Empty(FileFormat format, int index = 0)
        => FromBytes([], format, index);
}
