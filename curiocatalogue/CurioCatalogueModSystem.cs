using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue;

public class CurioCatalogueModSystem : ModSystem
{
    public const string ModId = "curiocatalogue";
    public const string ConfigFileName = "curiocatalogue.json";

    public CurioCatalogueConfig Config { get; private set; } = new();

    public LedgerLimits Limits => new(Config.MaxSheets, Config.SoftEntryLimit);

    private ICoreAPI api = null!;
    private StackKeys? keys;
    private LedgerStore? store;
    private ContainerReader? containers;
    private ContainerAccess? access;
    private InvoiceMaker? invoiceMaker;
    private InvoicePoster? invoicePoster;
    private CollectionRegistry? collections;
    private IReadOnlyDictionary<string, CollectionSettings>? serverSettings;
    private ProblemLog? problems;

    // All are made on first use, when the world and the other mod systems are surely there.

    /// <summary>Item identity: stack → key.</summary>
    public StackKeys Keys => keys ??= new StackKeys(api.World, Config.ExtraIgnoredAttributes);

    /// <summary>Reads and writes ledger and invoice data in items.</summary>
    public LedgerStore Store => store ??= new LedgerStore(Keys, id => Collections.Resolve(id));

    /// <summary>Tells and reads the containers the ledger works with. Both sides.</summary>
    public ContainerReader Containers => containers ??= new ContainerReader(api.World);

    /// <summary>Access to containers. Server only.</summary>
    public ContainerAccess Access => access ??=
        new ContainerAccess(api.World, api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().IsLockedForInteract);

    /// <summary>Makes invoices. Server only.</summary>
    public InvoiceMaker InvoiceMaker => invoiceMaker ??= new InvoiceMaker(api.World, Store, Containers, Access, () => Limits);

    /// <summary>Posts invoices. Server only.</summary>
    public InvoicePoster InvoicePoster => invoicePoster ??=
        new InvoicePoster(api.World, Store, Keys, Containers, Access, () => Limits, () => Config.ConsumeSheets);

    /// <summary>The collections (tapestries), the same on both sides.</summary>
    public CollectionRegistry Collections => collections ??=
        new CollectionRegistry(api.World, Keys, serverSettings ?? Config.Collections, () => DefinitionAsset.OfAllDomains(api.Assets), Problems);

    /// <summary>Problems with the collections' definitions and settings, each logged once in the session (the registry is built
    /// again after the server's settings arrive).</summary>
    private ProblemLog Problems => problems ??= new ProblemLog(api.Logger);

    /// <summary>Client: the server's <c>Collections</c> config arrived (on join). The registry is built again on next use, from it instead
    /// of this machine's file.</summary>
    public void UseServerSettings(IReadOnlyDictionary<string, CollectionSettings> settings)
    {
        serverSettings = settings;
        collections = null;
    }

    public override void Start(ICoreAPI api)
    {
        this.api = api;
        api.RegisterItemClass("CurioCatalogue.ItemLedger", typeof(ItemLedger));
        api.RegisterItemClass("CurioCatalogue.ItemInvoice", typeof(ItemInvoice));
        // Registered on both sides so the class name resolves wherever the game looks it up.
        api.RegisterBlockBehaviorClass(LedgerHintsBehavior.ClassName, typeof(LedgerHintsBehavior));
        ModNetwork.Register(api);
        Config = LoadConfig(api);
        ModLog.DebugEnabled = Config.Diagnostics;
        ModLog.Debug(api, $"config: MaxSheets={Config.MaxSheets}, SoftEntryLimit={Config.SoftEntryLimit}, ExtraIgnoredAttributes=[{string.Join(", ", Config.ExtraIgnoredAttributes)}], ConsumeSheets={Config.ConsumeSheets}, Diagnostics={Config.Diagnostics}, Collections=[{string.Join("; ", Config.Collections.Select(c => $"{c.Key}: Exclude=[{string.Join(", ", c.Value.Exclude)}], Add=[{string.Join(", ", c.Value.Add)}], Disabled={c.Value.Disabled}"))}]");
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        if (api is ICoreServerAPI) AlbumRecipes.RemoveOfDisabled(api, Config.Collections, DefinitionAsset.OfAllDomains(api.Assets), Problems);
        // Hints are drawn by the client, and behaviors added in code are not sent to it (checked in game 26.10.2026:
        // 63 blocks on the server, 0 on the client), so the client adds them itself. The server does not need them.
        if (api.Side == EnumAppSide.Client) LedgerHintsBehavior.AddTo(api);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        ModNetwork.StartServer(api, this);
    }

    public override void StartClientSide(Vintagestory.API.Client.ICoreClientAPI api)
    {
        EmptyHandGesture.StartClient(api, this);
        ModNetwork.StartClient(api, this);
        api.Event.LevelFinalize += () =>
        {
            // The packet may come before the grid recipes and the rest are there (K1): build again once everything is.
            collections = null;
            HandbookIndexFix.Apply(api);
        };
    }

    private static CurioCatalogueConfig LoadConfig(ICoreAPI api)
    {
        CurioCatalogueConfig? config;
        try
        {
            config = api.LoadModConfig<CurioCatalogueConfig>(ConfigFileName);
        }
        catch (Exception e)
        {
            // Keep the broken file as it is so the player can fix it; run with defaults meanwhile.
            api.Logger.Error("[CurioCatalogue] Could not read {0}, using defaults: {1}", ConfigFileName, e.Message);
            return new CurioCatalogueConfig();
        }

        config ??= new CurioCatalogueConfig();
        foreach (var moved in config.MoveLegacy()) api.Logger.Notification("[CurioCatalogue] {0}: {1}", ConfigFileName, moved);
        // Written back every time, so settings added in a newer version appear in the file with their defaults, and
        // corrections (logged as warnings) replace impossible values.
        foreach (var fix in config.Correct()) api.Logger.Warning("[CurioCatalogue] {0}: {1}", ConfigFileName, fix);
        api.StoreModConfig(config, ConfigFileName);
        return config;
    }
}
