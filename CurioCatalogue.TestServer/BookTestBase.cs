using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace CurioCatalogue.TestServer;

/// <summary>
/// What the tests of books and invoices share: stacks to put into hands and chests, readers of the world's state, and the gestures
/// (a click is the held item's <c>OnHeldInteractStart</c> called on the server, as the game calls it).
/// </summary>
public abstract class BookTestBase(GameServerFixture server)
{
    protected GameServerFixture game { get; } = server;

    protected CurioCatalogueModSystem Mod => game.Api.ModLoader.GetModSystem<CurioCatalogueModSystem>();
    protected IWorldAccessor World => game.Api.World;

    // ---- our texts

    static readonly Dictionary<string, Dictionary<string, string>> Texts = new();

    /// <summary>
    /// The text of a key of our lang file for this language, filled in as <c>Lang</c> fills it (string.Format). Tests of what the
    /// player reads take the expected text from here, so a change of wording changes no test (a key that is gone fails it).
    /// </summary>
    protected static string LangText(string language, string key, params object[] args)
    {
        lock (Texts)
        {
            if (!Texts.TryGetValue(language, out var texts))
            {
                string file = Path.Combine(TestPaths.ModAssets, "curiocatalogue", "lang", language + ".json");
                texts = Texts[language] = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))!;
            }
            return string.Format(texts[CurioCatalogueModSystem.ModId + ":" + key], args);
        }
    }

    // ---- the config on a running server

    /// <summary>
    /// Runs <paramref name="check"/> (on the main thread) with the config changed, and puts the config back. The mod builds its
    /// services on first use from the config of that moment ("keys ??= ..."), so they are dropped before and after: every private
    /// field of the mod system whose type is ours, found by reflection, so a service added later is dropped too.
    /// </summary>
    protected T WithConfig<T>(Action<CurioCatalogueConfig> change, Func<T> check)
    {
        var config = typeof(CurioCatalogueModSystem).GetProperty(nameof(CurioCatalogueModSystem.Config))!;
        // A copy through JSON, as the game stores the config: every setting, also one added later.
        var saved = Newtonsoft.Json.JsonConvert.SerializeObject(Mod.Config);
        change(Mod.Config);
        ForgetServices();
        try { return check(); }
        finally
        {
            config.SetValue(Mod, Newtonsoft.Json.JsonConvert.DeserializeObject<CurioCatalogueConfig>(saved));
            ForgetServices();
        }
    }

    void ForgetServices()
    {
        var ours = new[] { typeof(CurioCatalogueModSystem).Assembly, typeof(ItemKey).Assembly };
        foreach (var field in typeof(CurioCatalogueModSystem).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
            if (ours.Contains(field.FieldType.Assembly) && field.FieldType != typeof(CurioCatalogueConfig))
                field.SetValue(Mod, null);
    }

    // ---- stacks

    protected ItemStack Vanilla(string code, int size = 1) =>
        World.GetItem(new AssetLocation("game", code)) is { } item ? new ItemStack(item, size)
        : new ItemStack(World.GetBlock(new AssetLocation("game", code)) ?? throw new InvalidOperationException($"no game:{code}"), size);

    protected ItemStack Tapestry(string type, int size = 1)
    {
        var stack = Vanilla("tapestry-north", size);
        stack.Attributes.SetString("type", type);
        return stack;
    }

    protected ItemStack Ours(string code) => new(World.GetItem(new AssetLocation("curiocatalogue", code)) ?? throw new InvalidOperationException($"no curiocatalogue:{code}"));

    /// <summary>A ledger (or bill of materials) with these entries, one line per stack.</summary>
    protected ItemStack Book(string code, int sheets, params ItemStack[] entries)
    {
        var stack = Ours(code);
        var mode = ((ItemLedger)stack.Collectible).LedgerMode;
        Mod.Store.WriteLedger(stack, Core.Ledger.FromStored(mode, sheets, entries.Select(Mod.Store.ToEntry)));
        return stack;
    }

    /// <summary>An album with every member of the tapestry collection missing.</summary>
    protected ItemStack Album(int sheets)
    {
        var collection = Mod.Collections.ForSample(Tapestry("ambush1")) ?? throw new InvalidOperationException("no tapestry collection");
        var stack = Ours("book-album-normal-brickred");
        Mod.Store.WriteLedger(stack, collection.NewAlbum(sheets, ""));
        return stack;
    }

    protected ItemStack Invoice(InvoiceKind kind, params ItemStack[] lines)
    {
        var stack = new ItemStack(World.GetItem(InvoiceMaker.InvoiceCode(kind)));
        Mod.Store.WriteInvoice(stack, new Invoice(kind, [.. lines.Select(Mod.Store.ToEntry)], 0, new BlockPosition(0, 0, 0)));
        return stack;
    }

    // ---- reading the world

    protected static string Describe(ItemStack stack)
    {
        string? type = stack.Attributes.GetString("type");
        return $"{stack.Collectible.Code.Path}{(type == null ? "" : ":" + type)} x{stack.StackSize}";
    }

    /// <summary>Where taken items may go: the hotbar without the off-hand, and the backpack.</summary>
    protected static List<ItemSlot> Slots(IServerPlayer player)
    {
        var inventory = player.InventoryManager;
        return inventory.GetHotbarInventory().Where(s => s != inventory.OffhandHotbarSlot)
            .Concat(inventory.GetOwnInventory(GlobalConstants.backpackInvClassName)).ToList();
    }

    /// <summary>The player's stacks that are not ours, sorted ("ingot-copper x5, tapestry-north:ambush1 x1").</summary>
    protected static string Inventory(IServerPlayer player) => string.Join(", ", Slots(player)
        .Where(s => s.Itemstack is { } st && st.Collectible.Code.Domain != CurioCatalogueModSystem.ModId)
        .Select(s => Describe(s.Itemstack!)).Order());

    protected static string Contents(BlockEntity container) => string.Join(", ", ((IBlockEntityContainer)container).Inventory
        .Where(s => !s.Empty).Select(s => Describe(s.Itemstack)).Order());

    /// <summary>The first stack of ours in the main inventory with this code prefix, or null.</summary>
    protected static ItemStack? FindOurs(IServerPlayer player, string codePrefix) => Slots(player)
        .Select(s => s.Itemstack).FirstOrDefault(st => st is { } x && x.Collectible.Code.Domain == CurioCatalogueModSystem.ModId && x.Collectible.Code.Path.StartsWith(codePrefix));

    /// <summary>Takes the first stack of ours with this code prefix out of the main inventory, or null.</summary>
    protected static ItemStack? TakeOurs(IServerPlayer player, string codePrefix)
    {
        var stack = FindOurs(player, codePrefix);
        if (stack != null) Slots(player).First(s => s.Itemstack == stack).Itemstack = null;
        return stack;
    }

    /// <summary>The data of a book (ledger, bill of materials or album), read in its own mode.</summary>
    protected Core.Ledger ReadBook(ItemStack book) => Mod.Store.ReadLedger(book, ((ItemLedger)book.Collectible).LedgerMode);

    protected (int Sheets, int Entries) BookState(ItemStack book) => ToCounts(ReadBook(book));

    protected static (int Sheets, int Entries) ToCounts(Core.Ledger l) => (l.Sheets, l.Entries.Count);

    protected static string Lines(CurioCatalogueModSystem mod, ItemStack invoice) =>
        string.Join(", ", mod.Store.ReadInvoice(invoice, ((ItemInvoice)invoice.Collectible).InvoiceKind).Entries.Select(e => $"{e.Key.Code} x{e.Count}"));

    internal static List<string> Errors(FakeClient client) => client.Received.Where(p => p.IngameError != null).Select(p => p.IngameError.Code).ToList();
    /// <summary>The chat lines the player received, without the engine's greeting at join ("Welcome ...", in the server's language).</summary>
    internal static List<string> Chat(FakeClient client) => client.Received.Where(p => p.Chatline != null).Select(p => p.Chatline.Message)
        .Where(line => !line.StartsWith("Welcome ")).ToList();

    // ---- acting

    /// <summary>The click with the item in <paramref name="slot"/> on the block at <paramref name="pos"/>, Shift held (and Ctrl if asked).</summary>
    protected void Click(IServerPlayer player, ItemSlot slot, BlockPos pos, bool ctrl = false)
    {
        var controls = player.Entity.Controls;
        controls.ShiftKey = true;
        controls.CtrlKey = ctrl;
        try
        {
            var selection = new BlockSelection { Position = pos.Copy(), Face = BlockFacing.UP, HitPosition = new Vec3d(0.5, 1, 0.5) };
            var handling = EnumHandHandling.NotHandled;
            slot.Itemstack!.Collectible.OnHeldInteractStart(slot, player.Entity, selection, null, true, ref handling);
        }
        finally
        {
            controls.ShiftKey = false;
            controls.CtrlKey = false;
        }
    }

    /// <summary>Puts the player next to the block (the off-hand gesture checks the reach on the server).</summary>
    protected void Stand(IServerPlayer player, BlockPos pos, double dx = 0) => MoveTo(player, pos.X + 0.5 + dx, pos.Y, pos.Z + 1.5);

    // A server entity keeps its authoritative place in ServerPos and Pos follows it each tick: set both.
    protected static void MoveTo(IServerPlayer player, double x, double y, double z)
    {
        player.Entity.ServerPos.SetPos(x, y, z);
        player.Entity.Pos.SetPos(x, y, z);
    }

    /// <summary>Fills the main inventory with sticks, except the main hand and, if asked, one hotbar slot (the second).</summary>
    protected void FillWithSticks(IServerPlayer player, bool leaveOneFree = false)
    {
        foreach (var (slot, i) in Slots(player).Select((s, i) => (s, i)).Where(x => x.i >= 1 && !(leaveOneFree && x.i == 1))) slot.Itemstack = Vanilla("stick");
    }
}
