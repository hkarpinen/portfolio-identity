using Application.Ports;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

internal sealed class LocalFileStorage : IFileStorage
{
    private readonly LocalFileStorageOptions _options;

    public LocalFileStorage(IOptions<LocalFileStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_options.LocalPath, key);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using (var file = File.Create(path))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    public Task DeleteByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var prefix = _options.PublicBaseUrl.TrimEnd('/') + "/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal))
            return Task.CompletedTask;

        // The URL comes from our own database, but resolving before comparing means a malformed
        // row cannot walk out of the storage root.
        var root = Path.GetFullPath(_options.LocalPath) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, url[prefix.Length..]));
        if (!path.StartsWith(root, StringComparison.Ordinal))
            return Task.CompletedTask;

        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }
}

public sealed class LocalFileStorageOptions
{
    public string LocalPath { get; set; } = string.Empty;
    /// <summary>
    /// Absolute public URL prefix for stored files (e.g. "http://localhost:3000/uploads/avatars" in dev,
    /// "https://example.com/uploads/avatars" in prod). Used so persisted URLs are environment-independent
    /// and survive migration to object storage with no data changes.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "/uploads/avatars";
}
