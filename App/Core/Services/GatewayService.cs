using App.Core.Exceptions;
using App.Core.Interfaces;
using App.Shared.Constants;
using App.Shared.Models;
using GraphQL;
using Microsoft.Extensions.Options;

namespace App.Core.Services;

public class GatewayService(
    IRestClient restClient,
    IGraphQLClient graphqlClient,
    IGrpcClient grpcClient,
    IOptions<GatewayConfiguration> configuration,
    ILogger<GatewayService> logger)
    : IGatewayClient
{
    private readonly IGraphQLClient _graphqlClient = graphqlClient;
    private readonly IGrpcClient _grpcClient = grpcClient;
    private readonly ILogger<GatewayService> _logger = logger;
    private readonly GatewayConfiguration _configuration = configuration.Value;

    public async Task<T?> SendAsync<T>(
        string serviceName, 
        string path, 
        HttpMethod httpMethod, 
        object? body = null, 
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Gateway request to service {ServiceName}, path {Path}", serviceName, path);

        if (!_configuration.Service.TryGetValue(serviceName, out var serviceConfiguration))
        {
            throw new ServiceNotFoundException(serviceName);
        }
        
        var protocol = DetermineProtocol(serviceConfiguration, path);
        try
        {
            return protocol switch
            {
                ProtocolTypes.Rest => await HandleRestRequest<T>(serviceConfiguration, path, httpMethod, body, headers, cancellationToken),
                
                _ => throw new InvalidProtocolException(protocol, serviceName)
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while handling request to service {ServiceName}", serviceName);
            throw new ServiceUnavailableException(serviceName, e);
        }
    }

    private async Task<T?> HandleRestRequest<T>(
        ServiceConfiguration serviceConfiguration,
        string path,
        HttpMethod httpMethod,
        object? body,
        Dictionary<string, string>? headers,
        CancellationToken cancellationToken)
    {
        return httpMethod.Method.ToUpper() switch
        {
            "GET" => await restClient.GetAsync<T>(serviceConfiguration.BaseUrl, path, headers, cancellationToken),
            "PUT" => await restClient.PutAsync<object, T>(serviceConfiguration.BaseUrl, path, body!, headers,
                cancellationToken),
            "POST" => await restClient.PostAsync<object, T>(serviceConfiguration.BaseUrl, path, body!, headers,
                cancellationToken),
            "DELETE" => await restClient.DeleteAsync(serviceConfiguration.BaseUrl, path, headers, cancellationToken)
                ? default
                : throw new Exception("Delete operation failed"),
            _ => throw new InvalidOperationException($"Http Method {httpMethod} not supported"),
        };
    }

    private async Task<T?> HandleGqlRequest<T>(
        ServiceConfiguration serviceConfiguration,
        string path,
        object body,
        Dictionary<string, string>? headers,
        CancellationToken cancellationToken)
    {
        // Assuming body contains { query: "...", variables: {...} }
        if (body is not GraphQLRequest graphqlRequest)
        {
            throw new InvalidOperationException("GraphQL must provide query in request body");
        }

        if (graphqlRequest.Query.TrimStart().StartsWith("mutation", StringComparison.OrdinalIgnoreCase))
        {
            return await graphqlClient.MutationAsync<T>(
                serviceConfiguration.BaseUrl, 
                graphqlRequest.Query, 
                graphqlRequest.Variables, 
                headers, 
                cancellationToken);
        }
        
        return await graphqlClient.QueryAsync<T>(
            serviceConfiguration.BaseUrl, 
            graphqlRequest.Query, 
            graphqlRequest.Variables, 
            headers, 
            cancellationToken);
    }

    private async Task<T?> HandleGrpcRequest<T>(
        ServiceConfiguration serviceConfiguration,
        string path,
        object? body,
        Dictionary<string, string>? headers,
        CancellationToken cancellationToken) where T : class
    {
        if (body == null) throw new InvalidOperationException("gRPC request must provide a body request");
        
        var methodName = path.Split('/').Last();
        return await grpcClient.CallAsync<object, T>(
            serviceConfiguration.BaseUrl, 
            methodName, 
            body, 
            headers, 
            cancellationToken);
    }

    private static string DetermineProtocol(ServiceConfiguration configuration, string path)
    {
        if (path.Contains("/graphql", StringComparison.OrdinalIgnoreCase) &&
            configuration.Protocols.Contains(ProtocolTypes.GraphQl))
        {
            return ProtocolTypes.GraphQl;
        }

        if (configuration.Protocols.Contains(ProtocolTypes.Grpc) && path.StartsWith("/api/"))
        {
            return ProtocolTypes.Grpc;
        }

        return ProtocolTypes.Rest;
    }
}