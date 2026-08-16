using System;
using System.Collections.Generic;

namespace FlowScorecard.Engine.Models;

/// <summary>
/// Represents the complete result of executing a scorecard.
/// </summary>
public sealed class ScorecardExecutionResult
{
    /// <summary>
    /// Gets the scorecard identifier.
    /// </summary>
    public required string ScorecardId { get; init; }

    /// <summary>
    /// Gets the scorecard name.
    /// </summary>
    public required string ScorecardName { get; init; }

    /// <summary>
    /// Gets the optional scorecard version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the execution context identifier supplied by the caller.
    /// </summary>
    public Guid ExecutionContextId { get; init; }

    /// <summary>
    /// Gets the correlation identifier supplied by the caller.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Gets the sum of all successful rule contributions.
    /// </summary>
    public decimal TotalScore { get; init; }

    /// <summary>
    /// Gets a value indicating whether every rule completed successfully.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the ordered individual rule results.
    /// </summary>
    public IReadOnlyList<ScoringRuleExecutionResult> RuleExecutionResults { get; init; } = [];
}
