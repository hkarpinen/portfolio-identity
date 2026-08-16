namespace Application.Ports;

public interface IFileStorage
{
    Task<string> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes the URL rather than the key because the URL is what callers hold — it is what
    /// <see cref="SaveAsync"/> returns and what gets persisted. Reversing it into a key is the
    /// storage's own business, since the storage is what composed it.
    /// </summary>
    Task DeleteByUrlAsync(string url, CancellationToken cancellationToken = default);
}
