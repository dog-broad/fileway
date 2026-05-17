namespace Fileway.Shared.Errors;

public sealed class ProcessorDomainException : Exception
{
    public string ErrorCode { get; }

    public ProcessorDomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public ProcessorDomainException(string errorCode, string message, Exception inner)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}
