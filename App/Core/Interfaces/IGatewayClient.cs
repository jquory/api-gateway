namespace App.Core.Interfaces;

public interface IGatewayClient
{
    Task<T?> SendAsync<T>(
        string serviceName,
        string path,
        HttpMethod httpMethod,
        object? body = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}