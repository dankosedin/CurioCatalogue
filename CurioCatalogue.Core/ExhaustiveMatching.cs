using System;
using System.Collections.Generic;

namespace ExhaustiveMatching;

// Our own small implementation of the types the ExhaustiveMatching analyzer looks for (by full name), written from their public
// API only, so that no third-party dll ships in the mod (docs/architecture.md, Exhaustive switch).

/// <summary>
/// Lists the only direct subtypes of an abstract class or record, so the analyzer can check at compile time that a switch
/// covers every case.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ClosedAttribute : Attribute
{
    /// <summary>The permitted direct subtypes.</summary>
    public IReadOnlyList<Type> Cases { get; }

    public ClosedAttribute(params Type[] cases) => Cases = cases;
}

/// <summary>Thrown when a switch over a [Closed] hierarchy or an enum meets an unmatched value at runtime.</summary>
public class ExhaustiveMatchFailedException : InvalidOperationException
{
    public ExhaustiveMatchFailedException(string message) : base(message) { }
}

/// <summary>Use as <c>default: throw ExhaustiveMatch.Failed(x);</c> in switches the analyzer checks for exhaustiveness.</summary>
public static class ExhaustiveMatch
{
    /// <summary>Builds (does not throw) an exception for an unmatched value.</summary>
    public static ExhaustiveMatchFailedException Failed(object? value) =>
        new(value is null
            ? "Unexpected null value in an exhaustive match"
            : $"Unexpected value {value} of type {value.GetType()} in an exhaustive match");

    /// <summary>Builds (does not throw) an exception for an unmatched case.</summary>
    public static ExhaustiveMatchFailedException Failed() => new("Unexpected case in an exhaustive match");
}
