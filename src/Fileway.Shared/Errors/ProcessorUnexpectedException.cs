namespace Fileway.Shared.Errors;

public sealed class ProcessorUnexpectedException : Exception
{
    public ProcessorUnexpectedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
