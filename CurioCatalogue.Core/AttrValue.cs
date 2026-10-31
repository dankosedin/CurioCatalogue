using System.Collections.Generic;
using ExhaustiveMatching;

namespace CurioCatalogue.Core;

/// <summary>
/// A game-independent copy of an item's attribute tree. The game adapter converts the game's
/// <c>ITreeAttribute</c> into this model; the core only reads it to build an <see cref="ItemKey"/>.
/// Each value keeps its type, because the game tells e.g. an int 5 from a long 5.
/// </summary>
[Closed(typeof(AttrString), typeof(AttrInt), typeof(AttrLong), typeof(AttrFloat), typeof(AttrDouble), typeof(AttrBool),
    typeof(AttrBytes), typeof(AttrArray), typeof(AttrTree))]
public abstract record AttrValue;

public sealed record AttrString(string Value) : AttrValue;
public sealed record AttrInt(int Value) : AttrValue;
public sealed record AttrLong(long Value) : AttrValue;
public sealed record AttrFloat(float Value) : AttrValue;
public sealed record AttrDouble(double Value) : AttrValue;
public sealed record AttrBool(bool Value) : AttrValue;
public sealed record AttrBytes(byte[] Value) : AttrValue;

/// <summary>An array attribute (the game has int, long, float, double, bool, string and tree arrays).</summary>
public sealed record AttrArray(IReadOnlyList<AttrValue> Items) : AttrValue;

/// <summary>A nested tree. Key order does not matter: the canonical form sorts keys.</summary>
public sealed record AttrTree(IReadOnlyDictionary<string, AttrValue> Children) : AttrValue
{
    public static readonly AttrTree Empty = new(new Dictionary<string, AttrValue>());
}
