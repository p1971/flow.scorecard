using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FlowScorecard.Engine.Models;

namespace FlowScorecard.Engine.Interfaces;

/// <summary>
/// Dispatches registered scorecards by identifier across DTO types.
/// </summary>
public interface IScorecardRegistry
{
    /// <summary>
    /// Gets the registered scorecard identifiers.
    /// </summary>
    IReadOnlyList<string> ScorecardIds { get; }

    /// <summary>
    /// Executes a registered scorecard with a typed DTO.
    /// </summary>
    /// <typeparam name="T">The DTO type evaluated by the scorecard.</typeparam>
    /// <param name="scorecardId">The registered scorecard identifier.</param>
    /// <param name="correlationId">The caller-provided correlation identifier.</param>
    /// <param name="executionContextId">The caller-provided execution context identifier.</param>
    /// <param name="request">The DTO to score.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The complete scorecard result.</returns>
    ValueTask<ScorecardExecutionResult> ExecuteAsync<T>(
        string scorecardId,
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken)
        where T : class;
}
