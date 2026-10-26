using CurioCatalogue.Core;

namespace CurioCatalogue.Core.Tests;

public class ItemIdentityTests
{
    private static readonly ItemIdentity Identity =
        new(["temperature", "toolMode", "renderVariant", "transitionstate", "durability"]);

    private static AttrTree Tree(params (string Key, AttrValue Value)[] children) =>
        new(children.ToDictionary(c => c.Key, c => c.Value));

    private static ItemKey Tapestry(string type) =>
        Identity.KeyOf(CollectibleKind.Block, "game:tapestry-north", Tree(("type", new AttrString(type))));

    [Fact]
    public void Tapestry_key_reads_well()
    {
        Assert.Equal("block:game:tapestry-north{\"type\":\"ambush1\"}", Tapestry("ambush1").ToString());
    }

    [Fact]
    public void Same_attributes_give_equal_keys()
    {
        Assert.Equal(Tapestry("ambush1"), Tapestry("ambush1"));
        Assert.NotEqual(Tapestry("ambush1"), Tapestry("ambush2"));
    }

    [Fact]
    public void Key_order_does_not_matter()
    {
        var a = Identity.KeyOf(CollectibleKind.Item, "game:x", Tree(("a", new AttrInt(1)), ("b", new AttrInt(2))));
        var b = Identity.KeyOf(CollectibleKind.Item, "game:x", Tree(("b", new AttrInt(2)), ("a", new AttrInt(1))));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Kind_and_code_are_part_of_identity()
    {
        Assert.NotEqual(
            Identity.KeyOf(CollectibleKind.Item, "game:x", null),
            Identity.KeyOf(CollectibleKind.Block, "game:x", null));
        Assert.NotEqual(
            Identity.KeyOf(CollectibleKind.Item, "game:x", null),
            Identity.KeyOf(CollectibleKind.Item, "game:y", null));
    }

    [Fact]
    public void Ignored_attributes_do_not_count()
    {
        var hot = Identity.KeyOf(CollectibleKind.Item, "game:ingot-iron", Tree(("temperature", Tree(("temperature", new AttrFloat(900f))))));
        var worn = Identity.KeyOf(CollectibleKind.Item, "game:pickaxe-iron", Tree(("durability", new AttrInt(12))));
        Assert.Equal(Identity.KeyOf(CollectibleKind.Item, "game:ingot-iron", null), hot);
        Assert.Equal(Identity.KeyOf(CollectibleKind.Item, "game:pickaxe-iron", null), worn);
    }

    [Fact]
    public void No_attributes_and_empty_tree_are_the_same()
    {
        var none = Identity.KeyOf(CollectibleKind.Item, "game:x", null);
        Assert.Equal("", none.Attributes);
        Assert.Equal(none, Identity.KeyOf(CollectibleKind.Item, "game:x", AttrTree.Empty));
    }

    [Fact]
    public void Ignored_path_is_matched_by_full_path_only()
    {
        // "durability" is ignored at the top level, but a nested tree's own "durability" is data.
        var nested = Identity.KeyOf(CollectibleKind.Item, "game:x", Tree(("tool", Tree(("durability", new AttrInt(3))))));
        Assert.Equal("{\"tool\":{\"durability\":3}}", nested.Attributes);

        var custom = new ItemIdentity(["tool/durability"]);
        Assert.Equal("{\"tool\":{}}", custom.KeyOf(CollectibleKind.Item, "game:x", Tree(("tool", Tree(("durability", new AttrInt(3)))))).Attributes);
    }

    [Fact]
    public void Value_types_are_distinguished()
    {
        string Canon(AttrValue v) => Identity.KeyOf(CollectibleKind.Item, "game:x", Tree(("v", v))).Attributes;

        string[] all =
        [
            Canon(new AttrInt(5)), Canon(new AttrLong(5)), Canon(new AttrFloat(5)),
            Canon(new AttrDouble(5)), Canon(new AttrString("5")),
        ];
        Assert.Equal(all.Length, all.Distinct().Count());
    }

    [Fact]
    public void Floats_are_written_with_invariant_culture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("uk-UA"); // decimal comma
            var key = Identity.KeyOf(CollectibleKind.Item, "game:x", Tree(("v", new AttrDouble(1.5))));
            Assert.Equal("{\"v\":1.5D}", key.Attributes);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Strings_are_escaped()
    {
        var key = Identity.KeyOf(CollectibleKind.Item, "game:x", Tree(("a\"b", new AttrString("c\\d\n"))));
        Assert.Equal("{\"a\\\"b\":\"c\\\\d\\u000a\"}", key.Attributes);
    }

    [Fact]
    public void Arrays_bytes_and_bools_are_written()
    {
        var key = Identity.KeyOf(CollectibleKind.Item, "game:x", Tree(
            ("arr", new AttrArray([new AttrInt(1), new AttrString("two")])),
            ("bytes", new AttrBytes([0x0a, 0xff])),
            ("flag", new AttrBool(true))));
        Assert.Equal("{\"arr\":[1,\"two\"],\"bytes\":0x0aff,\"flag\":true}", key.Attributes);
    }
}
