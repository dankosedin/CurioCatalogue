using NSubstitute;
using Vintagestory.API.Common;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue.Tests;

/// <summary>
/// The refusal "missing" travels as stacks, so that the client names them in its own language. Window requests name the
/// item they are about, and the server acts only on that item.
/// </summary>
public class ModNetworkTests
{
    private readonly GameWorld game = new();

    private static Entry EntryOf(ItemStack stack, int count) =>
        new(new ItemKey(CollectibleKind.Item, stack.Collectible.Code.ToString(), ""), count, stack);

    [Fact]
    public void Refusal_names_the_first_lacking_items_and_counts_the_rest()
    {
        var items = new[] { "ingot-iron", "ingot-copper", "plank-oak", "nail-iron", "rope" }.Select(code => game.Item(code)).ToList();
        var lacking = items.Select((item, i) => EntryOf(new ItemStack(item), i + 1)).ToList();

        var refusal = ModNetwork.MissingRefusal(lacking);

        Assert.Equal(ModNetwork.LackingShown, refusal.Stacks.Count);
        Assert.Equal([1, 2, 3], refusal.Counts);
        Assert.Equal(2, refusal.More);
    }

    [Fact]
    public void Lacking_stacks_come_back_as_the_same_items_with_their_attributes()
    {
        // Tapestries are named from the attribute "type", so it must survive the trip.
        var tapestry = game.Block("tapestry-north");
        var stack = new ItemStack(tapestry);
        stack.Attributes.SetString("type", "ambush1");

        var refusal = ModNetwork.MissingRefusal([EntryOf(stack, 2)]);
        var back = new ItemStack(refusal.Stacks[0]);

        // Resolves through world.GetBlock(id) (GameWorld; vsapi ItemStack.cs:354#ResolveBlockOrItem).
        Assert.True(back.ResolveBlockOrItem(game.World));
        Assert.Same(tapestry, back.Collectible);
        Assert.Equal("ambush1", back.Attributes.GetString("type"));
        Assert.Equal(0, refusal.More);
    }

    // --- Window requests act only on the item the window shows (code review 28.10.2026) ---

    private ItemStack Book(string name)
    {
        var stack = new ItemStack(game.Item("curiocatalogue:book-materials-normal-teal", new ItemLedger()));
        stack.Attributes.GetOrAddTreeAttribute(LedgerStore.RootKey).SetString("name", name);
        return stack;
    }

    private static ItemSlot SlotWith(ItemStack? stack)
    {
        var slot = new InventoryGeneric(1, "testslot-1", Substitute.For<ICoreAPI>())[0];
        slot.Itemstack = stack;
        return slot;
    }

    private static ShownItem Shown(ItemStack stack, int hotbarSlot = 2, bool offHand = false) =>
        new() { OffHand = offHand, HotbarSlot = hotbarSlot, Fingerprint = LedgerStore.Fingerprint(stack) };

    private static ItemSlot? Target(ShownItem shown, ItemSlot active, int activeIndex = 2, ItemSlot? offhand = null) =>
        ModNetwork.ShownSlot(shown, offhand, active, activeIndex, c => c is ItemLedger);

    [Fact]
    public void The_book_the_window_shows_is_the_target()
    {
        var book = Book("tools");
        var hand = SlotWith(book);

        Assert.Same(hand, Target(Shown(book), hand));
    }

    [Fact]
    public void Another_hotbar_slot_is_refused_even_with_the_same_book()
    {
        // The hotbar keys and the wheel switch the active slot while a window is open (decompiled HudHotbar, OnKeySlot).
        var book = Book("tools");

        Assert.Null(Target(Shown(book, hotbarSlot: 2), SlotWith(book.Clone()), activeIndex: 3));
    }

    [Fact]
    public void Another_book_in_the_same_slot_is_refused()
    {
        var shown = Shown(Book("tools"));

        Assert.Null(Target(shown, SlotWith(Book("ores"))));
    }

    [Fact]
    public void A_hand_without_the_item_kind_is_refused()
    {
        var book = Book("tools");

        Assert.Null(Target(Shown(book), SlotWith(null)));
        Assert.Null(Target(Shown(book), SlotWith(new ItemStack(game.Item("ingot-iron")))));
        Assert.Null(ModNetwork.ShownSlot(null, null, SlotWith(book), 2, c => c is ItemLedger));
    }

    [Fact]
    public void The_off_hand_ignores_the_hotbar_slot()
    {
        var book = Book("tools");
        var offhand = SlotWith(book);

        Assert.Same(offhand, Target(Shown(book, hotbarSlot: 2, offHand: true), SlotWith(null), activeIndex: 7, offhand));
    }

    [Fact]
    public void A_fresh_book_without_data_has_a_fingerprint_too()
    {
        var book = new ItemStack(game.Item("curiocatalogue:book-materials-normal-teal", new ItemLedger()));
        var hand = SlotWith(book);

        Assert.Same(hand, Target(Shown(book), hand));
    }

    // --- Book names ---

    [Fact]
    public void A_long_name_is_cut_to_the_limit_and_trimmed()
    {
        Assert.Equal(new string('a', ModNetwork.MaxNameLength), ModNetwork.LimitName("  " + new string('a', 60) + "  "));
        Assert.Equal("Tools", ModNetwork.LimitName(" Tools "));
        Assert.Equal("", ModNetwork.LimitName(null));
    }

    [Fact]
    public void Cutting_a_name_does_not_split_an_emoji()
    {
        // A character outside the basic plane is two UTF-16 chars (a surrogate pair); half of one is not text.
        string name = new string('a', ModNetwork.MaxNameLength - 1) + "📦";

        string cut = ModNetwork.LimitName(name);

        Assert.Equal(new string('a', ModNetwork.MaxNameLength - 1), cut);
        Assert.False(char.IsHighSurrogate(cut[^1]));
    }
}
