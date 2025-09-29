namespace App.Core.Interfaces;

public interface IGrpcClient
{
    Task<TResponse> CallAsync<TRequest, TResponse>(
        string baseUrl,
        string methodName,
        TRequest request,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;
}