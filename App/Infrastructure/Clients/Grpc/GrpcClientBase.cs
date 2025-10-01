using App.Core.Interfaces;
using Grpc.Core;
using Grpc.Net.Client;

namespace App.Infrastructure.Clients.Grpc;

public class GrpcClientBase(ILogger<GrpcClientBase> logger) : IGrpcClient
{
    private readonly ILogger<GrpcClientBase> _logger = logger;
    private readonly Dictionary<string, GrpcChannel> _channels = new();

    public async Task<TResponse> CallAsync<TRequest, TResponse>(
        string baseUrl,
        string methodName,
        TRequest request,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        logger.LogInformation("gRPC to call {BaseUrl}/{MethodName}", baseUrl, methodName);
        
        var channel = GetOrCreateChannel(baseUrl);

        try
        {
            var metadata = CreateMetadata(headers);
            
            logger.LogInformation("Calling grpc successfully");

            throw new NotImplementedException("gRPC client requires service-specific implementation with generated stubs from .proto files");
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC call failed: {Status}", ex.Status);
            throw new HttpRequestException($"gRPC call failed: {ex.Status.Detail}", ex);
        }
    }

    private GrpcChannel GetOrCreateChannel(string baseUrl)
    {
        if (_channels.TryGetValue(baseUrl, out var existingChannel))
        {
            return existingChannel;
        }
        
        var channel = GrpcChannel.ForAddress(baseUrl, new GrpcChannelOptions()
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            }
        });
        
        _channels[baseUrl] = channel;
        return channel;
    }

    private static Metadata CreateMetadata(IDictionary<string, string>? headers)
    {
        var metadata = new Metadata();

        if (headers == null) return metadata;
        foreach (var header in headers)
        {
            metadata.Add(header.Key, header.Value);
        }

        return metadata;
    }
}