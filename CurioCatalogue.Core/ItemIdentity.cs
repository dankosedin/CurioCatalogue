using System.Collections.Generic;

namespace CurioCatalogue.Core;

/// <summary>
/// Builds <see cref="ItemKey"/>s. Holds the attribute paths that do not count for identity:
/// the game's own list (temperature, spoilage, ...) plus extra ones from the config (durability).
/// </summary>
public sealed class ItemIdentity
{
    private readonly HashSet<string> ignoredPaths;

    /// <param name="ignoredPaths">Attribute paths, nested levels separated by "/", e.g. "temperature".</param>
    public ItemIdentity(IEnumerable<string> ignoredPaths)
    {
        this.ignoredPaths = new HashSet<string>(ignoredPaths);
    }

    public ItemKey KeyOf(CollectibleKind kind, string code, AttrTree? attributes) =>
        new(kind, code, AttrCanonical.Write(attributes, ignoredPaths));
}
