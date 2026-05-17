using Fileway.Shared.Processors;

namespace Fileway.Tests.Client.Fixtures;

/// <summary>
/// Implements IProgress<ProcessorProgressEvent> and stores every reported event.
/// Use for all progress-related assertions in processor tests.
/// </summary>
public sealed class TestProgressCollector : IProgress<ProcessorProgressEvent>
{
    private readonly List<ProcessorProgressEvent> _events = [];

    public IReadOnlyList<ProcessorProgressEvent> Events => _events;

    public void Report(ProcessorProgressEvent value) => _events.Add(value);
}
