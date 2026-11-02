using System.Text.Json;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// T2 of <c>docs/testing/functional.md</c>: a dedicated server in English, a client in Ukrainian. The fixture's server speaks
/// English (<c>Lang.PreLoad(..., "en")</c>); a player who joins with <c>"uk"</c> must get refusals, chat lines and the names of
/// our items in Ukrainian, an <c>"en"</c> player in English. Unlike the other server tests, the texts are the point here:
/// the expected ones are read from the mod's own <c>lang/*.json</c>, not written in the tests.
/// </summary>
[Collection(ServerCollection.Name)]
public class LanguageTests(GameServerFixture server) : BookTestBase(server)
{
    static readonly Dictionary<string, Dictionary<string, string>> Texts = new();

    /// <summary>The text of a key of our lang file for this language, filled in as <c>Lang</c> fills it (string.Format).</summary>
    static string Expected(string language, string key, params object[] args)
    {
        lock (Texts)
        {
            if (!Texts.TryGetValue(language, out var texts))
            {
                string file = Path.Combine(TestPaths.ModAssets, "curiocatalogue", "lang", language + ".json");
                texts = Texts[language] = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))!;
            }
            return string.Format(texts[CurioCatalogueModSystem.ModId + ":" + key], args);
        }
    }

    // The chat lines of the mod: a joining player is also greeted by the engine ("Welcome ..." in the server language), not ours.
    static List<string> Told(FakeClient client) => Chat(client).Where(line => !line.StartsWith("Welcome ")).ToList();

    [Theory]
    [InlineData("uk")]
    [InlineData("en")]
    public void A_red_refusal_comes_in_the_players_language(string language)
    {
        // I6: "not supported yet" on a barrel. The text is made on the server with the player's language (Lang.GetL).
        var client = FakeClient.Join(game, "refused-" + language, language);
        game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, _) = TestBlocks.Place(game, "barrel");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-ledger-normal-brickred", 8);
            Click(player, hand, pos);
            return 0;
        });
        client.WaitFor(() => Errors(client).Contains("unsupported"), "the refusal 'not supported'");

        Assert.Equal(language, game.OnMain(() => client.Player.LanguageCode));
        var refusal = client.Received.First(p => p.IngameError?.Code == "unsupported").IngameError;
        Assert.Equal(Expected(language, "ingameerror-unsupported"), refusal.Message);
    }

    [Theory]
    [InlineData("uk")]
    [InlineData("en")]
    public void The_invoice_is_named_in_the_players_language_in_the_chat_line(string language)
    {
        // The bug of 27.10.2026: "Incoming invoice: рядків — 2" (ItemStack.GetName() speaks the server's language).
        var client = FakeClient.Join(game, "namer-" + language, language);
        game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-ledger-normal-brickred", 8);
            Click(player, hand, pos);
            return 0;
        });
        client.WaitFor(() => Told(client).Count > 0, "the chat line about the new list");

        string expected = Expected(language, "invoice-made", Expected(language, "item-invoice-incoming"), 1, 7, Mod.Config.MaxSheets);
        Assert.Equal([expected], Told(client));
    }

    [Theory]
    [InlineData("uk")]
    [InlineData("en")]
    public void A_post_tells_in_the_players_language_that_the_sheet_is_back_in_the_album(string language)
    {
        // I1 with an album in the off-hand: "posted" and "the sheet went back into the album" are two texts in one line.
        var client = FakeClient.Join(game, "poster-" + language, language);
        game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Tapestry("ambush1");
            player.Entity.LeftHandItemSlot.Itemstack = Album(7);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.Incoming, Tapestry("ambush1"));
            Click(player, hand, pos);
            return 0;
        });
        client.WaitFor(() => Told(client).Count > 0, "the chat line about the post");

        var told = Assert.Single(Told(client));
        Assert.StartsWith(Expected(language, "invoice-posted", 1, 1), told);
        Assert.EndsWith(Expected(language, "invoice-posted-sheet-album"), told);
    }

    [Theory]
    [InlineData("uk")]
    [InlineData("en")]
    public void An_album_tells_in_the_players_language_what_it_took(string language)
    {
        // A2: the chat line after the taking.
        var client = FakeClient.Join(game, "collector-" + language, language);
        int missing = 0;
        game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Tapestry("ambush1");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Album(8);
            missing = BookState(hand.Itemstack).Entries;
            Click(player, hand, pos);
            return 0;
        });
        client.WaitFor(() => Told(client).Count > 0, "the chat line about the taking");

        Assert.Equal([Expected(language, "album-collected", 1, missing - 1)], Told(client));
    }
}
