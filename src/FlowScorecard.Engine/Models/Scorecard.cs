using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowScorecard.Engine.Models;

/// <summary>
/// Represents a scorecard containing an ordered set of scoring rules.
/// </summary>
/// <typeparam name="T">The DTO type evaluated by the scorecard.</typeparam>
public sealed class Scorecard<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Scorecard{T}"/> class.
    /// </summary>
    /// <param name="id">The scorecard identifier.</param>
    /// <param name="name">The scorecard name.</param>
    /// <param name="description">An optional scorecard description.</param>
    /// <param name="rules">The ordered scoring rules.</param>
    /// <param name="version">An optional scorecard version.</param>
    public Scorecard(
        string id,
        string name,
        string? description,
        IList<ScoringRule<T>> rules,
        string? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(rules);

        if (rules.Count == 0)
        {
            throw new ArgumentException("A scorecard must contain at least one rule.", nameof(rules));
        }

        List<ScoringRule<T>> snapshot = [.. rules];
        ScoringRule<T>? duplicateRule = snapshot
            .GroupBy(rule => rule.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?
            .First();

        if (duplicateRule is not null)
        {
            throw new ArgumentException(
                $"A scorecard cannot contain duplicate rule ids. Duplicate id: [{duplicateRule.Id}].",
                nameof(rules));
        }

        Id = id;
        Name = name;
        Description = description;
        Version = version;
        Rules = snapshot.AsReadOnly();
    }

    /// <summary>
    /// Gets the scorecard identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the scorecard name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional scorecard description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the optional scorecard version.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// Gets an immutable snapshot of the ordered scoring rules.
    /// </summary>
    public IReadOnlyList<ScoringRule<T>> Rules { get; }
}
