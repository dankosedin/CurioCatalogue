using System;
using System.Collections.Generic;
using System.Linq;

namespace CurioCatalogue.Core;

/// <summary>A container slot holding an item. <paramref name="Index"/> is the caller's own slot number.</summary>
public sealed record SourceSlot(int Index, ItemKey Key, int Count);

/// <summary>A player inventory slot; <paramref name="Key"/> is null when the slot is empty.</summary>
public sealed record TargetSlot(int Index, ItemKey? Key, int Count);

/// <summary>Move <paramref name="Amount"/> items from source slot to target slot.</summary>
public sealed record Move(int Source, int Target, int Amount);

public abstract record TransferPlan
{
    /// <summary>Everything is there and fits; carry out the moves in order.</summary>
    public sealed record Ready(IReadOnlyList<Move> Moves) : TransferPlan;

    /// <summary>The container lacks these items; each entry's count is how many are missing.</summary>
    public sealed record Missing(IReadOnlyList<Entry> Lacking) : TransferPlan;

    /// <summary>The player's inventory lacks room; <paramref name="SlotsShort"/> more free slots would do.</summary>
    public sealed record NoRoom(int SlotsShort) : TransferPlan;
}

/// <summary>What fitted: the moves to carry out, the lines they take, and the lines left behind (no room, or not in the container).</summary>
public sealed record PartialPlan(IReadOnlyList<Move> Moves, IReadOnlyList<Entry> Taken, IReadOnlyList<Entry> Left);

/// <summary>
/// Plans posting an invoice (domain.md, Posting an invoice) as a list of moves from container slots to player slots, so that the
/// caller can check the whole transaction before changing anything. The game's rules come in as two functions.
/// </summary>
public static class Transfer
{
    /// <param name="lines">The invoice lines; the same item on several lines is summed.</param>
    /// <param name="capacity">How many of this item the player slot can hold in total; 0 when it may not hold it
    /// at all (bags with storage flags) or, for an occupied slot, when the game would not merge it with what is there.</param>
    /// <param name="maxStack">How many of this item fit in an ordinary slot; only for estimating the slots short.</param>
    public static TransferPlan Plan(
        IEnumerable<Entry> lines,
        IReadOnlyList<SourceSlot> sources,
        IReadOnlyList<TargetSlot> targets,
        Func<int, ItemKey, int> capacity,
        Func<ItemKey, int> maxStack)
    {
        var needed = Multiset.Sum(lines);

        // 1. Where to take from: partial (smallest) stacks first, then slot order, so full stacks stay in the container.
        var takes = new List<(SourceSlot From, int Amount)>();
        var lacking = new List<Entry>();
        foreach (var line in needed)
        {
            int left = line.Count;
            foreach (var source in sources.Where(s => s.Key == line.Key).OrderBy(s => s.Count).ThenBy(s => s.Index))
            {
                if (left == 0) break;
                int amount = Math.Min(left, source.Count);
                takes.Add((source, amount));
                left -= amount;
            }
            if (left > 0) lacking.Add(line.WithCount(left));
        }
        if (lacking.Count > 0) return new TransferPlan.Missing(lacking);

        // 2. Where to put: top up stacks of the same item first, then empty slots that may hold it.
        var content = targets.ToDictionary(t => t.Index, t => (t.Key, t.Count));
        var order = targets.Select(t => t.Index).ToList();
        var moves = new List<Move>();
        var unplaced = new Dictionary<ItemKey, int>();

        foreach (var (from, amount) in takes)
        {
            int left = Place(from, amount, content, order, capacity, moves);
            if (left > 0) unplaced[from.Key] = unplaced.GetValueOrDefault(from.Key) + left;
        }

        if (unplaced.Count > 0)
        {
            int slotsShort = unplaced.Sum(u => (u.Value + Math.Max(1, maxStack(u.Key)) - 1) / Math.Max(1, maxStack(u.Key)));
            return new TransferPlan.NoRoom(slotsShort);
        }
        return new TransferPlan.Ready(moves);
    }

    /// <summary>
    /// Plans as many whole lines as fit, in the given order (domain.md, Albums and collections: what does not fit stays behind).
    /// A line is taken whole or not at all; a line that does not fit is skipped and later lines may still fit.
    /// Sources and room are chosen as in <see cref="Plan"/>.
    /// </summary>
    /// <param name="lines">Distinct items, in the order to take them.</param>
    public static PartialPlan PlanWhatFits(
        IEnumerable<Entry> lines,
        IReadOnlyList<SourceSlot> sources,
        IReadOnlyList<TargetSlot> targets,
        Func<int, ItemKey, int> capacity)
    {
        var available = sources.ToDictionary(s => s.Index, s => s.Count);
        var content = targets.ToDictionary(t => t.Index, t => (t.Key, t.Count));
        var order = targets.Select(t => t.Index).ToList();
        var moves = new List<Move>();
        var taken = new List<Entry>();
        var left = new List<Entry>();

        foreach (var line in lines)
        {
            var takes = new List<(SourceSlot From, int Amount)>();
            int needed = line.Count;
            foreach (var source in sources.Where(s => s.Key == line.Key && available[s.Index] > 0).OrderBy(s => available[s.Index]).ThenBy(s => s.Index))
            {
                if (needed == 0) break;
                int amount = Math.Min(needed, available[source.Index]);
                takes.Add((source, amount));
                needed -= amount;
            }

            // Try the line on a copy of the inventory; keep it only when all of it fits.
            var tryContent = new Dictionary<int, (ItemKey? Key, int Count)>(content);
            var tryMoves = new List<Move>();
            bool fits = needed == 0 && takes.All(t => Place(t.From, t.Amount, tryContent, order, capacity, tryMoves) == 0);
            if (!fits)
            {
                left.Add(line);
                continue;
            }
            content = tryContent;
            moves.AddRange(tryMoves);
            foreach (var (from, amount) in takes) available[from.Index] -= amount;
            taken.Add(line);
        }
        return new PartialPlan(moves, taken, left);
    }

    /// <summary>
    /// Puts <paramref name="amount"/> items from a source into the player's slots: tops up stacks of the same item
    /// first, then empty slots that may hold it. Updates <paramref name="content"/> and returns how many did not fit.
    /// </summary>
    private static int Place(SourceSlot from, int amount, Dictionary<int, (ItemKey? Key, int Count)> content,
        IReadOnlyList<int> order, Func<int, ItemKey, int> capacity, List<Move> moves)
    {
        int left = amount;
        foreach (bool topUp in new[] { true, false })
        {
            foreach (int target in order)
            {
                if (left == 0) break;
                var (key, count) = content[target];
                if (topUp ? key != from.Key : key != null) continue;
                int room = capacity(target, from.Key) - count;
                if (room <= 0) continue;
                int put = Math.Min(left, room);
                content[target] = (from.Key, count + put);
                moves.Add(new Move(from.Index, target, put));
                left -= put;
            }
        }
        return left;
    }
}
