using App.Core.Interfaces;
using App.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers;

[ApiController]
[Route("gateway")]
public class GatewayController(IGatewayClient gatewayClient, ILogger<GatewayController> logger): ControllerBase
{
    [HttpGet("{servicename}/{**path}")]
    public async Task<IActionResult> Get(string servicename, string path)
    {
        logger.LogInformation("Get request to {ServiceName}/{Path}", servicename, path);

        var headers = ExtractHeaders();
        var result = await gatewayClient.SendAsync<object>(
            servicename,
            path,
            HttpMethod.Get,
            headers: headers);
        
        return Ok(GatewayResponse<object>.SuccessResponse(result));
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