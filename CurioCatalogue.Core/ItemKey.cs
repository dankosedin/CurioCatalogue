namespace CurioCatalogue.Core;

public enum CollectibleKind { Item, Block }

/// <summary>
/// Identity of an item for comparison and storage: kind, full code (with domain) and the canonical
/// form of its attributes without the ignored ones. Two stacks are "the same item" exactly when their
/// keys are equal. Build keys with <see cref="ItemIdentity"/>, which does the normalisation.
/// </summary>
/// <param name="Attributes">Canonical attributes, "" when the item has none (see <see cref="AttrCanonical"/>).</param>
public sealed record ItemKey(CollectibleKind Kind, string Code, string Attributes)
{
    public override string ToString() =>
        (Kind == CollectibleKind.Block ? "block:" : "item:") + Code + Attributes;
}
