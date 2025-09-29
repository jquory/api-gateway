namespace App.Core.Interfaces;

public interface IGraphQLClient
{
    Task<TResponse?> QueryAsync<TResponse>(
        string baseUrl,
        string query,
        object? variables = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
    
    Task<TResponse?> MutationAsync<TResponse>(
        string baseUrl,
        string query,
        object? variables = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}