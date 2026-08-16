using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FlowScorecard.Engine.Interfaces;
using FlowScorecard.Engine.Models;

using Microsoft.Extensions.Logging;

namespace FlowScorecard.Engine;

internal sealed class ScorecardManager<T>(
    Scorecard<T> scorecard,
    ILogger<ScorecardManager<T>> logger) : IScorecardManager<T>
    where T : class
{
    private readonly Lazy<IReadOnlyDictionary<string, ScoringRule<T>>> _rulesById =
        new(() => scorecard.Rules.ToDictionary(rule => rule.Id, StringComparer.Ordinal));

    public async ValueTask<ScorecardExecutionResult> Execute(
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogScorecardStarted(scorecard.Id, scorecard.Name, executionContextId);

        ScoringRuleExecutionResult[] ruleResults = new ScoringRuleExecutionResult[scorecard.Rules.Count];
        decimal totalScore = 0m;
        bool succeeded = true;

        for (int index = 0; index < scorecard.Rules.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ScoringRuleExecutionResult result = await ExecuteRule(
                scorecard.Rules[index],
                executionContextId,
                request,
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            ruleResults[index] = result;
            totalScore += result.Score;
            succeeded &= result.Succeeded;
        }

        return new ScorecardExecutionResult
        {
            ScorecardId = scorecard.Id,
            ScorecardName = scorecard.Name,
            Version = scorecard.Version,
            ExecutionContextId = executionContextId,
            CorrelationId = correlationId,
            TotalScore = totalScore,
            Succeeded = succeeded,
            RuleExecutionResults = ruleResults
        };
    }

    public async ValueTask<ScoringRuleExecutionResult> Execute(
        string ruleId,
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        ScoringRule<T> rule = _rulesById.Value.TryGetValue(ruleId, out ScoringRule<T>? registeredRule)
            ? registeredRule
            : throw new InvalidOperationException($"No rule with id [{ruleId}] was found.");

        ScoringRuleExecutionResult result = await ExecuteRule(
            rule,
            executionContextId,
            request,
            cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Rule failures are part of the result contract and must not prevent later rules from executing.")]
    private async ValueTask<ScoringRuleExecutionResult> ExecuteRule(
        ScoringRule<T> rule,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken)
    {
        logger.LogRuleStarted(rule.Id, rule.Name, executionContextId);

        long startTimestamp = TimeProvider.System.GetTimestamp();
        decimal score = 0m;
        Exception? exception = null;

        try
        {
            ValueTask<decimal> execution = rule.Source(request, cancellationToken);
            score = execution.IsCompletedSuccessfully
                ? execution.Result
                : await execution.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            exception = ex;
            logger.LogRuleFailed(ex, rule.Id, rule.Name);
        }

        TimeSpan elapsed = TimeProvider.System.GetElapsedTime(startTimestamp);

        return new ScoringRuleExecutionResult(
            rule.Id,
            rule.Name,
            score,
            exception is null,
            elapsed,
            rule.Description,
            exception);
    }
}
