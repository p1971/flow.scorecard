using System;
using System.Threading;
using System.Threading.Tasks;

using FlowScorecard.Engine.Interfaces;
using FlowScorecard.Engine.Models;

namespace FlowScorecard.Engine;

internal sealed class ScorecardRegistryEntry<T>(
    string scorecardId,
    IScorecardManager<T> manager) : IScorecardRegistryEntry<T>
    where T : class
{
    public string ScorecardId { get; } = scorecardId;

    public Type RequestType { get; } = typeof(T);

    public ValueTask<ScorecardExecutionResult> ExecuteAsync(
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken) =>
        manager.Execute(correlationId, executionContextId, request, cancellationToken);
}
