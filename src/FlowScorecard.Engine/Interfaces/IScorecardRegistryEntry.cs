using System;
using System.Threading;
using System.Threading.Tasks;

using FlowScorecard.Engine.Models;

namespace FlowScorecard.Engine.Interfaces;

/// <summary>
/// Provides type-erased scorecard metadata for registry construction.
/// </summary>
internal interface IScorecardRegistryEntry
{
    /// <summary>
    /// Gets the scorecard identifier.
    /// </summary>
    string ScorecardId { get; }

    /// <summary>
    /// Gets the DTO type evaluated by the scorecard.
    /// </summary>
    Type RequestType { get; }
}

/// <summary>
/// Executes a registry entry with its typed DTO.
/// </summary>
/// <typeparam name="T">The DTO type evaluated by the scorecard.</typeparam>
internal interface IScorecardRegistryEntry<in T> : IScorecardRegistryEntry
    where T : class
{
    /// <summary>
    /// Executes the scorecard entry.
    /// </summary>
    /// <param name="correlationId">The caller-provided correlation identifier.</param>
    /// <param name="executionContextId">The caller-provided execution context identifier.</param>
    /// <param name="request">The DTO to score.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The complete scorecard result.</returns>
    ValueTask<ScorecardExecutionResult> ExecuteAsync(
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken);
}
