namespace Fileway.Api.Jobs;

public sealed class JobDispatchException(int httpStatusCode, string errorCode, string message)
    : Exception(message)
{
    public int HttpStatusCode { get; } = httpStatusCode;
    public string ErrorCode { get; } = errorCode;
}
