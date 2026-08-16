using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FlowScorecard.Engine.Interfaces;
using FlowScorecard.Engine.Models;

namespace FlowScorecard.Engine;

internal sealed class ScorecardRegistry : IScorecardRegistry
{
    private readonly Dictionary<string, IScorecardRegistryEntry> _entries = new(StringComparer.Ordinal);

    public ScorecardRegistry(IEnumerable<IScorecardRegistryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (IScorecardRegistryEntry entry in entries)
        {
            if (!_entries.TryAdd(entry.ScorecardId, entry))
            {
                throw new InvalidOperationException(
                    $"A scorecard with id '{entry.ScorecardId}' has already been registered. " +
                    "Scorecard ids must be unique across the registry.");
            }
        }
    }

    public IReadOnlyList<string> ScorecardIds => [.. _entries.Keys];

    public ValueTask<ScorecardExecutionResult> ExecuteAsync<T>(
        string scorecardId,
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scorecardId);

        if (!_entries.TryGetValue(scorecardId, out IScorecardRegistryEntry? entry))
        {
            throw new InvalidOperationException(
                $"No scorecard with id '{scorecardId}' has been registered. " +
                $"Registered scorecards: [{string.Join(", ", _entries.Keys.Order())}].");
        }

        if (entry is not IScorecardRegistryEntry<T> typedEntry)
        {
            throw new InvalidOperationException(
                $"Scorecard '{scorecardId}' operates on '{entry.RequestType.Name}', not '{typeof(T).Name}'.");
        }

        return typedEntry.ExecuteAsync(correlationId, executionContextId, request, cancellationToken);
    }
}
