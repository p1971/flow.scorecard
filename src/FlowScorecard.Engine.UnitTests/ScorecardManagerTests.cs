using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FlowScorecard.Engine.Models;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace FlowScorecard.Engine.UnitTests;

public sealed class ScorecardManagerTests
{
    [Fact]
    public async Task ExecuteSumsSignedDecimalScoresAndMapsMetadata()
    {
        List<string> executionOrder = [];
        Scorecard<Applicant> scorecard = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithName("Applicant score")
            .WithDescription("Applicant description")
            .WithVersion("2.0.0")
            .WithRule("R001", "Positive", applicant =>
            {
                executionOrder.Add("R001");
                return applicant.Age >= 18 ? 12.5m : 0m;
            })
            .WithRule("R002", "Penalty", applicant =>
            {
                executionOrder.Add("R002");
                return applicant.Income < 30_000m ? -2.25m : 0m;
            })
            .WithRule("R003", "No contribution", applicant =>
            {
                executionOrder.Add("R003");
                return 0m;
            })
            .Build();

        ScorecardManager<Applicant> manager = CreateManager(scorecard);
        Guid contextId = Guid.CreateVersion7();
        Applicant applicant = new("Alex", 30, 20_000m);

        ScorecardExecutionResult result = await manager.Execute(
            "correlation-id",
            contextId,
            applicant,
            CancellationToken.None);

        Assert.Equal(10.25m, result.TotalScore);
        Assert.True(result.Succeeded);
        Assert.Equal("S001", result.ScorecardId);
        Assert.Equal("Applicant score", result.ScorecardName);
        Assert.Equal("2.0.0", result.Version);
        Assert.Equal("correlation-id", result.CorrelationId);
        Assert.Equal(contextId, result.ExecutionContextId);
        Assert.Equal(["R001", "R002", "R003"], executionOrder);
        Assert.Equal([12.5m, -2.25m, 0m], result.RuleExecutionResults.Select(item => item.Score));
        Assert.All(result.RuleExecutionResults, item => Assert.True(item.Elapsed >= TimeSpan.Zero));
        Assert.All(result.RuleExecutionResults, item => Assert.True(item.Succeeded));
    }

    [Fact]
    public async Task ExecuteRecordsRuleExceptionAsZeroAndContinues()
    {
        bool finalRuleExecuted = false;
        Scorecard<Applicant> scorecard = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithName("Applicant score")
            .WithRule("R001", "First", _ => 5m)
            .WithRule("R002", "Broken", (Func<Applicant, decimal>)(_ => throw new InvalidOperationException("broken")))
            .WithRule("R003", "Final", _ =>
            {
                finalRuleExecuted = true;
                return -1m;
            })
            .Build();

        ScorecardExecutionResult result = await CreateManager(scorecard).Execute(
            "correlation-id",
            Guid.CreateVersion7(),
            new Applicant("Alex", 30, 20_000m),
            CancellationToken.None);

        Assert.True(finalRuleExecuted);
        Assert.Equal(4m, result.TotalScore);
        Assert.False(result.Succeeded);
        Assert.Equal(3, result.RuleExecutionResults.Count);

        ScoringRuleExecutionResult failedRule = result.RuleExecutionResults[1];
        Assert.Equal(0m, failedRule.Score);
        Assert.False(failedRule.Succeeded);
        InvalidOperationException exception = Assert.IsType<InvalidOperationException>(failedRule.Exception);
        Assert.Equal("broken", exception.Message);
    }

    [Fact]
    public async Task ExecutePropagatesCancellationAndDoesNotRunLaterRules()
    {
        bool laterRuleExecuted = false;
        using CancellationTokenSource source = new();

        Scorecard<Applicant> scorecard = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithName("Applicant score")
            .WithRule(
                "R001",
                "Cancel",
                new Func<Applicant, CancellationToken, Task<decimal>>(async (_, token) =>
                {
                    await source.CancelAsync();
                    token.ThrowIfCancellationRequested();
                    return 1m;
                }))
            .WithRule("R002", "Later", _ =>
            {
                laterRuleExecuted = true;
                return 2m;
            })
            .Build();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await CreateManager(scorecard).Execute(
                "correlation-id",
                Guid.CreateVersion7(),
                new Applicant("Alex", 30, 20_000m),
                source.Token));

        Assert.False(laterRuleExecuted);
    }

    [Fact]
    public async Task ExecuteRunsOneRuleById()
    {
        Scorecard<Applicant> scorecard = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithName("Applicant score")
            .WithRule("R001", "First", _ => 1m)
            .WithRule("R002", "Second", _ => -3.5m)
            .Build();

        ScoringRuleExecutionResult result = await CreateManager(scorecard).Execute(
            "R002",
            "correlation-id",
            Guid.CreateVersion7(),
            new Applicant("Alex", 30, 20_000m),
            CancellationToken.None);

        Assert.Equal("R002", result.Id);
        Assert.Equal(-3.5m, result.Score);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ExecuteOneRuleThrowsForUnknownId()
    {
        Scorecard<Applicant> scorecard = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithName("Applicant score")
            .WithRule("R001", "First", _ => 1m)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CreateManager(scorecard).Execute(
                "unknown",
                "correlation-id",
                Guid.CreateVersion7(),
                new Applicant("Alex", 30, 20_000m),
                CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteRejectsNullRequest()
    {
        Scorecard<Applicant> scorecard = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithName("Applicant score")
            .WithRule("R001", "First", _ => 1m)
            .Build();

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await CreateManager(scorecard).Execute(
                "correlation-id",
                Guid.CreateVersion7(),
                null!,
                CancellationToken.None));
    }

    private static ScorecardManager<Applicant> CreateManager(Scorecard<Applicant> scorecard) =>
        new(scorecard, NullLogger<ScorecardManager<Applicant>>.Instance);
}
