namespace Fileway.Shared.Processors;

public interface IWasmProcessor : IApiProcessor
{
    bool CanHandleSize(long fileSizeBytes);
}
