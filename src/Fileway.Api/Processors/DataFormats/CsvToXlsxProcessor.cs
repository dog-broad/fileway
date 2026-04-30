using System.Text.Json;
using Fileway.Shared.Errors;
using Fileway.Shared.Processors;

namespace Fileway.Api.Processors.DataFormats;

public sealed class CsvToXlsxProcessor : IApiProcessor
{
    public void ValidateOptions(JsonElement toolOptions)
    {
        // csv-to-xlsx has no tool options to validate
    }

    public Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("CsvToXlsxProcessor is implemented in the Data Format Processors section.");
    }
}
