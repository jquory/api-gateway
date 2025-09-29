namespace App.Shared.Models;

public class ServiceConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string HealthCheckPath { get; set; } = "/health";
    public int Timeout { get; set; } = 30;
    public List<string> Protocols { get; set; } = [];
}

public class GatewayConfiguration
{
    public Dictionary<string, ServiceConfiguration> Service { get; set; } = new();
}