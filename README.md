# Flow.Scorecard

[![MIT](https://badgen.net/badge/license/MIT/green)](LICENSE)

A typed, fluent scorecard engine for .NET 10. 

## Concepts

A scorecard has an ID, name, optional description and version, and an ordered set of scoring rules. Each rule returns a `decimal` contribution:

- A positive value increases the total.
- A negative value applies a penalty.
- Zero means the rule made no contribution.

Rules run sequentially in registration order. A rule exception is captured in its result and contributes zero, while later rules continue to execute. Cancellation stops execution immediately and propagates `OperationCanceledException`; no partial scorecard result is returned.

## Getting started

Install the package:

```bash
dotnet add package Flow.Scorecard
```

Define the DTO and scorecard:

```csharp
public sealed record CustomerScoreRequest(
    int Age,
    decimal AnnualIncome,
    int YearsAtAddress,
    int MissedPayments);

Scorecard<CustomerScoreRequest> scorecard = ScorecardBuilder<CustomerScoreRequest>.Create()
    .WithId("CUSTOMER-001")
    .WithName("Customer suitability score")
    .WithVersion("1.0.0")
    .WithRule("AGE", "Age range", request => request.Age is >= 25 and <= 65 ? 20m : 0m)
    .WithRule("INCOME", "Income", request => request.AnnualIncome switch
    {
        >= 75_000m => 40m,
        >= 40_000m => 25m,
        _ => 0m,
    })
    .WithRule("STABILITY", "Address stability", request =>
        Math.Min(request.YearsAtAddress * 5m, 20m))
    .WithRule("MISSED", "Missed payments", request => request.MissedPayments * -15m)
    .Build();
```

Synchronous, `Task<decimal>`, and `ValueTask<decimal>` rules are supported. Asynchronous rules also receive a cancellation token.

## Dependency injection and execution

Register one or more scorecards. Registrations are keyed by scorecard ID, allowing the same DTO type to have multiple scorecards:

```csharp
services.AddFlowScorecard<CustomerScoreRequest>(() => scorecard);
services.AddFlowScorecardRegistry();
```

Execute a known scorecard through its typed manager:

```csharp
IScorecardManager<CustomerScoreRequest> manager =
    serviceProvider.GetRequiredKeyedService<IScorecardManager<CustomerScoreRequest>>("CUSTOMER-001");

string correlationId =
    Activity.Current?.TraceId.ToString()
    ?? Guid.CreateVersion7().ToString();

Guid executionContextId = Guid.CreateVersion7();

ScorecardExecutionResult result = await manager.Execute(
    correlationId,
    executionContextId,
    request,
    cancellationToken);
```

Or dispatch by ID through the registry, including across different DTO types:

```csharp
ScorecardExecutionResult result = await registry.ExecuteAsync(
    "CUSTOMER-001",
    correlationId,
    executionContextId,
    request,
    cancellationToken);
```

## Acting on a score

Flow.Scorecard deliberately has no score-band or action model. Keep those application decisions with the caller:

```csharp
string nextAction = result.TotalScore switch
{
    >= 70m => "Proceed",
    >= 40m => "Refer for review",
    _ => "Stop",
};
```

Check `result.Succeeded` before trusting the score as complete. Each `ScoringRuleExecutionResult` includes the rule's score, elapsed time, success state, metadata, and any captured exception.

## Building and testing

```bash
dotnet build src/Flow.Scorecard.slnx
dotnet test src/Flow.Scorecard.slnx
dotnet run --project src/Flow.Scorecard.Samples.Console
```

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) before opening an issue or pull request.

## License

MIT License. See [LICENSE](LICENSE).
