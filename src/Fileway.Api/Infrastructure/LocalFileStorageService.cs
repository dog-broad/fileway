using Fileway.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Fileway.Api.Infrastructure;

public sealed class LocalFileStorageService(
    IOptions<StorageOptions> options,
    ILogger<LocalFileStorageService> logger) : IStorageService
{
    public async Task<string> SaveAsync(
        ReadOnlyMemory<byte> content, string filename, string mimeType, CancellationToken cancellationToken)
    {
        var dir = options.Value.LocalStoragePath;
        Directory.CreateDirectory(dir);

        var id = Guid.NewGuid();
        var key = $"{id:N}_{filename}";
        var path = Path.Combine(dir, key);

        await File.WriteAllBytesAsync(path, content.ToArray(), cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Saved output to local storage {StorageId}", id);

        return key;
    }

    public Task<(string Url, DateTimeOffset ExpiresAt)> GetSignedUrlAsync(
        string storageKey, CancellationToken cancellationToken)
    {
        var ttl = options.Value.SignedUrlTtlMinutes;
        var url = $"/storage/{Uri.EscapeDataString(storageKey)}";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(ttl);
        return Task.FromResult((url, expiresAt));
    }
}
