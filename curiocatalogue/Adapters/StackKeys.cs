using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

/// <summary>
/// Turns game item stacks into core <see cref="ItemKey"/>s: converts the attribute tree and applies
/// the ignored attributes (the game's list plus the config's extras).
/// </summary>
public sealed class StackKeys
{
    private readonly IWorldAccessor world;
    private readonly ItemIdentity identity;

    public StackKeys(IWorldAccessor world, IEnumerable<string> extraIgnoredAttributes)
    {
        this.world = world;
        identity = new ItemIdentity(GlobalConstants.IgnoredStackAttributes.Concat(extraIgnoredAttributes));
    }

    public ItemKey KeyOf(ItemStack stack) =>
        identity.KeyOf(KindOf(stack.Class), CodeOf(stack), ToAttrTree(stack.Attributes));

    private static CollectibleKind KindOf(EnumItemClass itemClass) =>
        itemClass == EnumItemClass.Block ? CollectibleKind.Block : CollectibleKind.Item;

    /// <summary>
    /// The stack's full code. A stack read from bytes (e.g. one nested in attributes) knows only its numeric id
    /// until resolved, so resolve it here: otherwise the same item would get a different key before and after.
    /// A stack whose block or item no longer exists (a removed mod) gets "#id".
    /// </summary>
    private string CodeOf(ItemStack stack)
    {
        // Assumption: ResolveBlockOrItem looks up world.GetBlock/GetItem(Id) by Class (vsapi ItemStack.cs:354#ResolveBlockOrItem).
        if (stack.Collectible == null) stack.ResolveBlockOrItem(world);
        return stack.Collectible?.Code?.ToString() ?? "#" + stack.Id;
    }

    public AttrTree ToAttrTree(ITreeAttribute? tree)
    {
        var children = new Dictionary<string, AttrValue>();
        if (tree != null)
        {
            foreach (var (key, value) in tree) children[key] = ToAttrValue(value);
        }
        return new AttrTree(children);
    }

    private AttrValue ToAttrValue(IAttribute attr) => attr switch
    {
        StringAttribute a => new AttrString(a.value ?? ""),
        IntAttribute a => new AttrInt(a.value),
        LongAttribute a => new AttrLong(a.value),
        FloatAttribute a => new AttrFloat(a.value),
        DoubleAttribute a => new AttrDouble(a.value),
        BoolAttribute a => new AttrBool(a.value),
        // Covers StreamedByteArrayAttribute too: it derives from ArrayAttribute<byte>, not from ByteArrayAttribute.
        ArrayAttribute<byte> a => new AttrBytes(a.value ?? []),
        StringArrayAttribute a => Array(a.value, v => new AttrString(v ?? "")),
        IntArrayAttribute a => Array(a.value, v => new AttrInt(v)),
        LongArrayAttribute a => Array(a.value, v => new AttrLong(v)),
        FloatArrayAttribute a => Array(a.value, v => new AttrFloat(v)),
        DoubleArrayAttribute a => Array(a.value, v => new AttrDouble(v)),
        BoolArrayAttribute a => Array(a.value, v => new AttrBool(v)),
        TreeArrayAttribute a => Array(a.value, v => ToAttrTree(v)),
        ITreeAttribute t => ToAttrTree(t),
        ItemstackAttribute a => StackToAttr(a.value),
        // Attribute types added by other mods: their JSON form is the best deterministic text we have.
        _ => new AttrString(attr.GetType().FullName + ":" + attr.ToJsonToken()),
    };

    private static AttrArray Array<T>(T[]? values, System.Func<T, AttrValue> convert) =>
        new((values ?? []).Select(convert).ToList());

    // A stack stored inside attributes (e.g. a bag's contents) counts with its code, size and attributes.
    private AttrValue StackToAttr(ItemStack? stack)
    {
        if (stack == null) return AttrTree.Empty;
        return new AttrTree(new Dictionary<string, AttrValue>
        {
            ["class"] = new AttrString(stack.Class.ToString()),
            ["code"] = new AttrString(CodeOf(stack)),
            ["size"] = new AttrInt(stack.StackSize),
            ["attributes"] = ToAttrTree(stack.Attributes),
        });
    }
}
