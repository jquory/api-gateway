namespace App.Shared.Models;

public class GatewayResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }

    public static GatewayResponse<T> SuccessResponse(T data, int statusCode = 200)
    {
        return new GatewayResponse<T>
        {
            Success = true,
            Data = data,
            StatusCode = statusCode
        };
    }

    public static GatewayResponse<T> ErrorResponse(string errorMessage, int statusCode = 500)
    {
        return new GatewayResponse<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            StatusCode = statusCode
        };
    }
}