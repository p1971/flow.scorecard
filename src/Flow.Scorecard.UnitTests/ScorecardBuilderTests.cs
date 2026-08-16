using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FlowScorecard.Engine.Models;

using Xunit;

namespace FlowScorecard.Engine.UnitTests;

public sealed class ScorecardBuilderTests
{
    [Fact]
    public void BuildThrowsWhenIdIsMissing()
    {
        ScorecardBuilder<Applicant> builder = ScorecardBuilder<Applicant>.Create()
            .WithName("Applicant score")
            .WithRule("R001", "Age", applicant => applicant.Age);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Contains("WithId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildThrowsWhenNameIsMissing()
    {
        ScorecardBuilder<Applicant> builder = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithRule("R001", "Age", applicant => applicant.Age);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Contains("WithName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildThrowsWhenNoRulesExist()
    {
        ScorecardBuilder<Applicant> builder = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithName("Applicant score");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Contains("WithRule", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMapsMetadataAndRuleOverloads()
    {
        Scorecard<Applicant> scorecard = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithName("Applicant score")
            .WithDescription("Scores an applicant")
            .WithVersion("1.2.3")
            .WithRule("R001", "Synchronous", _ => 1m, "Sync description")
            .WithRule("R002", "Task", (_, _) => Task.FromResult(2m))
            .WithRule("R003", "ValueTask", (_, _) => ValueTask.FromResult(3m))
            .Build();

        Assert.Equal("S001", scorecard.Id);
        Assert.Equal("Applicant score", scorecard.Name);
        Assert.Equal("Scores an applicant", scorecard.Description);
        Assert.Equal("1.2.3", scorecard.Version);
        Assert.Equal(3, scorecard.Rules.Count);
        Assert.Equal("Sync description", scorecard.Rules[0].Description);
    }

    [Fact]
    public void BuildCreatesAnImmutableRuleSnapshot()
    {
        ScorecardBuilder<Applicant> builder = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithName("Applicant score")
            .WithRule("R001", "First", _ => 1m);

        Scorecard<Applicant> scorecard = builder.Build();
        builder.WithRule("R002", "Second", _ => 2m);

        ScoringRule<Applicant> rule = Assert.Single(scorecard.Rules);
        Assert.Equal("R001", rule.Id);
    }

    [Fact]
    public void ScorecardCreatesAnImmutableRuleSnapshot()
    {
        List<ScoringRule<Applicant>> rules =
        [
            new("R001", "First", null, (_, _) => ValueTask.FromResult(1m))
        ];

        Scorecard<Applicant> scorecard = new("S001", "Applicant score", null, rules);
        rules.Add(new ScoringRule<Applicant>("R002", "Second", null, (_, _) => ValueTask.FromResult(2m)));

        Assert.Single(scorecard.Rules);
    }

    [Fact]
    public void BuildRejectsDuplicateRuleIdsUsingOrdinalComparison()
    {
        ScorecardBuilder<Applicant> builder = ScorecardBuilder<Applicant>.Create()
            .WithId("S001")
            .WithName("Applicant score")
            .WithRule("R001", "First", _ => 1m)
            .WithRule("R001", "Duplicate", _ => 2m);

        ArgumentException exception = Assert.Throws<ArgumentException>(builder.Build);

        Assert.Contains("duplicate rule ids", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RuleRejectsInvalidIds(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new ScoringRule<Applicant>(id!, "Rule", null, (_, _) => ValueTask.FromResult(0m)));
    }

    [Fact]
    public void RuleRejectsNullSource()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ScoringRule<Applicant>(
                "R001",
                "Rule",
                null,
                (Func<Applicant, CancellationToken, ValueTask<decimal>>)null!));
    }
}
