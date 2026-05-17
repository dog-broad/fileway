using System.Reflection;
using System.Text;

namespace Fileway.Tests.Api.Helpers;

/// <summary>
/// Reads small test fixture files embedded as resources in the TestData/ folder.
/// Resource names follow the pattern: Fileway.Tests.Api.TestData.{filename}
/// </summary>
public static class EmbeddedTestFiles
{
    private static readonly Assembly Assembly = typeof(EmbeddedTestFiles).Assembly;
    private const string Prefix = "Fileway.Tests.Api.TestData.";

    /// <summary>Returns the raw bytes of the named embedded test file.</summary>
    public static byte[] GetBytes(string name)
    {
        var resourceName = Prefix + name.Replace('/', '.').Replace('\\', '.');
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded test file '{name}' not found. " +
                $"Available: {string.Join(", ", Assembly.GetManifestResourceNames())}");

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>Returns the UTF-8 text of the named embedded test file.</summary>
    public static string GetText(string name) => Encoding.UTF8.GetString(GetBytes(name));
}
