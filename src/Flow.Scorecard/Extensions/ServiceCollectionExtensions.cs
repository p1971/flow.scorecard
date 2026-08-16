using System;

using FlowScorecard.Engine.Interfaces;
using FlowScorecard.Engine.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowScorecard.Engine.Extensions;

/// <summary>
/// Registers Flow.Scorecard services with a dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a scorecard and its manager.
    /// </summary>
    /// <typeparam name="T">The DTO type evaluated by the scorecard.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="scorecardFactory">Creates the immutable scorecard definition.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>
    /// Multiple scorecards for the same DTO type are keyed by scorecard identifier.
    /// Unkeyed <see cref="IScorecardManager{T}"/> resolution returns the last registration.
    /// </remarks>
    public static IServiceCollection AddFlowScorecard<T>(
        this IServiceCollection services,
        Func<Scorecard<T>> scorecardFactory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(scorecardFactory);

        Scorecard<T> scorecard = scorecardFactory();
        ArgumentNullException.ThrowIfNull(scorecard);

        string scorecardId = scorecard.Id;
        services.AddKeyedSingleton<Scorecard<T>>(scorecardId, (_, _) => scorecard);
        services.AddKeyedScoped<IScorecardManager<T>>(scorecardId, (serviceProvider, _) =>
        {
            Scorecard<T> definition = serviceProvider.GetRequiredKeyedService<Scorecard<T>>(scorecardId);
            ILogger<ScorecardManager<T>> logger = serviceProvider.GetService<ILogger<ScorecardManager<T>>>()
                ?? NullLogger<ScorecardManager<T>>.Instance;

            return new ScorecardManager<T>(definition, logger);
        });

        services.AddScoped<IScorecardManager<T>>(serviceProvider =>
            serviceProvider.GetRequiredKeyedService<IScorecardManager<T>>(scorecardId));

        services.AddScoped<IScorecardRegistryEntry>(serviceProvider =>
        {
            IScorecardManager<T> manager =
                serviceProvider.GetRequiredKeyedService<IScorecardManager<T>>(scorecardId);
            return new ScorecardRegistryEntry<T>(scorecardId, manager);
        });

        return services;
    }

    /// <summary>
    /// Registers the ID-based scorecard registry after scorecards have been registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddFlowScorecardRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<IScorecardRegistry, ScorecardRegistry>();
        return services;
    }
}
