using System.Text;
using System.Text.Json;
using App.Core.Interfaces;

namespace App.Infrastructure.Clients.GraphQL;

public class GraphQLClient(HttpClient client, ILogger<GraphQLClient> logger) : IGraphQLClient
{
    private readonly HttpClient _client = client;
    private readonly ILogger<GraphQLClient> _logger = logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TResponse?> QueryAsync<TResponse>(
        string baseUrl,
        string query,
        object? variables,
        Dictionary<string, string>? header, CancellationToken cancellationToken)
    {
        return await ExecuteGraphQLAsync<TResponse>(baseUrl, query, variables, header, cancellationToken);
    }
    
    public async Task<TResponse?> MutationAsync<TResponse>(
        string baseUrl,
        string query,
        object? variables,
        Dictionary<string, string>? header, CancellationToken cancellationToken)
    {
        return await ExecuteGraphQLAsync<TResponse>(baseUrl, query, variables, header, cancellationToken);
    }

    private async Task<TResponse?> ExecuteGraphQLAsync<TResponse>(
        string baseUrl,
        string query,
        object? variables,
        Dictionary<string, string>? header,
        CancellationToken cancellationToken)
    {
        var url = $"{baseUrl.TrimEnd('/')}/graphql";
        _logger.LogInformation("GraphQL request to {Url}", url);

        var requestBody = new
        {
            query = query,
            variables = variables,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, _jsonSerializerOptions), Encoding.UTF8, "application/json")
        };
        
        AddHeaders(request, header);
        
        var response = await _client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("GraphQL request failed with status code {StatusCode}: {Error}", response.StatusCode, errorContent);
            throw new HttpRequestException($"GraphQL request failed with status code {response.StatusCode}: {errorContent}");
        }
        
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var graphQlResponse = JsonSerializer.Deserialize<GraphQLRespons<TResponse>>(responseContent, _jsonSerializerOptions);

        if (graphQlResponse?.Errors != null && graphQlResponse.Errors.Any())
        {
            var errors = string.Join(", ", graphQlResponse.Errors.Select(e => e.Message));
            _logger.LogError("GraphQL request failed with errors: {Errors}", errors);
            throw new HttpRequestException($"GraphQL request failed with errors: {errors}");
        }
        
        return graphQlResponse.Data;
    }
    
    private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers == null) return;

        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private class GraphQLRespons<T>
    {
        public T? Data { get; set; }
        public List<GraphQLError>? Errors { get; set; }
    }

    private class GraphQLError
    {
        public string Message { get; set; } = string.Empty;
        public List<GraphQLErrorLocation>? Locations { get; set; }
        public List<object>? Path { get; set; }
    }

    private class GraphQLErrorLocation
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }
}