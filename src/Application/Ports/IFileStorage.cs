namespace Application.Ports;

public interface IFileStorage
{
    Task<string> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes the URL because that is what callers hold and what gets persisted. Reversing it into
    /// a key belongs to the storage that composed it.
    /// </summary>
    Task DeleteByUrlAsync(string url, CancellationToken cancellationToken = default);
}
