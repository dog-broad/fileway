using System.Text.Json;

namespace Fileway.Shared.Processors;

public interface IApiProcessor
{
    void ValidateOptions(JsonElement toolOptions);
    Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken cancellationToken);
}
