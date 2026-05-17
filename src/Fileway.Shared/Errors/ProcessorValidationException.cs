namespace Fileway.Shared.Errors;

public sealed class ProcessorValidationException : Exception
{
    public string ErrorCode { get; }

    public ProcessorValidationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
