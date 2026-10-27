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
