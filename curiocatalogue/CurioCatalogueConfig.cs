namespace CurioCatalogue;

/// <summary>
/// Settings stored in ModConfig/curiocatalogue.json.
/// </summary>
public class CurioCatalogueConfig
{
    /// <summary>
    /// Tapestry pieces left out of the tapestry collection, by piece name; "*" matches any text. Empty by default.
    /// Candidates that may not appear in survival (not in the game's tapestryGroups): "aid-*", "glider*", "symptoms*".
    /// (A note here, not in the file: the config is written back by a serializer that keeps no comments.)
    /// </summary>
    public string[] TapestryExclude { get; set; } = [];

    /// <summary>Extra tapestry pieces (the stack's "type") to count in the collection, each its own picture.</summary>
    public string[] TapestryAdd { get; set; } = [];

    /// <summary>Maximum number of sheets a ledger can hold.</summary>
    public int MaxSheets { get; set; } = 32;

    /// <summary>Soft limit of entries in one ledger; tune after playtests.</summary>
    public int SoftEntryLimit { get; set; } = 500;

    /// <summary>Attributes ignored when comparing items, on top of the game's own list (temperature, spoilage...).</summary>
    public string[] ExtraIgnoredAttributes { get; set; } = ["durability"];

    /// <summary>Hardcore option: a posted invoice never returns its sheet to the ledger.</summary>
    public bool ConsumeSheets { get; set; } = false;

    /// <summary>Write diagnostics to the game log (server-main.log / client-main.log). For finding problems; off by default.</summary>
    public bool Diagnostics { get; set; } = false;
}
