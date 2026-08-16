using System;

using Microsoft.Extensions.Logging;

namespace FlowScorecard.Engine;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Executing scorecard {ScorecardId} ({ScorecardName}) with context {ExecutionContextId}.")]
    public static partial void LogScorecardStarted(
        this ILogger logger,
        string scorecardId,
        string scorecardName,
        Guid executionContextId);

    [LoggerMessage(2, LogLevel.Debug, "Executing scoring rule {RuleId} ({RuleName}) with context {ExecutionContextId}.")]
    public static partial void LogRuleStarted(
        this ILogger logger,
        string ruleId,
        string ruleName,
        Guid executionContextId);

    [LoggerMessage(3, LogLevel.Error, "Scoring rule {RuleId} ({RuleName}) failed.")]
    public static partial void LogRuleFailed(
        this ILogger logger,
        Exception exception,
        string ruleId,
        string ruleName);
}
