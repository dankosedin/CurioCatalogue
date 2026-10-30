using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CurioCatalogue.Tests;

// Catches the asset traps of docs/critical.md before the game starts: the game only logs them and skips the asset.
public class AssetsTests
{
    private static readonly string Assets = Path.Combine(RepoRoot(), "curiocatalogue", "assets", "curiocatalogue");

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

    [Fact]
    public void Every_book_is_in_the_creative_inventory()
    {
        // The handbook builds a page only for what the creative inventory lists (vsapi Collectible.cs:2474#GetHandBookStacks); a cover left
        // out has no page, and the handbook opens its main page instead (the aged covers, found 30.10.2026).
        var book = Load(Path.Combine("itemtypes", "book.json"));
        var states = (string group) => book["variantgroups"]!.Single(g => (string)g["code"]! == group)["states"]!.Values<string>().ToList();
        var listed = book["creativeinventory"]!.Children<JProperty>().SelectMany(p => p.Value.Values<string>())
            .Select(p => new Regex("^" + Regex.Escape(p!).Replace("\\*", ".*") + "$")).ToList();

        var missing = states("kind").SelectMany(k => states("cover").Select(c => $"book-{k}-{c}"))
            .Where(code => !listed.Any(r => r.IsMatch(code)));

        Assert.Empty(missing);
    }

    [Fact]
    public void Handbook_has_a_page_per_kind_and_age()
    {
        // groupBy by type: the first key matching the code wins (RegistryObjectType.solveByType, VSEssentials.dll), {kind} is the variant's value.
        // Aged and normal covers have different recipes, so each of the 3 kinds gets 2 pages, and every book belongs to the page it matches.
        var book = Load(Path.Combine("itemtypes", "book.json"));
        var states = (string group) => book["variantgroups"]!.Single(g => (string)g["code"]! == group)["states"]!.Values<string>().ToList();
        var byType = book["attributes"]!["handbook"]!["groupByByType"]!.Children<JProperty>().ToList();
        var wildcard = (string pattern) => new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$");

        var pages = states("kind").SelectMany(k => states("cover").Select(c => $"book-{k}-{c}"), (k, code) => (kind: k, code))
            .Select(b => (b.code, patterns: byType.First(p => wildcard(p.Name).IsMatch(b.code)).Value.Values<string>()
                .Select(p => p!.Replace("{kind}", b.kind)).ToList()))
            .ToList();

        Assert.All(pages, p => Assert.Contains(p.patterns, pattern => wildcard(pattern).IsMatch(p.code)));
        Assert.Equal(6, pages.Select(p => string.Join("|", p.patterns)).Distinct().Count());
    }
}
