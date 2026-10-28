using System.Collections.Generic;

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

        MaxSheets = AtLeastOne(nameof(MaxSheets), MaxSheets);
        SoftEntryLimit = AtLeastOne(nameof(SoftEntryLimit), SoftEntryLimit);
        TapestryExclude = NotNull(nameof(TapestryExclude), TapestryExclude);
        TapestryAdd = NotNull(nameof(TapestryAdd), TapestryAdd);
        ExtraIgnoredAttributes = NotNull(nameof(ExtraIgnoredAttributes), ExtraIgnoredAttributes);
        return fixes;
    }
}
