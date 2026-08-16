using System;
using System.Diagnostics;
using System.Threading;

using FlowScorecard.Engine;
using FlowScorecard.Engine.Extensions;
using FlowScorecard.Engine.Interfaces;
using FlowScorecard.Engine.Models;

using Microsoft.Extensions.DependencyInjection;

Scorecard<CustomerScoreRequest> customerScorecard = ScorecardBuilder<CustomerScoreRequest>.Create()
    .WithId("CUSTOMER-001")
    .WithName("Customer suitability score")
    .WithDescription("Illustrates positive, negative, and zero scoring rules.")
    .WithVersion("1.0.0")
    .WithRule("AGE", "Age range", request => request.Age is >= 25 and <= 65 ? 20m : 0m)
    .WithRule("INCOME", "Income", request => request.AnnualIncome switch
    {
        >= 75_000m => 40m,
        >= 40_000m => 25m,
        _ => 0m,
    })
    .WithRule("STABILITY", "Address stability", request => Math.Min(request.YearsAtAddress * 5m, 20m))
    .WithRule("MISSED", "Missed payments", request => request.MissedPayments * -15m)
    .Build();

ServiceCollection services = new();
services.AddFlowScorecard<CustomerScoreRequest>(() => customerScorecard);
services.AddFlowScorecardRegistry();

await using ServiceProvider provider = services.BuildServiceProvider();
await using AsyncServiceScope scope = provider.CreateAsyncScope();

IScorecardRegistry registry = scope.ServiceProvider.GetRequiredService<IScorecardRegistry>();
CustomerScoreRequest customer = new(35, 55_000m, 3, 1);

ScorecardExecutionResult result = await registry.ExecuteAsync(
    "CUSTOMER-001",
    Activity.Current?.TraceId.ToString() ?? Guid.CreateVersion7().ToString(),
    Guid.CreateVersion7(),
    customer,
    CancellationToken.None);

foreach (ScoringRuleExecutionResult ruleResult in result.RuleExecutionResults)
{
    Console.WriteLine($"Rule {ruleResult.Id} ({ruleResult.Name}) scored {ruleResult.Score}");
}

string nextAction = result.TotalScore switch
{
    >= 70m => "Proceed",
    >= 40m => "Refer for review",
    _ => "Stop",
};

Console.WriteLine($"Score: {result.TotalScore}; next action: {nextAction}");

internal sealed record CustomerScoreRequest(
    int Age,
    decimal AnnualIncome,
    int YearsAtAddress,
    int MissedPayments);
