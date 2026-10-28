using CurioCatalogue.Core;

namespace CurioCatalogue.Core.Tests;

public class TransferTests
{
    private static ItemKey K(string name) => new(CollectibleKind.Item, "game:" + name, "");
    private static Entry Line(string name, int count) => new(K(name), count, new object());
    private static SourceSlot S(int index, string name, int count) => new(index, K(name), count);
    private static TargetSlot Empty(int index) => new(index, null, 0);
    private static TargetSlot T(int index, string name, int count) => new(index, K(name), count);

    private static SourceSlot S(int index, string name, int count, int stack) => new(index, K(name), count, stack);
    private static TargetSlot T(int index, string name, int count, int stack) => new(index, K(name), count, stack);

    private static int Holds64(int target, SourceSlot source) => 64;
    private static int Stack64(ItemKey key) => 64;

    private static TransferPlan Plan(Entry[] lines, SourceSlot[] sources, TargetSlot[] targets,
        Func<int, SourceSlot, int>? capacity = null, Func<ItemKey, int>? maxStack = null) =>
        Transfer.Plan(lines, sources, targets, capacity ?? Holds64, maxStack ?? Stack64);

    private static Move[] Moves(TransferPlan plan) => Assert.IsType<TransferPlan.Ready>(plan).Moves.ToArray();

    [Fact]
    public void Simple_move_into_an_empty_slot()
    {
        var moves = Moves(Plan([Line("a", 3)], [S(0, "a", 3)], [Empty(5)]));
        Assert.Equal([new Move(0, 5, 3)], moves);
    }

    [Fact]
    public void Takes_from_partial_stacks_first_then_in_slot_order()
    {
        // Slots 0 and 2 are full-ish, slot 1 is the small one: take 1 from slot 1, then the rest from slot 0.
        var moves = Moves(Plan([Line("a", 15)], [S(0, "a", 64), S(1, "a", 5), S(2, "a", 64)], [Empty(9)]));
        Assert.Equal([new Move(1, 9, 5), new Move(0, 9, 10)], moves);
    }

    [Fact]
    public void Same_item_on_several_lines_is_summed()
    {
        var moves = Moves(Plan([Line("a", 2), Line("a", 3)], [S(0, "a", 10)], [Empty(9)]));
        Assert.Equal([new Move(0, 9, 5)], moves);
    }

    [Fact]
    public void Tops_up_stacks_of_the_same_item_before_using_empty_slots()
    {
        var moves = Moves(Plan([Line("a", 10)], [S(0, "a", 10)], [Empty(1), T(2, "a", 60), T(3, "b", 1)]));
        Assert.Equal([new Move(0, 2, 4), new Move(0, 1, 6)], moves);
    }

    [Fact]
    public void Splits_over_several_empty_slots_by_stack_size()
    {
        var moves = Moves(Plan([Line("a", 5)], [S(0, "a", 5)], [Empty(1), Empty(2), Empty(3)], capacity: (_, _) => 2));
        Assert.Equal([new Move(0, 1, 2), new Move(0, 2, 2), new Move(0, 3, 1)], moves);
    }

    [Fact]
    public void Slots_that_may_not_hold_the_item_are_skipped()
    {
        // E.g. a seed bag slot: only seeds go there.
        int onlySlot2(int target, SourceSlot source) => target == 2 ? 64 : 0;
        var moves = Moves(Plan([Line("a", 1)], [S(0, "a", 1)], [Empty(1), Empty(2)], capacity: onlySlot2));
        Assert.Equal([new Move(0, 2, 1)], moves);
    }

    [Fact]
    public void Slot_capacity_can_differ_per_slot()
    {
        // E.g. a slot with its own smaller limit, like the bag slots of the backpack inventory.
        int small1(int target, SourceSlot source) => target == 1 ? 1 : 64;
        var moves = Moves(Plan([Line("a", 3)], [S(0, "a", 3)], [Empty(1), Empty(2)], capacity: small1));
        Assert.Equal([new Move(0, 1, 1), new Move(0, 2, 2)], moves);
    }

    [Fact]
    public void Stack_the_game_would_not_merge_with_is_not_topped_up()
    {
        // Same key (durability is ignored by us) but the game does not merge them: capacity 0 for that slot.
        int notSlot1(int target, SourceSlot source) => target == 1 ? 0 : 64;
        var moves = Moves(Plan([Line("a", 1)], [S(0, "a", 1)], [T(1, "a", 1), Empty(2)], capacity: notSlot1));
        Assert.Equal([new Move(0, 2, 1)], moves);
    }

    [Fact]
    public void Missing_items_are_reported_with_how_many_lack()
    {
        var plan = Plan([Line("a", 5), Line("b", 2), Line("c", 1)], [S(0, "a", 3), S(1, "c", 1)], [Empty(9)]);
        var lacking = Assert.IsType<TransferPlan.Missing>(plan).Lacking;
        Assert.Equal([(K("a"), 2), (K("b"), 2)], lacking.Select(e => (e.Key, e.Count)));
    }

    [Fact]
    public void No_room_reports_how_many_more_slots_would_do()
    {
        var plan = Plan([Line("a", 5), Line("b", 1)], [S(0, "a", 5), S(1, "b", 1)], [Empty(1)], capacity: (_, _) => 2, maxStack: _ => 2);
        // a: 2 placed, 3 left = 2 slots; b: 1 left = 1 slot.
        Assert.Equal(3, Assert.IsType<TransferPlan.NoRoom>(plan).SlotsShort);
    }

    [Fact]
    public void Missing_is_checked_before_room()
    {
        var plan = Plan([Line("a", 5)], [S(0, "a", 1)], []);
        Assert.IsType<TransferPlan.Missing>(plan);
    }

    [Fact]
    public void Planned_slots_are_not_used_twice()
    {
        // Two different items, one empty slot each: they must not both go into slot 1.
        var moves = Moves(Plan([Line("a", 1), Line("b", 1)], [S(0, "a", 1), S(1, "b", 1)], [Empty(1), Empty(2)]));
        Assert.Equal([new Move(0, 1, 1), new Move(1, 2, 1)], moves);
    }

    [Fact]
    public void Empty_invoice_plans_nothing()
    {
        Assert.Empty(Moves(Plan([], [S(0, "a", 1)], [Empty(1)])));
    }

    // --- As much as fits (domain.md, Albums and collections) ---

    private static PartialPlan WhatFits(Entry[] lines, SourceSlot[] sources, TargetSlot[] targets, Func<int, SourceSlot, int>? capacity = null) =>
        Transfer.PlanWhatFits(lines, sources, targets, capacity ?? Holds64);

    [Fact]
    public void What_fits_takes_lines_in_order_until_the_inventory_is_full()
    {
        var plan = WhatFits([Line("c", 1), Line("a", 1), Line("b", 1)], [S(0, "a", 1), S(1, "b", 1), S(2, "c", 1)], [Empty(7)]);

        Assert.Equal([new Move(2, 7, 1)], plan.Moves);
        Assert.Equal([K("c")], plan.Taken.Select(e => e.Key));
        Assert.Equal([K("a"), K("b")], plan.Left.Select(e => e.Key));
    }

    [Fact]
    public void What_fits_skips_a_line_without_room_and_still_tops_up_for_a_later_one()
    {
        var plan = WhatFits([Line("a", 1), Line("b", 1)], [S(0, "a", 1), S(1, "b", 1)], [T(4, "b", 10)]);

        Assert.Equal([new Move(1, 4, 1)], plan.Moves);
        Assert.Equal([K("b")], plan.Taken.Select(e => e.Key));
        Assert.Equal([K("a")], plan.Left.Select(e => e.Key));
    }

    [Fact]
    public void What_fits_takes_a_line_whole_or_not_at_all()
    {
        // 3 wanted, room for 2: nothing moves for that line.
        var plan = WhatFits([Line("a", 3)], [S(0, "a", 3)], [Empty(1)], capacity: (_, _) => 2);

        Assert.Empty(plan.Moves);
        Assert.Empty(plan.Taken);
        Assert.Equal([K("a")], plan.Left.Select(e => e.Key));
    }

    [Fact]
    public void What_fits_leaves_a_line_the_container_does_not_have()
    {
        var plan = WhatFits([Line("a", 1), Line("b", 1)], [S(0, "b", 1)], [Empty(1), Empty(2)]);

        Assert.Equal([K("b")], plan.Taken.Select(e => e.Key));
        Assert.Equal([K("a")], plan.Left.Select(e => e.Key));
    }

    // --- The game's merge rule (code review 28.10.2026) ---
    // Stacks with the same key may still not merge in the game (an attribute the config ignores, e.g. durability):
    // they get different Stack numbers, and the plan never puts them into one slot.

    [Fact]
    public void Stacks_the_game_would_not_merge_go_into_separate_slots()
    {
        var moves = Moves(Plan([Line("a", 6)], [S(0, "a", 3, stack: 1), S(1, "a", 3, stack: 2)], [Empty(7), Empty(8)]));
        Assert.Equal([new Move(0, 7, 3), new Move(1, 8, 3)], moves);
    }

    [Fact]
    public void One_free_slot_is_not_room_for_two_stacks_the_game_would_not_merge()
    {
        var plan = Plan([Line("a", 6)], [S(0, "a", 3, stack: 1), S(1, "a", 3, stack: 2)], [Empty(7)]);
        Assert.Equal(1, Assert.IsType<TransferPlan.NoRoom>(plan).SlotsShort);
    }

    [Fact]
    public void Only_a_stack_the_game_would_merge_is_topped_up()
    {
        var moves = Moves(Plan([Line("a", 3)], [S(0, "a", 3, stack: 1)], [T(6, "a", 10, stack: 2), T(7, "a", 10, stack: 1)]));
        Assert.Equal([new Move(0, 7, 3)], moves);
    }

    [Fact]
    public void What_fits_follows_the_merge_rule_too()
    {
        var plan = WhatFits([Line("a", 2), Line("b", 1)], [S(0, "a", 1, stack: 1), S(1, "a", 1, stack: 2), S(2, "b", 1)], [Empty(7), Empty(8)]);
        Assert.Equal([K("a")], plan.Taken.Select(e => e.Key));
        Assert.Equal([new Move(0, 7, 1), new Move(1, 8, 1)], plan.Moves);
    }
}
