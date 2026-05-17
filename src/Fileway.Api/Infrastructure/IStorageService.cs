namespace Fileway.Api.Infrastructure;

public interface IStorageService
{
    Task<string> SaveAsync(ReadOnlyMemory<byte> content, string filename, string mimeType, CancellationToken cancellationToken);
    Task<(string Url, DateTimeOffset ExpiresAt)> GetSignedUrlAsync(string storageKey, CancellationToken cancellationToken);
}
