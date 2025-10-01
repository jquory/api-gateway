using App.Core.Interfaces;
using App.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers;

[ApiController]
[Route("gateway")]
public class GatewayController(IGatewayClient gatewayClient, ILogger<GatewayController> logger): ControllerBase
{
    [HttpGet("{serviceName}/{**path}")]
    public async Task<IActionResult> Get(string serviceName, string path)
    {
        logger.LogInformation("Get request to {ServiceName}/{Path}", serviceName, path);

        var headers = ExtractHeaders();
        var result = await gatewayClient.SendAsync<object>(
            serviceName,
            path,
            HttpMethod.Get,
            headers: headers);
        
        return Ok(GatewayResponse<object>.SuccessResponse(result));
    }

    [HttpPost("{serviceName}/{**path}")]
    public async Task<IActionResult> Post(string serviceName, string path, [FromBody] object body)
    {
        logger.LogInformation("Post request to {ServiceName}/{Path}", serviceName, path);
        
        var headers = ExtractHeaders();
        var result = await gatewayClient.SendAsync<object>(
            serviceName,
            path,
            HttpMethod.Post,
            body,
            headers);
        
        return Ok(GatewayResponse<object>.SuccessResponse(result));
    }

    [HttpPut("{serviceName}/{**path}")]
    public async Task<IActionResult> Put(string serviceName, string path, [FromBody] object body)
    {
        logger.LogInformation("Put request to {ServiceName}/{Path}", serviceName, path);
        
        var headers = ExtractHeaders();
        var result = await gatewayClient.SendAsync<object>(
            serviceName,
            path,
            HttpMethod.Put,
            body,
            headers);
        
        return Ok(GatewayResponse<object>.SuccessResponse(result));
    }

    [HttpDelete("{serviceName}/{**path}")]
    public async Task<IActionResult> Delete(string serviceName, string path)
    {
        logger.LogInformation("Delete request to {ServiceName}/{Path}", serviceName, path);
        
        var headers = ExtractHeaders();
        await gatewayClient.SendAsync<object>(
            serviceName,
            path,
            HttpMethod.Delete,
            headers);
        
        return Ok(GatewayResponse<object>.SuccessResponse(new { Message = "Delete successfully" }));
    }

    private Dictionary<string, string> ExtractHeaders()
    {
        var headers = new Dictionary<string, string>();

        foreach (var header in Request.Headers)
        {
            if (ShouldForwardHeader(header.Key))
            {
                headers[header.Key] = header.Value.ToString();
            }
        }
        
        return headers;
    }

    private static bool ShouldForwardHeader(string headerName)
    {
        var excludeHeader = new[]
        {
            "Host", "Connection", "Keep-Alive", "Transfer-Encoding",
            "Upgrade", "Proxy-Connection", "Proxy-Authenticate", "Proxy-Authorization"
        };
        
        return !excludeHeader.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }
}