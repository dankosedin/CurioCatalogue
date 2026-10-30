using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using CurioCatalogue.Core;

namespace CurioCatalogue;

/// <summary>
/// Commands for testing in game. They live on the branch dev-commands, never in main (docs/development.md, Dev commands).
/// Texts are English only on purpose: they are not part of the product.
/// </summary>
internal static class DevCommands
{
    private static readonly AssetLocation TapestryCode = new("game", "tapestry-north");

    public static void Register(ICoreServerAPI api, CurioCatalogueModSystem mod)
    {
        api.ChatCommands.Create("curio")
            .WithDescription("Curio Catalogue: commands for testing")
            .RequiresPrivilege(Privilege.gamemode)
            .RequiresPlayer()
            .BeginSubCommand("sheets")
                .WithDescription($"Set the number of sheets (0-{mod.Config.MaxSheets}) in the ledger held in the main hand")
                .WithArgs(api.ChatCommands.Parsers.Int("count"))
                .HandleWith(args => SetSheets(args, mod))
            .EndSubCommand()
            .BeginSubCommand("tapestries")
                .WithDescription("Give random different tapestry pieces, one of each (default 10; a number above the total gives all)")
                .WithArgs(api.ChatCommands.Parsers.OptionalInt("count", 10))
                .HandleWith(args => GiveRandomTapestries(api, args))
            .EndSubCommand()
            .BeginSubCommand("tapestry")
                .WithDescription("Give a tapestry piece by name, e.g. ambush1 or salvation12 (quantity 1-64)")
                .WithArgs(api.ChatCommands.Parsers.Word("piece"), api.ChatCommands.Parsers.OptionalInt("quantity", 1))
                .HandleWith(args => GiveTapestry(api, args))
            .EndSubCommand()
            .BeginSubCommand("jug")
                .WithDescription("Give a fired jug with lime water (restoration recipe) or sulfuric acid (ageing recipe): /curio jug [lime|acid] [litres 1-3]")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("liquid"), api.ChatCommands.Parsers.OptionalInt("litres", 1))
                .HandleWith(args => GiveJug(api, args))
            .EndSubCommand()
            .BeginSubCommand("books")
                .WithDescription("Give a book of every cover: /curio books [ledger|materials|album] [normal|aged|all] [sheets] (sheets: write the ledger data like a used book, -1 leaves the book plain)")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("kind"), api.ChatCommands.Parsers.OptionalWord("age"), api.ChatCommands.Parsers.OptionalInt("sheets", -1))
                .HandleWith(args => GiveBooks(api, args, mod))
            .EndSubCommand()
            .BeginSubCommand("missing")
                .WithDescription("Give one of each item listed by the album or want list held in the main hand")
                .HandleWith(args => GiveMissing(api, args, mod))
            .EndSubCommand();
    }

    // Ranges are checked here, not with the game's IntRange parser: its error ("Number out of range") does not say the range.
    private static TextCommandResult SetSheets(TextCommandCallingArgs args, CurioCatalogueModSystem mod)
    {
        int sheets = (int)args[0];
        int max = mod.Config.MaxSheets;
        if (sheets < 0 || sheets > max)
            return TextCommandResult.Error($"Sheets must be from 0 to {max} (MaxSheets in the config), got {sheets}.");

        var slot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;
        if (slot?.Itemstack?.Collectible is not ItemLedger ledgerItem)
            return TextCommandResult.Error("Hold a ledger or album in your main hand.");

        var ledger = mod.Store.ReadLedger(slot.Itemstack, ledgerItem.LedgerMode);
        mod.Store.WriteLedger(slot.Itemstack, ledger with { Sheets = sheets });
        slot.MarkDirty();
        return TextCommandResult.Success($"Ledger now has {sheets} of {max} sheets.");
    }

    private static TextCommandResult GiveRandomTapestries(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        int count = (int)args[0];
        if (count < 1) return TextCommandResult.Error($"Count must be at least 1, got {count}.");
        if (!TryGetPieces(api, out var block, out var pieces) || block == null) return NoTapestries();

        var chosen = pieces.OrderBy(_ => api.World.Rand.Next()).Take(count).ToList();
        foreach (var piece in chosen) Give(api, args.Caller.Player, block, piece, 1);
        return TextCommandResult.Success($"Gave {chosen.Count} of {pieces.Count} tapestry pieces: {string.Join(", ", chosen)}");
    }

    private static TextCommandResult GiveTapestry(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        int quantity = (int)args[1];
        if (quantity < 1 || quantity > 64) return TextCommandResult.Error($"Quantity must be from 1 to 64, got {quantity}.");
        if (!TryGetPieces(api, out var block, out var pieces) || block == null) return NoTapestries();

        var piece = (string)args[0];
        if (!pieces.Contains(piece))
            return TextCommandResult.Error($"No tapestry piece '{piece}'. Try /curio tapestries to see some names.");

        Give(api, args.Caller.Player, block, piece, quantity);
        return TextCommandResult.Success($"Gave {quantity} x {piece}.");
    }

    // Assumption: the picture of a tapestry is the stack attribute "type", the list of pictures and their sizes
    // is the block attribute "sizes" (RESEARCH.md 6; assets/survival/blocktypes/cloth/tapestry.json).
    private static bool TryGetPieces(ICoreServerAPI api, out Block? block, out List<string> pieces)
    {
        block = api.World.GetBlock(TapestryCode);
        var sizes = block?.Attributes?["sizes"].AsObject<Dictionary<string, string>>();
        pieces = sizes == null ? [] : TapestryPieces.WithPictures(sizes).Select(p => p.Piece).ToList();
        return pieces.Count > 0;
    }

    // Assumption: a jug is a BlockLiquidContainerBase; its content is a portion stack of ItemsPerLitre per litre
    // (vssurvivalmod BlockLiquidContainerBase.cs:240 SetCurrentLitres, :314 GetContainableProps, :353 SetContent).
    private static TextCommandResult GiveJug(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        string liquid = args[0] is string { Length: > 0 } word ? word : "lime";
        string? code = liquid switch { "lime" => "limewaterportion", "acid" => "acid-full-sulfuric", _ => null };
        if (code == null) return TextCommandResult.Error($"Liquid must be lime or acid, got {liquid}.");
        int litres = (int)args[1];
        if (litres < 1 || litres > 3) return TextCommandResult.Error($"Litres must be from 1 to 3 (a jug holds 3), got {litres}.");

        var jug = api.World.GetBlock(new AssetLocation("game", "jug-blue-fired")) as Vintagestory.GameContent.BlockLiquidContainerBase;
        var portion = api.World.GetItem(new AssetLocation("game", code));
        if (jug == null || portion == null) return TextCommandResult.Error($"Jug or {code} not found.");

        var content = new ItemStack(portion);
        var props = Vintagestory.GameContent.BlockLiquidContainerBase.GetContainableProps(content);
        content.StackSize = (int)(litres * (props?.ItemsPerLitre ?? 100));
        var stack = new ItemStack(jug);
        jug.SetContent(stack, content);

        var player = args.Caller.Player;
        player.InventoryManager.TryGiveItemstack(stack, true);
        if (stack.StackSize > 0) api.World.SpawnItemEntity(stack, player.Entity.Pos.XYZ);
        return TextCommandResult.Success($"Gave a jug with {litres} L of {code}.");
    }

    // Reproducing "no recipes for old covers": one book per cover, found among the registered items (not a hardcoded list).
    private static TextCommandResult GiveBooks(ICoreServerAPI api, TextCommandCallingArgs args, CurioCatalogueModSystem mod)
    {
        string kind = args[0] is string { Length: > 0 } k ? k : "ledger";
        string age = args[1] is string { Length: > 0 } a ? a : "all";
        int sheets = (int)args[2];
        if (kind is not ("ledger" or "materials" or "album")) return TextCommandResult.Error($"Kind must be ledger, materials or album, got {kind}.");
        if (age is not ("normal" or "aged" or "all")) return TextCommandResult.Error($"Age must be normal, aged or all, got {age}.");
        if (sheets > mod.Config.MaxSheets) return TextCommandResult.Error($"Sheets must be at most {mod.Config.MaxSheets} (MaxSheets in the config), got {sheets}.");

        string prefix = $"book-{kind}-" + (age == "all" ? "" : age + "-");
        var items = api.World.Items
            .Where(i => i.Code?.Domain == CurioCatalogueModSystem.ModId && i.Code.Path.StartsWith(prefix))
            .OrderBy(i => i.Code.Path).ToList();
        if (items.Count == 0) return TextCommandResult.Error($"No items with code {prefix}*.");

        var player = args.Caller.Player;
        foreach (var item in items)
        {
            var stack = new ItemStack(item);
            if (sheets >= 0 && item is ItemLedger ledgerItem)
                mod.Store.WriteLedger(stack, Ledger.Empty(ledgerItem.LedgerMode, sheets));
            player.InventoryManager.TryGiveItemstack(stack, true);
            if (stack.StackSize > 0) api.World.SpawnItemEntity(stack, player.Entity.Pos.XYZ);
        }
        return TextCommandResult.Success($"Gave {items.Count} books: {string.Join(", ", items.Select(i => i.Code.Path))}");
    }

    private static TextCommandResult GiveMissing(ICoreServerAPI api, TextCommandCallingArgs args, CurioCatalogueModSystem mod)
    {
        var player = args.Caller.Player;
        var stack = player.InventoryManager.ActiveHotbarSlot?.Itemstack;
        IEnumerable<Entry> listed = stack?.Collectible switch
        {
            ItemLedger { IsAlbum: true } => mod.Store.ReadLedger(stack, LedgerMode.Album).Entries.Values,
            ItemInvoice { InvoiceKind: InvoiceKind.WantList } => mod.Store.ReadInvoice(stack, InvoiceKind.WantList).Entries,
            _ => [],
        };
        var samples = listed.Select(e => ((ItemStack)e.Sample).Clone()).ToList();
        if (samples.Count == 0) return TextCommandResult.Error("Hold an album or a want list with something listed in your main hand.");

        foreach (var sample in samples)
        {
            sample.StackSize = 1;
            player.InventoryManager.TryGiveItemstack(sample, true);
            if (sample.StackSize > 0) api.World.SpawnItemEntity(sample, player.Entity.Pos.XYZ);
        }
        return TextCommandResult.Success($"Gave {samples.Count} items: {string.Join(", ", samples.Select(s => s.GetName()))}");
    }

    private static TextCommandResult NoTapestries() =>
        TextCommandResult.Error($"Block {TapestryCode} or its 'sizes' attribute not found.");

    /// <summary>Into the player's inventory; what does not fit drops at their feet.</summary>
    private static void Give(ICoreServerAPI api, IPlayer player, Block block, string piece, int quantity)
    {
        var stack = new ItemStack(block, quantity);
        stack.Attributes.SetString("type", piece);
        player.InventoryManager.TryGiveItemstack(stack, true);
        if (stack.StackSize > 0) api.World.SpawnItemEntity(stack, player.Entity.Pos.XYZ);
    }
}
