using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CurioCatalogue.Gui;

/// <summary>
/// A cell or a table heading of the album window, for the game's tooltip: the element passes it to
/// <c>TriggerOnMouseEnterSlot</c> (album view, A1). The tooltip's title is <see cref="GetStackName"/> and its text
/// <see cref="GetStackDescription"/> (vsapi GuiElementItemstackInfo.cs:126#GetStackName, ItemSlot.cs:497#GetStackName,
/// ItemSlot.cs:508#GetStackDescription), so our lines go above the item's own.
/// </summary>
public sealed class AlbumSlot(InventoryBase inventory) : ReadOnlySlot(inventory)
{
    /// <summary>A heading: its value's name as the title and no item text (a rock's own description says nothing here).</summary>
    public string? HeadingName { get; set; }

    /// <summary>A cell: lines before the item's text (in the collection or missing, what it leaves free, rarity).</summary>
    public string? Lines { get; set; }

    public override string GetStackName() => HeadingName ?? base.GetStackName();

    public override string GetStackDescription(IClientWorldAccessor world, bool extendedDebugInfo)
    {
        if (HeadingName != null) return "";
        string own = base.GetStackDescription(world, extendedDebugInfo) ?? "";
        return string.IsNullOrEmpty(Lines) ? own : Lines + "\n" + own;
    }
}
