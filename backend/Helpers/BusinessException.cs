namespace GiaPhuWaterDelivery.Api.Helpers;

public sealed class BusinessException(int statusCode, string message, string? code = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string? Code { get; } = code;
}
