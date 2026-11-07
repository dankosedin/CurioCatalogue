using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CurioCatalogue;

/// <summary>What the server changes in one collection (<c>Collections.&lt;id&gt;</c> in the config; docs/decisions.md,
/// Collections). The patterns and names are each source's own name of a member: an item code without domain for variants, a piece
/// name for tapestries.</summary>
public class CollectionSettings
{
    /// <summary>Members left out, by name; "*" matches any text. Added to the definition's own exclusions.</summary>
    public string[] Exclude { get; set; } = [];

    /// <summary>Extra members by name, for a source that can make one from a name (tapestries: a piece, its own picture).</summary>
    public string[] Add { get; set; } = [];

    /// <summary>The collection is off on this server: no album of it can be made or filled.</summary>
    public bool Disabled { get; set; }
}

/// <summary>
/// Settings stored in ModConfig/curiocatalogue.json.
/// </summary>
public class CurioCatalogueConfig
{
    /// <summary>The collections' settings by collection id. Tapestry pieces left out can be listed under "tapestries" in "Exclude";
    /// candidates that may not appear in survival (not in the game's tapestryGroups): "aid-*", "glider*", "symptoms*".
    /// (A note here, not in the file: the config is written back by a serializer that keeps no comments.)</summary>
    public Dictionary<string, CollectionSettings> Collections { get; set; } = new() { [TapestriesId] = new() };

    public const string TapestriesId = "tapestries";

    /// <summary>The settings of a collection; the defaults when the file has none.</summary>
    public CollectionSettings SettingsOf(string id) => Collections.TryGetValue(id, out var s) ? s : new();

    /// <summary>The way up from 0.1.x: read once by <see cref="MoveLegacy"/> into <c>Collections.tapestries</c> and then null, so
    /// the file loses them when it is written back.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string[]? TapestryExclude { get; set; }

    /// <inheritdoc cref="TapestryExclude"/>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string[]? TapestryAdd { get; set; }

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

    /// <summary>
    /// Moves the 0.1.x settings <c>TapestryExclude</c> and <c>TapestryAdd</c> into <c>Collections.tapestries</c> (after what is
    /// already there) and clears them. Call it before <see cref="Correct"/>, once on load.
    /// </summary>
    /// <returns>What was moved, a line each, for the log.</returns>
    public List<string> MoveLegacy()
    {
        var moved = new List<string>();
        if (TapestryExclude == null && TapestryAdd == null) return moved;
        Collections ??= new();
        if (!Collections.TryGetValue(TapestriesId, out var settings) || settings == null) Collections[TapestriesId] = settings = new();
        // Empty lists (0.1.1 writes them so) are removed silently: nothing moved, nothing to tell the admin (K7, H14).
        if (TapestryExclude is { Length: > 0 })
        {
            settings.Exclude = [.. settings.Exclude ?? [], .. TapestryExclude];
            moved.Add($"TapestryExclude [{string.Join(", ", TapestryExclude)}] moved to Collections.{TapestriesId}.Exclude");
        }
        if (TapestryAdd is { Length: > 0 })
        {
            settings.Add = [.. settings.Add ?? [], .. TapestryAdd];
            moved.Add($"TapestryAdd [{string.Join(", ", TapestryAdd)}] moved to Collections.{TapestriesId}.Add");
        }
        TapestryExclude = null;
        TapestryAdd = null;
        return moved;
    }

    /// <summary>
    /// Puts impossible values of a hand-edited file right: limits of at least 1 (a book of 0 sheets makes nothing, a limit of
    /// 0 makes every book full), and a list set to null as empty.
    /// </summary>
    /// <returns>What was corrected, a line each, for the log.</returns>
    public List<string> Correct()
    {
        var fixes = new List<string>();
        int AtLeastOne(string name, int value)
        {
            if (value >= 1) return value;
            fixes.Add($"{name} {value} is below 1, using 1");
            return 1;
        }
        string[] NotNull(string name, string[]? list)
        {
            if (list != null) return list;
            fixes.Add($"{name} is null, using an empty list");
            return [];
        }
        // A null inside a list (a stray comma in a hand-edited file) would break the collection's patterns (K7, R1).
        string[] NoNulls(string name, string[] list)
        {
            if (!list.Contains(null)) return list;
            fixes.Add($"{name} has an empty entry (null), dropped");
            return list.Where(s => s != null).ToArray();
        }

        MaxSheets = AtLeastOne(nameof(MaxSheets), MaxSheets);
        SoftEntryLimit = AtLeastOne(nameof(SoftEntryLimit), SoftEntryLimit);
        ExtraIgnoredAttributes = NotNull(nameof(ExtraIgnoredAttributes), ExtraIgnoredAttributes);
        if (Collections == null)
        {
            fixes.Add($"{nameof(Collections)} is null, using none");
            Collections = new();
        }
        foreach (var id in Collections.Keys.ToList())
        {
            if (Collections[id] == null)
            {
                fixes.Add($"{nameof(Collections)}.{id} is null, using the defaults");
                Collections[id] = new();
            }
            var settings = Collections[id];
            settings.Exclude = NoNulls($"{nameof(Collections)}.{id}.{nameof(CollectionSettings.Exclude)}",
                NotNull($"{nameof(Collections)}.{id}.{nameof(CollectionSettings.Exclude)}", settings.Exclude));
            settings.Add = NoNulls($"{nameof(Collections)}.{id}.{nameof(CollectionSettings.Add)}",
                NotNull($"{nameof(Collections)}.{id}.{nameof(CollectionSettings.Add)}", settings.Add));
        }
        return fixes;
    }
}
