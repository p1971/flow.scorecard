using System;
using System.Threading;
using System.Threading.Tasks;

using FlowScorecard.Engine.Extensions;
using FlowScorecard.Engine.Interfaces;
using FlowScorecard.Engine.Models;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace FlowScorecard.Engine.UnitTests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task RegistrationSupportsMultipleScorecardsForOneDto()
    {
        ServiceCollection services = new();
        services.AddFlowScorecard<Applicant>(() => CreateApplicantScorecard("first", 1m));
        services.AddFlowScorecard<Applicant>(() => CreateApplicantScorecard("second", 2m));
        services.AddFlowScorecardRegistry();

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        IScorecardManager<Applicant> first =
            scope.ServiceProvider.GetRequiredKeyedService<IScorecardManager<Applicant>>("first");
        IScorecardManager<Applicant> second =
            scope.ServiceProvider.GetRequiredKeyedService<IScorecardManager<Applicant>>("second");
        IScorecardManager<Applicant> unkeyed =
            scope.ServiceProvider.GetRequiredService<IScorecardManager<Applicant>>();

        Applicant applicant = new("Alex", 30, 20_000m);
        Assert.Equal(1m, (await Execute(first, applicant)).TotalScore);
        Assert.Equal(2m, (await Execute(second, applicant)).TotalScore);
        Assert.Equal(2m, (await Execute(unkeyed, applicant)).TotalScore);
    }

    [Fact]
    public async Task RegistryDispatchesAcrossDtoTypes()
    {
        ServiceCollection services = new();
        services.AddFlowScorecard<Applicant>(() => CreateApplicantScorecard("applicant", 3m));
        services.AddFlowScorecard<Order>(() => ScorecardBuilder<Order>.Create()
            .WithId("order")
            .WithName("Order")
            .WithRule("R001", "Value", order => order.Value)
            .Build());
        services.AddFlowScorecardRegistry();

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IScorecardRegistry registry = scope.ServiceProvider.GetRequiredService<IScorecardRegistry>();

        ScorecardExecutionResult applicantResult = await registry.ExecuteAsync(
            "applicant",
            "correlation-id",
            Guid.CreateVersion7(),
            new Applicant("Alex", 30, 20_000m),
            CancellationToken.None);
        ScorecardExecutionResult orderResult = await registry.ExecuteAsync(
            "order",
            "correlation-id",
            Guid.CreateVersion7(),
            new Order(42m),
            CancellationToken.None);

        Assert.Equal(3m, applicantResult.TotalScore);
        Assert.Equal(42m, orderResult.TotalScore);
        Assert.Equal(["applicant", "order"], registry.ScorecardIds);
    }

    [Fact]
    public void RegistryRejectsDuplicateIdsAcrossDtoTypes()
    {
        ServiceCollection services = new();
        services.AddFlowScorecard<Applicant>(() => CreateApplicantScorecard("duplicate", 1m));
        services.AddFlowScorecard<Order>(() => ScorecardBuilder<Order>.Create()
            .WithId("duplicate")
            .WithName("Order")
            .WithRule("R001", "Value", order => order.Value)
            .Build());
        services.AddFlowScorecardRegistry();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.Throws<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IScorecardRegistry>());
    }

    [Fact]
    public async Task RegistryRejectsWrongDtoTypeAndUnknownId()
    {
        ServiceCollection services = new();
        services.AddFlowScorecard<Applicant>(() => CreateApplicantScorecard("applicant", 1m));
        services.AddFlowScorecardRegistry();

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IScorecardRegistry registry = scope.ServiceProvider.GetRequiredService<IScorecardRegistry>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await registry.ExecuteAsync(
                "applicant",
                "correlation-id",
                Guid.CreateVersion7(),
                new Order(10m),
                CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await registry.ExecuteAsync(
                "unknown",
                "correlation-id",
                Guid.CreateVersion7(),
                new Applicant("Alex", 30, 20_000m),
                CancellationToken.None));
    }

    private static Scorecard<Applicant> CreateApplicantScorecard(string id, decimal score) =>
        ScorecardBuilder<Applicant>.Create()
            .WithId(id)
            .WithName(id)
            .WithRule("R001", "Score", _ => score)
            .Build();

    private static ValueTask<ScorecardExecutionResult> Execute(
        IScorecardManager<Applicant> manager,
        Applicant applicant) =>
        manager.Execute("correlation-id", Guid.CreateVersion7(), applicant, CancellationToken.None);
}
