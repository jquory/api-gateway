namespace App.Core.Exceptions;

public class GatewayException(string message, int statusCode = 500) : Exception(message)
{
    public int StatusCode { get; set; } = statusCode;
}

public class ServiceUnavailableException: GatewayException
{
    public ServiceUnavailableException(string serviceName, Exception? innerException = null) 
        : base($"Service '{serviceName}' is currently not available", 503){}
}

public class ServiceNotFoundException: GatewayException
{
    public ServiceNotFoundException(string serviceName) 
        : base($"Service '{serviceName}' is not found", 404){}
}

public class InvalidProtocolException: GatewayException
{
    public InvalidProtocolException(string protocolName, string serviceName) 
        : base($"Protocol '{protocolName}' is not supported by '{serviceName}'", 400){}
}

public class TimeoutException: GatewayException
{
    public TimeoutException(string serviceName) 
        : base($"Service '{serviceName}' timed out", 408){}
}
