using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CurioCatalogue.Tests;

// Catches the asset traps of docs/critical.md before the game starts: the game only logs them and skips the asset.
public class AssetsTests
{
    private static readonly string Assets = Path.Combine(RepoRoot(), "curiocatalogue", "assets", "curiocatalogue");

    internal static string AssetsFolder => Assets;

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "curiocatalogue.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("curiocatalogue.sln not found above " + AppContext.BaseDirectory);
    }

    private static IEnumerable<string> AssetFiles() =>
        Directory.GetFiles(Assets, "*.json", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(Assets, f));

    public static TheoryData<string> JsonFiles() => new(AssetFiles());

    private static JToken Load(string relative, CommentHandling comments = CommentHandling.Ignore) =>
        JToken.Parse(File.ReadAllText(Path.Combine(Assets, relative)), new JsonLoadSettings { CommentHandling = comments });

    private static Dictionary<string, string> Lang(string code) =>
        Load(Path.Combine("lang", code + ".json")).ToObject<Dictionary<string, string>>()!;

    [Theory]
    [MemberData(nameof(JsonFiles))]
    public void Json_file_parses(string file)
    {
        // The game reads assets with Newtonsoft, which accepts JSON5-style unquoted keys, single quotes and comments.
        var ex = Record.Exception(() => Load(file));

        Assert.True(ex == null, $"{file}: {ex?.Message}");
    }

    /// <summary>
    /// The refusals the player sees as "ingameerror-" + the reason in lower case (ItemLedger, ItemInvoice, ModNetwork).
    /// NoDifference is worded by what was asked: nothing the book lacks or has, or nothing missing in the album.
    /// </summary>
    public static TheoryData<string> RefusalCodes() => new(
        Enum.GetNames<Adapters.MakeRefusal>().Concat(Enum.GetNames<Adapters.PostRefusal>()).Concat(Enum.GetNames<Adapters.CollectRefusal>())
            .Where(name => name != nameof(Adapters.MakeRefusal.NoDifference))
            .Select(name => name.ToLowerInvariant())
            .Concat(["nodifference-incoming", "nodifference-outgoing", "allcollected"])
            .Distinct());

    [Theory]
    [MemberData(nameof(RefusalCodes))]
    public void Every_refusal_has_its_text(string code)
    {
        Assert.Contains("curiocatalogue:ingameerror-" + code, Lang("en").Keys);
    }

    public static TheoryData<string> Languages() =>
        new(Directory.GetFiles(Path.Combine(Assets, "lang"), "*.json").Select(Path.GetFileNameWithoutExtension)!);

    [Theory]
    [MemberData(nameof(Languages))]
    public void Lang_file_has_the_keys_of_en(string code)
    {
        // Every language is translated in full (CONTRIBUTING.md, Translations).
        var en = Lang("en").Keys.ToHashSet();
        var keys = Lang(code).Keys.ToHashSet();

        Assert.Empty(en.Except(keys));
        Assert.Empty(keys.Except(en));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Lang_texts_have_no_single_braces(string code)
    {
        // Lang texts go through string.Format (modding.md, Texts and messages): a brace is {{ or }}, or a format item like {0}.
        var formatItem = new Regex(@"\{\{|\}\}|\{\d+(,-?\d+)?(:[^{}]*)?\}");
        var broken = Lang(code).Where(e => formatItem.Replace(e.Value, "").IndexOfAny(['{', '}']) >= 0).Select(e => e.Key);

        Assert.Empty(broken);
    }

    public static TheoryData<string> RecipeFiles() =>
        new(AssetFiles().Where(f => f.StartsWith("recipes" + Path.DirectorySeparatorChar)));

    [Theory]
    [MemberData(nameof(RecipeFiles))]
    public void Recipe_codes_name_their_domain(string file)
    {
        // A code without a domain resolves in ours, so a vanilla code without "game:" is silently not found (modding.md, Recipes).
        // This covers every "code" in the file, also inside requiresContent.
        var bare = ((JContainer)Load(file)).Descendants().OfType<JProperty>()
            .Where(p => p.Name == "code" && p.Value.Type == JTokenType.String && !((string)p.Value!).Contains(':'))
            .Select(p => $"{p.Path}: {p.Value}");

        Assert.Empty(bare);
    }

    [Theory]
    [MemberData(nameof(RecipeFiles))]
    public void Recipe_arrays_have_no_comments(string file)
    {
        // A comment in an array becomes an element and the recipe fails to parse (vssurvivalmod RecipeLoader.cs:89#parsedContent).
        var comments = ((JContainer)Load(file, CommentHandling.Load)).DescendantsAndSelf().OfType<JArray>()
            .SelectMany(a => a.Children()).Where(t => t.Type == JTokenType.Comment)
            .Select(t => $"{t.Path}: {t}");

        Assert.Empty(comments);
    }

    // ---- Books and the handbook: shared helpers ----

    private static JToken Book() => Load(Path.Combine("itemtypes", "book.json"));

    private static List<string> States(JToken type, string group) =>
        type["variantgroups"]!.Single(g => (string)g["code"]! == group)["states"]!.Values<string>().Select(s => s!).ToList();

    // A wildcard as the game reads it: "*" is any text (WildcardUtil.Match).
    private static Regex Wildcard(string pattern) => new("^" + Regex.Escape(pattern).Replace(@"\*", ".*") + "$");

    private static List<(string kind, string cover, string code)> Books() =>
        States(Book(), "kind").SelectMany(k => States(Book(), "cover").Select(c => (k, c, $"book-{k}-{c}"))).ToList();

    // Codes of an item type with one variant group of any name, without the domain: "invoice-incoming".
    private static IEnumerable<string> ItemCodes(string file)
    {
        var type = Load(Path.Combine("itemtypes", file));
        return States(type, (string)type["variantgroups"]![0]!["code"]!).Select(s => $"{(string)type["code"]!}-{s}");
    }

    // The 24 covers: 13 normal and 11 aged (no aged teal and purple). Frozen on purpose: a removed state turns such books in a world
    // into unknown items, an added one needs recipes (domain.md, Covers).
    private static readonly string[] FrozenCovers =
    [
        "normal-brickred", "normal-cherryred", "normal-darkbeige", "normal-darkgray", "normal-darkgreen", "normal-darkolive", "normal-gray",
        "normal-olive", "normal-orange", "normal-orangebrown", "normal-purple", "normal-purpleorange", "normal-teal",
        "aged-brickred", "aged-cherryred", "aged-darkbeige", "aged-darkgray", "aged-darkgreen", "aged-darkolive", "aged-gray",
        "aged-olive", "aged-orange", "aged-orangebrown", "aged-purpleorange"
    ];

    [Fact]
    public void Every_book_is_in_both_creative_tabs()
    {
        // The handbook builds a page only for what the creative inventory lists (vsapi Collectible.cs:2474#GetHandBookStacks); a cover left
        // out has no page, and the handbook opens its main page instead (the aged covers, found 30.10.2026).
        var tabs = Book()["creativeinventory"]!.Children<JProperty>().ToDictionary(p => p.Name, p => p.Value.Values<string>().Select(v => Wildcard(v!)).ToList());

        Assert.Equal(["general", "items"], tabs.Keys.Order());
        var missing = tabs.SelectMany(tab => Books().Where(b => !tab.Value.Any(r => r.IsMatch(b.code))).Select(b => $"{tab.Key}: {b.code}"));
        Assert.Empty(missing);
    }

    [Fact]
    public void Recipe_icons_group_by_kind_and_age()
    {
        // groupBy by type: the first key matching the code wins (RegistryObjectType.cs:356#jsonObj), {kind} is the variant's value.
        // The groups only fold the icons of "Ingredient for" and "Created by"; every book has its own page (modding.md, Handbook).
        // Aged and normal covers have different recipes, so a book's group is exactly the books of the same kind and age.
        var byType = Book()["attributes"]!["handbook"]!["groupByByType"]!.Children<JProperty>().ToList();
        var books = Books();

        foreach (var (kind, cover, code) in books)
        {
            var patterns = byType.First(p => Wildcard(p.Name).IsMatch(code)).Value.Values<string>().Select(p => Wildcard(p!.Replace("{kind}", kind))).ToList();
            var grouped = books.Where(b => patterns.Any(r => r.IsMatch(b.code))).Select(b => b.code).Order();
            var sameKindAndAge = books.Where(b => b.kind == kind && b.cover.Split('-')[0] == cover.Split('-')[0]).Select(b => b.code).Order();

            Assert.Equal(sameKindAndAge, grouped);
        }
    }

    [Fact]
    public void Covers_are_the_frozen_list_in_book_and_recipes()
    {
        // The 24 covers are written in book.json and in every recipe that names covers (a recipe lists them in allowedVariants.cover).
        Assert.Equal(["ledger", "materials", "album"], States(Book(), "kind"));
        Assert.Equal(FrozenCovers, States(Book(), "cover"));

        var lists = AssetFiles().Where(f => f.StartsWith("recipes" + Path.DirectorySeparatorChar)).SelectMany(f =>
            ((JContainer)Load(f)).Descendants().OfType<JProperty>().Where(p => p.Name == "cover" && p.Parent?.Parent is JProperty { Name: "allowedVariants" })
                .Select(p => (where: $"{f} {p.Path}", covers: p.Value.Values<string>().ToList()))).ToList();

        Assert.NotEmpty(lists);
        Assert.All(lists, l => Assert.True(FrozenCovers.SequenceEqual(l.covers!), l.where));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Handbook_links_point_to_existing_pages(string code)
    {
        // A link to a missing page opens the handbook's main page without an error (modding.md, Handbook). Ours are item-curiocatalogue:<code>
        // (an item or block of the vanilla game, "item-paper-parchment", is not checked) or a guide's pageCode.
        var ours = Books().Select(b => b.code).Concat(ItemCodes("invoice.json")).ToHashSet();
        var guides = Directory.GetFiles(Path.Combine(Assets, "config", "handbook"), "*.json")
            .Select(f => (string)JToken.Parse(File.ReadAllText(f))["pageCode"]!).ToHashSet();

        var targets = Lang(code).Values.SelectMany(text => Regex.Matches(text, @"handbook://([^""\\]+)").Select(m => m.Groups[1].Value)).ToList();
        var broken = targets.Where(t => t.StartsWith("item-curiocatalogue:") ? !ours.Contains(t["item-curiocatalogue:".Length..])
            : t.StartsWith("item-") || t.StartsWith("block-") ? false : !guides.Contains(t));

        // The regex finds links at all: the English guide links the aged ledger. Not required of a translation, which may word it otherwise.
        if (code == "en") Assert.Contains("item-curiocatalogue:book-ledger-aged-darkgreen", targets);
        Assert.Empty(broken);
    }
}
