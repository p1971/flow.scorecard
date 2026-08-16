using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlowScorecard.Engine.Models;

/// <summary>
/// Represents a scoring rule that contributes to a scorecard.
/// </summary>
/// <typeparam name="T">The DTO type evaluated by the rule.</typeparam>
public sealed class ScoringRule<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScoringRule{T}"/> class.
    /// </summary>
    /// <param name="id">The unique rule identifier within its scorecard.</param>
    /// <param name="name">The human-readable rule name.</param>
    /// <param name="description">An optional rule description.</param>
    /// <param name="source">The scorer executed against the DTO.</param>
    public ScoringRule(
        string id,
        string name,
        string? description,
        Func<T, CancellationToken, ValueTask<decimal>> source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(source);

        Id = id;
        Name = name;
        Description = description;
        Source = source;
    }

    /// <summary>
    /// Gets the rule identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the rule name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional rule description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the scorer executed against the DTO.
    /// </summary>
    public Func<T, CancellationToken, ValueTask<decimal>> Source { get; }
}
