using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CurioCatalogue;

/// <summary>
/// How the mod talks to the player (agreed: refusals as the red in-game error, results as a chat line;
/// may change after playtests). Texts are translated on the server into the player's own language.
/// </summary>
internal static class PlayerMessages
{
    /// <param name="code">Lang key suffix: the text is "curiocatalogue:ingameerror-{code}".</param>
    public static void Refuse(IServerPlayer player, string code, params object[] args) =>
        player.SendIngameError(code, Text(player, "ingameerror-" + code, args));

    /// <summary>
    /// Refuses an action on books or invoices whose data is from a newer build of the mod (after a downgrade): writing
    /// them would lose what this build does not know (LedgerStore.IsNewer). Only our items count; others are passed in
    /// freely (e.g. whatever is in the off-hand).
    /// </summary>
    /// <returns>True when refused.</returns>
    public static bool RefuseNewer(IServerPlayer player, params ItemStack?[] stacks)
    {
        if (!stacks.Any(Adapters.LedgerStore.IsNewer)) return false;
        Refuse(player, "newerversion");
        return true;
    }

    /// <summary>
    /// The chat line after a cancel. A want list has its own four texts (the name agrees in gender in Ukrainian, so the
    /// item's name cannot be a placeholder); an intake or pick list shares one set.
    /// </summary>
    public static string CancelledKey(Core.InvoiceKind kind, Core.InvoiceFate fate, bool toAlbum)
    {
        string prefix = kind == Core.InvoiceKind.WantList ? "wantlist-cancelled" : "invoice-cancelled";
        return fate switch
        {
            Core.InvoiceFate.SheetReturned => prefix + (toAlbum ? "-sheet-album" : "-sheet"),
            Core.InvoiceFate.Parchment => prefix + "-parchment",
            Core.InvoiceFate.Consumed => prefix,
            Core.InvoiceFate.Kept => throw ExhaustiveMatching.ExhaustiveMatch.Failed(fate), // only a filled want list is kept
            _ => throw ExhaustiveMatching.ExhaustiveMatch.Failed(fate),
        };
    }

    public static void Tell(IServerPlayer player, string key, params object[] args) =>
        TellText(player, Text(player, key, args));

    /// <summary>A chat line already put together from <see cref="Text"/> pieces.</summary>
    public static void TellText(IServerPlayer player, string text) =>
        player.SendMessage(GlobalConstants.GeneralChatGroup, text, EnumChatType.Notification);

    public static string Text(IServerPlayer player, string key, params object[] args) =>
        Lang.GetL(player.LanguageCode, CurioCatalogueModSystem.ModId + ":" + key, args);

    /// <summary>
    /// The name of one of our items in the player's language. <c>ItemStack.GetName()</c> translates into the server's
    /// language, which differs from the player's on a dedicated server (checked 27.10.2026: "Incoming invoice: рядків — 2").
    /// </summary>
    public static string ItemName(IServerPlayer player, ItemStack stack) =>
        Lang.GetMatchingL(player.LanguageCode, $"{stack.Collectible.Code.Domain}:item-{stack.Collectible.Code.Path}");
}
