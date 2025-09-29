using System.Text;
using System.Text.Json;
using App.Core.Interfaces;

namespace App.Infrastructure.Clients.Rest;

public class RestClient(HttpClient httpClient, ILogger<RestClient> logger) : IRestClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<RestClient> _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TResponse?> GetAsync<TResponse>(
        string baseUrl,
        string path,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var url = CombineUrl(baseUrl, path);
        _logger.LogInformation("GET request to {url}", url);
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(request, headers);
        
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await HandleResponse<TResponse>(response);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string baseUrl,
        string path,
        TRequest body,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var url = CombineUrl(baseUrl, path);
        _logger.LogInformation("POST request to {url}", url);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json")
        };
        AddHeaders(request, headers);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await HandleResponse<TResponse>(response);
    }
    
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string baseUrl,
        string path,
        TRequest body,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var url = CombineUrl(baseUrl, path);
        _logger.LogInformation("PUT request to {url}", url);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json")
        };
        AddHeaders(request, headers);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await HandleResponse<TResponse>(response);
    }
    
    public async Task<bool> DeleteAsync(
        string baseUrl,
        string path,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var url = CombineUrl(baseUrl, path);
        _logger.LogInformation("DELETE request to {url}", url);
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(request, headers);
        
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
    
    private static string CombineUrl(string baseUrl, string path)
    {
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }
    
    private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers == null) return;

        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }
    
    private async Task<TResponse?> HandleResponse<TResponse>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Request failed with status {StatusCode}: {Error}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException(
                $"Request failed with status {response.StatusCode}: {errorContent}");
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
    }
}