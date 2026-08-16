using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FlowScorecard.Engine.Models;

namespace FlowScorecard.Engine;

/// <summary>
/// Builds a typed scorecard using a fluent API.
/// </summary>
/// <typeparam name="T">The DTO type evaluated by the scorecard.</typeparam>
public sealed class ScorecardBuilder<T>
    where T : class
{
    private readonly List<ScoringRule<T>> _rules = [];
    private string? _id;
    private string? _name;
    private string? _description;
    private string? _version;

    /// <summary>
    /// Creates a new builder.
    /// </summary>
    /// <returns>A new scorecard builder.</returns>
    public static ScorecardBuilder<T> Create() => new();

    /// <summary>
    /// Sets the scorecard identifier.
    /// </summary>
    /// <param name="id">The scorecard identifier.</param>
    /// <returns>This builder.</returns>
    public ScorecardBuilder<T> WithId(string id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the scorecard name.
    /// </summary>
    /// <param name="name">The scorecard name.</param>
    /// <returns>This builder.</returns>
    public ScorecardBuilder<T> WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the optional scorecard description.
    /// </summary>
    /// <param name="description">The scorecard description.</param>
    /// <returns>This builder.</returns>
    public ScorecardBuilder<T> WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the optional scorecard version.
    /// </summary>
    /// <param name="version">The scorecard version.</param>
    /// <returns>This builder.</returns>
    public ScorecardBuilder<T> WithVersion(string version)
    {
        _version = version;
        return this;
    }

    /// <summary>
    /// Adds a synchronous scoring rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="name">The rule name.</param>
    /// <param name="source">The scorer.</param>
    /// <param name="description">An optional rule description.</param>
    /// <returns>This builder.</returns>
    public ScorecardBuilder<T> WithRule(
        string id,
        string name,
        Func<T, decimal> source,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return AddRule(id, name, (request, _) => ValueTask.FromResult(source(request)), description);
    }

    /// <summary>
    /// Adds an asynchronous scoring rule backed by a task.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="name">The rule name.</param>
    /// <param name="source">The scorer.</param>
    /// <param name="description">An optional rule description.</param>
    /// <returns>This builder.</returns>
    public ScorecardBuilder<T> WithRule(
        string id,
        string name,
        Func<T, CancellationToken, Task<decimal>> source,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return AddRule(id, name, (request, token) => new ValueTask<decimal>(source(request, token)), description);
    }

    /// <summary>
    /// Adds an asynchronous scoring rule backed by a value task.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="name">The rule name.</param>
    /// <param name="source">The scorer.</param>
    /// <param name="description">An optional rule description.</param>
    /// <returns>This builder.</returns>
    public ScorecardBuilder<T> WithRule(
        string id,
        string name,
        Func<T, CancellationToken, ValueTask<decimal>> source,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return AddRule(id, name, source, description);
    }

    /// <summary>
    /// Builds an immutable scorecard.
    /// </summary>
    /// <returns>The configured scorecard.</returns>
    public Scorecard<T> Build()
    {
        if (string.IsNullOrWhiteSpace(_id))
        {
            throw new InvalidOperationException("Scorecard Id must be set via WithId().");
        }

        if (string.IsNullOrWhiteSpace(_name))
        {
            throw new InvalidOperationException("Scorecard Name must be set via WithName().");
        }

        if (_rules.Count == 0)
        {
            throw new InvalidOperationException("Scorecard must contain at least one rule via WithRule().");
        }

        return new Scorecard<T>(_id, _name, _description, _rules, _version);
    }

    private ScorecardBuilder<T> AddRule(
        string id,
        string name,
        Func<T, CancellationToken, ValueTask<decimal>> source,
        string? description)
    {
        _rules.Add(new ScoringRule<T>(id, name, description, source));
        return this;
    }
}
