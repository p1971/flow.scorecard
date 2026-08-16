using System;
using System.Threading;
using System.Threading.Tasks;

using FlowScorecard.Engine.Models;

namespace FlowScorecard.Engine.Interfaces;

/// <summary>
/// Executes a scorecard for a specific DTO type.
/// </summary>
/// <typeparam name="T">The DTO type evaluated by the scorecard.</typeparam>
public interface IScorecardManager<in T>
    where T : class
{
    /// <summary>
    /// Executes every rule in the scorecard.
    /// </summary>
    /// <param name="correlationId">The caller-provided correlation identifier.</param>
    /// <param name="executionContextId">The caller-provided execution context identifier.</param>
    /// <param name="request">The DTO to score.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The complete scorecard result.</returns>
    ValueTask<ScorecardExecutionResult> Execute(
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes one rule from the scorecard.
    /// </summary>
    /// <param name="ruleId">The identifier of the rule to execute.</param>
    /// <param name="correlationId">The caller-provided correlation identifier.</param>
    /// <param name="executionContextId">The caller-provided execution context identifier.</param>
    /// <param name="request">The DTO to score.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The individual rule result.</returns>
    ValueTask<ScoringRuleExecutionResult> Execute(
        string ruleId,
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken);
}
