using App.API.Middlewares;
using App.Core.Interfaces;
using App.Core.Services;
using App.Infrastructure.Clients.GraphQL;
using App.Infrastructure.Clients.Grpc;
using App.Infrastructure.Clients.Rest;
using App.Shared.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<GatewayConfiguration>(
    builder.Configuration.GetSection("Gateway"));
builder.Services.AddHttpClient<IRestClient, RestClient>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
builder.Services.AddHttpClient<IGraphQLClient, GraphQLClient>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
builder.Services.AddSingleton<IGrpcClient, GrpcClientBase>();
builder.Services.AddScoped<IGatewayClient, GatewayService>();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();
builder.Services.AddCors(option =>
{
    option.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(option =>
{
    option.EnableForHttps = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseResponseCompression();
app.UseAuthorization();
app.MapControllers();
app.MapReverseProxy();
app.MapHealthChecks("/health");

Log.Information("Starting API Gateway");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terimated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Polly retry policy
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Logger.Warning(
                    "Retry {RetryCount} after delay {Delay}s due to {Result}",
                    retryCount, timespan.TotalSeconds, outcome.Result.StatusCode);
            });
}

// Polly circuit breaker
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
            onBreak: (outcome, timespan) =>
            {
                Log.Error("Circuit breaker is open for {Duration}", timespan.TotalSeconds);
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker is reset");
            });
}