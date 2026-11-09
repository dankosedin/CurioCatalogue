using ExhaustiveMatching;

namespace CurioCatalogue.Core;

/// <summary>Why an album does nothing with its collection now; the code also names the player-facing message ("ingameerror-" +
/// the code in lower case), the same wherever the album is used (taking new, a want list, the window, the tooltip).</summary>
public enum AlbumRefusal
{
    /// <summary>The server's config switched the album's collection off.</summary>
    Disabled,

    /// <summary>The game does not have the album's collection (a mod removed, the definition broken).</summary>
    NoCollection,

    /// <summary>The collection has several levels and the album has none chosen (A2 of the album view plan).</summary>
    NoLevel,
}

/// <summary>
/// Where an album stands against its collection as the game has it now: at a level of it, or refused, and why. The one ladder
/// (switched off, then gone, then no level) that every use of an album asks (docs/decisions.md, Collections, one resolver).
/// </summary>
[Closed(typeof(AlbumStanding.AtLevel), typeof(AlbumStanding.Refused))]
public abstract record AlbumStanding
{
    /// <summary>The album can take and list: its collection is on and its level chosen (or the only one).</summary>
    public sealed record AtLevel(DefinedCollection Value, CollectionLevel Level) : AlbumStanding;

    /// <param name="Value">The collection, for <see cref="AlbumRefusal.NoLevel"/> (the level is chosen in it); else null.</param>
    public sealed record Refused(AlbumRefusal Reason, DefinedCollection? Value = null) : AlbumStanding;

    public static AlbumStanding Of(Album album, CollectionResolution resolution) => resolution switch
    {
        CollectionResolution.Found found => album.LevelIn(found.Value) is { } level
            ? new AtLevel(found.Value, level)
            : new Refused(AlbumRefusal.NoLevel, found.Value),
        CollectionResolution.Disabled _ => new Refused(AlbumRefusal.Disabled),
        CollectionResolution.Unknown _ => new Refused(AlbumRefusal.NoCollection),
        _ => throw ExhaustiveMatch.Failed(resolution),
    };

    /// <summary>The collection when the game has it on (at a level or not), else null. (Not named as the cases' parameter: a
    /// record would reuse this property for it.)</summary>
    public DefinedCollection? Collection => this switch
    {
        AtLevel at => at.Value,
        Refused refused => refused.Value,
        _ => throw ExhaustiveMatch.Failed(this),
    };

    /// <summary>The level when <see cref="AtLevel"/>, else null.</summary>
    public CollectionLevel? LevelOrNull => (this as AtLevel)?.Level;

    /// <summary>The refusal, null when <see cref="AtLevel"/>.</summary>
    public AlbumRefusal? Refusal => (this as Refused)?.Reason;
}
