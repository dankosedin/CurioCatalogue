using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CurioCatalogue.PageGen;

/// <summary>A section of the description (or a ### subsection of one) with its body already rendered to HTML.</summary>
public sealed class Section
{
    public string Key { get; init; } = "";
    public string Title { get; init; } = "";
    public string Html { get; init; } = "";
    public List<Section> Subs { get; init; } = new();
}

/// <summary>One description (docs/moddb/description*.md) as data for the template.</summary>
public sealed class DescriptionData
{
    public string Lang { get; init; } = "";
    public string Title { get; init; } = "";
    public string HookHtml { get; init; } = "";
    public string WarningHtml { get; init; } = "";
    public string HeroSrc { get; init; } = "";
    public string HeroAlt { get; init; } = "";
    /// <summary>The list items of "What's inside", as inline HTML.</summary>
    public List<string> Items { get; init; } = new();
    public Dictionary<string, Section> Sections { get; init; } = new();
    /// <summary>The sections' keys in the description's order.</summary>
    public List<string> Keys { get; init; } = new();
    /// <summary>The <c>::: sequence</c> blocks in the description's order (already rendered into the sections' HTML).</summary>
    public List<Sequence.Info> Sequences { get; init; } = new();
}

/// <summary>Reads a description by the contract (docs/releasing.md). Problems are collected in the list, not thrown one by one.</summary>
public static class Description
{
    static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePipeTables().Use<Sequence.Extension>().Build();

    public static DescriptionData Parse(string markdown, string lang, Links links, List<string> problems, int insideIndex = -1)
    {
        string where = $"Description ({lang})";
        var doc = Markdown.Parse(markdown, Pipeline);

        foreach (var html in doc.Descendants<HtmlBlock>())
            problems.Add($"{where}: HTML in the description (line {html.Line + 1}); use plain Markdown");
        foreach (var html in doc.Descendants<HtmlInline>())
            problems.Add($"{where}: HTML in the description (the tag {html.Tag}); use plain Markdown");

        foreach (var link in doc.Descendants<LinkInline>())
            link.Url = links.Absolute(link.Url, link.IsImage);

        var sequences = Sequence.RenderAll(doc, markdown, where, problems, RenderBlock);

        // The head: everything before the first ##.
        int firstH2 = IndexOfHeading(doc, 2, 0);
        var head = doc.Take(firstH2 < 0 ? doc.Count : firstH2).ToList();
        string title = "", hookHtml = "", warningHtml = "", heroSrc = "", heroAlt = "";

        if (head.Count == 0 || head[0] is not HeadingBlock { Level: 1 } h1)
            problems.Add($"{where}: the head must start with the # title");
        else
            title = PlainText(h1.Inline);

        var afterTitle = head.Skip(1).ToList();
        if (afterTitle.Count == 0 || afterTitle[0] is not ParagraphBlock hook || ImageOf(hook) != null)
            problems.Add($"{where}: the first paragraph after the title must be the hook (text, not an image)");
        else
            hookHtml = InlineHtml(hook);

        var quote = afterTitle.OfType<QuoteBlock>().FirstOrDefault();
        if (quote == null) problems.Add($"{where}: the head needs the early-development warning as a blockquote");
        else warningHtml = string.Concat(quote.Select(RenderBlock));

        var heroPara = afterTitle.OfType<ParagraphBlock>().FirstOrDefault(p => ImageOf(p) != null);
        if (heroPara == null) problems.Add($"{where}: the head needs one image (the hero's) in a paragraph of its own");
        else
        {
            var img = ImageOf(heroPara)!;
            heroSrc = img.Url ?? "";
            heroAlt = PlainText(img);
        }

        // The page shows the head's four parts only, so anything else there would be lost silently.
        foreach (var extra in afterTitle.Skip(1).Where(b => b != quote && b != heroPara))
            problems.Add($"{where}: the head has a block the page does not show (line {extra.Line + 1}); the head is the title, the hook, the warning and the hero image");

        // The sections.
        var sections = new Dictionary<string, Section>();
        var keys = new List<string>();
        var items = new List<string>();
        int idx = firstH2;
        while (idx >= 0 && idx < doc.Count)
        {
            var h2 = (HeadingBlock)doc[idx];
            int next = IndexOfHeading(doc, 2, idx + 1);
            int end = next < 0 ? doc.Count : next;
            var body = doc.Skip(idx + 1).Take(end - idx - 1).ToList();
            var section = BuildSection(h2, body, where, problems, items, insideIndex >= 0 ? keys.Count == insideIndex : Slug(PlainText(h2.Inline)) == Contract.InsideKey, lang, keys.Count);
            if (sections.ContainsKey(section.Key)) problems.Add($"{where}: two sections with the key '{section.Key}'");
            else sections[section.Key] = section;
            keys.Add(section.Key);
            idx = next;
        }

        return new DescriptionData
        {
            Lang = lang, Title = title, HookHtml = hookHtml, WarningHtml = warningHtml, HeroSrc = heroSrc, HeroAlt = heroAlt,
            Items = items, Sections = sections, Keys = keys, Sequences = sequences,
        };
    }

    static Section BuildSection(HeadingBlock h2, List<Block> body, string where, List<string> problems, List<string> items, bool inside, string lang, int position)
    {
        string title = PlainText(h2.Inline);
        // English sections are found by the slug of the title; a translation by position (the generator renames them).
        string key = lang == "en" ? Slug(title) : "#" + position;
        var subs = new List<Section>();

        int firstH3 = body.FindIndex(b => b is HeadingBlock { Level: 3 });
        var lead = firstH3 < 0 ? body : body.Take(firstH3).ToList();

        if (inside)
        {
            // The icon table: the list's items by order; the paragraph under it is the section's body.
            if (lead.Count == 0 || lead[0] is not ListBlock list)
                problems.Add($"{where}: '{title}' must start with a list of {Contract.InsideItems} items");
            else
            {
                var listItems = list.OfType<ListItemBlock>().ToList();
                if (listItems.Count != Contract.InsideItems)
                    problems.Add($"{where}: the list in '{title}' has {listItems.Count} items, the page has {Contract.InsideItems} icons");
                foreach (var li in listItems) items.Add(StripOuter(RenderBlock(li), "li"));
                lead = lead.Skip(1).ToList();
            }
        }

        int i = firstH3;
        while (i >= 0 && i < body.Count)
        {
            var h3 = (HeadingBlock)body[i];
            int next = body.FindIndex(i + 1, b => b is HeadingBlock { Level: 3 });
            int end = next < 0 ? body.Count : next;
            var sub = body.Skip(i + 1).Take(end - i - 1);
            string t = PlainText(h3.Inline);
            subs.Add(new Section { Key = Slug(t), Title = t, Html = string.Concat(sub.Select(RenderBlock)) });
            i = next;
        }

        return new Section { Key = key, Title = title, Html = string.Concat(lead.Select(RenderBlock)), Subs = subs };
    }

    static int IndexOfHeading(MarkdownDocument doc, int level, int from)
    {
        for (int i = from; i < doc.Count; i++)
            if (doc[i] is HeadingBlock h && h.Level == level) return i;
        return -1;
    }

    /// <summary>The slug of a title: lower case, apostrophes dropped, other non-alphanumerics become '-'.</summary>
    public static string Slug(string title)
    {
        var s = title.ToLowerInvariant().Replace("'", "").Replace("’", "");
        return Regex.Replace(s, "[^a-z0-9]+", "-").Trim('-');
    }

    internal static LinkInline? ImageOf(ParagraphBlock p)
    {
        var inlines = p.Inline?.ToList() ?? new List<Inline>();
        var img = inlines.OfType<LinkInline>().FirstOrDefault(l => l.IsImage);
        if (img == null) return null;
        bool onlyImage = inlines.All(x => x is LinkInline { IsImage: true } || x is LineBreakInline ||
                                          x is LiteralInline lit && string.IsNullOrWhiteSpace(lit.Content.ToString()));
        return onlyImage ? img : null;
    }

    internal static string PlainText(ContainerInline? c)
    {
        if (c == null) return "";
        var sb = new StringBuilder();
        foreach (var x in c.Descendants())
            if (x is LiteralInline lit) sb.Append(lit.Content.ToString());
            else if (x is CodeInline code) sb.Append(code.Content);
        return sb.ToString();
    }

    static string RenderBlock(Block b)
    {
        var sw = new StringWriter();
        var renderer = new HtmlRenderer(sw);
        Pipeline.Setup(renderer);
        renderer.Render(b);
        return sw.ToString();
    }

    static string InlineHtml(LeafBlock b) => StripOuter(RenderBlock(b), "p");

    internal static string StripOuter(string html, string tag)
    {
        var m = Regex.Match(html.Trim(), $"^<{tag}[^>]*>(.*)</{tag}>$", RegexOptions.Singleline);
        return m.Success ? m.Groups[1].Value.Trim() : html.Trim();
    }
}

/// <summary>
/// Where relative links of the description point: images to raw files, links to blobs, both at a fixed ref.
/// A relative path is read from <see cref="Dir"/>, the description's folder in the repository (e.g. docs/moddb/), as GitHub reads it.
/// </summary>
public sealed class Links
{
    public string RawBase { get; }
    public string BlobBase { get; }
    public string Dir { get; }

    public Links(string rawBase, string blobBase, string dir = "")
    {
        RawBase = rawBase.EndsWith('/') ? rawBase : rawBase + "/";
        BlobBase = blobBase.EndsWith('/') ? blobBase : blobBase + "/";
        Dir = dir == "" || dir.EndsWith('/') ? dir : dir + "/";
    }

    /// <summary>The absolute URL; a path that climbs above the repository's root stays relative, and the page check reports it.</summary>
    public string Absolute(string? url, bool image)
    {
        if (string.IsNullOrEmpty(url)) return url ?? "";
        if (url.StartsWith('#') || url.StartsWith("//") || Regex.IsMatch(url, "^[a-zA-Z][a-zA-Z0-9+.-]*:")) return url;
        var path = Resolve(Dir + url);
        return path == null ? url : (image ? RawBase : BlobBase) + path.Replace(" ", "%20");
    }

    /// <summary>"docs/moddb/../images/a.png" → "docs/images/a.png"; null above the root.</summary>
    static string? Resolve(string path)
    {
        var parts = new List<string>();
        foreach (var part in path.Split('/'))
        {
            if (part == "" || part == ".") continue;
            if (part == "..")
            {
                if (parts.Count == 0) return null;
                parts.RemoveAt(parts.Count - 1);
            }
            else parts.Add(part);
        }
        return string.Join('/', parts) + (path.EndsWith('/') ? "/" : "");
    }
}
