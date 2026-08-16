using System;

namespace FlowScorecard.Engine.Models;

/// <summary>
/// Represents the outcome of executing one scoring rule.
/// </summary>
/// <param name="Id">The rule identifier.</param>
/// <param name="Name">The rule name.</param>
/// <param name="Score">The score contributed by the rule.</param>
/// <param name="Succeeded">Whether the rule completed without an exception.</param>
/// <param name="Elapsed">The rule execution duration.</param>
/// <param name="Description">The optional rule description.</param>
/// <param name="Exception">The exception captured from the rule, when unsuccessful.</param>
public sealed record ScoringRuleExecutionResult(
    string Id,
    string Name,
    decimal Score,
    bool Succeeded,
    TimeSpan Elapsed,
    string? Description,
    Exception? Exception);
