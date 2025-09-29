namespace App.Core.Interfaces;

public interface IRestClient
{
    Task<TResponse?> GetAsync<TResponse>(
        string baseUrl,
        string path,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
    
    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string baseUrl,
        string path,
        TRequest body,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
    
    Task<TResponse?> PutAsync<TRequest, TResponse>(
        string baseUrl,
        string path,
        TRequest body,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
    
    Task<bool> DeleteAsync(
        string message,
        string path,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}