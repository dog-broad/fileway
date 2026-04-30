using Fileway.Shared.Api;

namespace Fileway.Api.Jobs;

public abstract record DispatchResult;

public sealed record SyncDispatchResult(SyncJobResult Result) : DispatchResult;

public sealed record AsyncDispatchResult(AsyncJobAccepted Accepted) : DispatchResult;
