namespace Application.Ports;

public interface IFileStorage
{
    Task<string> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
