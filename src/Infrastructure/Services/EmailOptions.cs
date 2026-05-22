namespace Infrastructure.Services;

internal sealed class EmailOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 465;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string FromAddress { get; init; } = "noreply@hankkarpinen.com";
    public string FromName { get; init; } = "hankkarpinen.com";
    public string BaseUrl { get; init; } = "http://localhost:3000";
}
