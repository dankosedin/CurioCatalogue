using ExhaustiveMatching;

namespace CurioCatalogue.Core;

/// <summary>
/// What the game says of a collection an album (or a want list) names: it is there, it is off on this server, or it is not
/// known at all. Closed, so every place that refuses or shows an album has to say what it does for each: "disabled on this
/// server" and "no longer in the game" read differently to a player (docs/architecture.md, Part 1, Collections, one resolver).
/// </summary>
[Closed(typeof(CollectionResolution.Found), typeof(CollectionResolution.Disabled), typeof(CollectionResolution.Unknown))]
public abstract record CollectionResolution
{
    /// <summary>The collection is on and has members.</summary>
    public sealed record Found(DefinedCollection Value) : CollectionResolution;

    /// <summary>The server's config switched it off (<c>Collections.&lt;id&gt;.Disabled</c>).</summary>
    public sealed record Disabled(string Id) : CollectionResolution;

    /// <summary>No definition, or one that cannot be used, or a source that finds nothing: a mod was removed, or the game changed.</summary>
    public sealed record Unknown(string Id) : CollectionResolution;

    /// <summary>The collection when found, else null. (The parameter of <c>Found</c> is not named like this: a record would reuse this property for it.)</summary>
    public DefinedCollection? Collection => (this as Found)?.Value;
}
