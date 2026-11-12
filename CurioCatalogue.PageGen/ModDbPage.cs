using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using PreMailer.Net;
using Scriban;
using Scriban.Runtime;

namespace CurioCatalogue.PageGen;

/// <summary>Everything the generator needs, as values: reading files is the caller's job.</summary>
public sealed record PageInput
{
    public required string DescriptionEn { get; init; }
    public required string DescriptionUk { get; init; }
    /// <summary>The descriptions' folder in the repository: their relative paths are read from it, as GitHub reads them.</summary>
    public string DescriptionDir { get; init; } = "docs/moddb/";
    /// <summary>The guide's files (docs/guide/README.md, README.uk.md): read only to check the headings the descriptions link to.</summary>
    public required string GuideEn { get; init; }
    public required string GuideUk { get; init; }
    public required string Template { get; init; }
    public required string Version { get; init; }
    public required string GameVersion { get; init; }
    /// <summary>"Universal", "Server" or "Client" (modinfo.json has no side: Universal).</summary>
    public string Side { get; init; } = "Universal";
    /// <summary>Codes of lang/*.json.</summary>
    public required IReadOnlyList<string> LangCodes { get; init; }
    /// <summary>Where relative images point, e.g. https://raw.githubusercontent.com/dankosedin/CurioCatalogue/v0.1.1/</summary>
    public required string RawBase { get; init; }
    /// <summary>Where relative links point, e.g. https://github.com/dankosedin/CurioCatalogue/blob/v0.1.1/</summary>
    public required string BlobBase { get; init; }
    public required string RepoUrl { get; init; }
    /// <summary>Report a bug in the row of buttons (ModDB's card has it too).</summary>
    public bool ModDbLinks { get; init; }
    /// <summary>The page may not be larger than this many bytes (UTF-8); see <see cref="Contract.MaxPageBytes"/>.</summary>
    public int MaxBytes { get; init; } = Contract.MaxPageBytes;
}

/// <summary>The two descriptions + template → the HTML that is pasted into ModDB's editor (docs/plans/2026-11-01-mod-page.md, "The generator's shape").</summary>
public static class ModDbPage
{
    public static string Generate(PageInput input)
    {
        var problems = new List<string>();
        var links = new Links(input.RawBase, input.BlobBase, input.DescriptionDir);
        var en = Description.Parse(input.DescriptionEn, "en", links, problems);
        var uk = Description.Parse(input.DescriptionUk, "uk", links, problems, en.Keys.IndexOf(Contract.InsideKey));
        CrossCheck(en, uk, problems);
        CheckGuideAnchors(en, uk, input, problems);
        CheckTemplateCss(input.Template, problems);
        if (problems.Count > 0) throw new ContractException(problems);

        var html = Fill(input, en, uk, problems);
        if (problems.Count > 0) throw new ContractException(problems);

        html = Inline(html, problems);
        int bytes = System.Text.Encoding.UTF8.GetByteCount(html);
        if (bytes > input.MaxBytes)
            problems.Add($"The page is {bytes} bytes, ModDB takes at most about 65 000 and the limit here is {input.MaxBytes}: shorten the descriptions or lighten the template's CSS");
        if (problems.Count > 0) throw new ContractException(problems);
        return html;
    }

    static void CrossCheck(DescriptionData en, DescriptionData uk, List<string> problems)
    {
        var known = Contract.SectionKeys.ToHashSet();
        foreach (var k in en.Keys.Where(k => !known.Contains(k)))
            problems.Add($"Description (en): the section '{k}' is not known to the template; add it to the template and to Contract, or rename it");
        foreach (var k in known.Where(k => !en.Keys.Contains(k)))
            problems.Add($"Description (en): the section '{k}' is missing, the template shows it");

        if (en.Keys.Count != uk.Keys.Count)
        {
            problems.Add($"The descriptions differ: en has {en.Keys.Count} sections, uk has {uk.Keys.Count}");
            return;
        }

        // Ukrainian sections get the key of the English one at the same position.
        var ukByKey = new Dictionary<string, Section>();
        var ukSections = uk.Keys.Select(k => uk.Sections[k]).ToList();
        for (int i = 0; i < en.Keys.Count; i++)
        {
            var s = ukSections[i];
            ukByKey[en.Keys[i]] = new Section { Key = en.Keys[i], Title = s.Title, Html = s.Html, Subs = s.Subs };
        }
        uk.Sections.Clear();
        foreach (var (k, v) in ukByKey) uk.Sections[k] = v;
        uk.Keys.Clear();
        uk.Keys.AddRange(en.Keys);

        foreach (var k in en.Keys)
            if (en.Sections[k].Subs.Count != uk.Sections[k].Subs.Count)
                problems.Add($"The descriptions differ: '{en.Sections[k].Title}' has {en.Sections[k].Subs.Count} ### subsections in en and {uk.Sections[k].Subs.Count} in uk");
        if (en.Items.Count != uk.Items.Count)
            problems.Add($"The descriptions differ: {en.Items.Count} list items in en and {uk.Items.Count} in uk");

        // The same sequences (names and numbers of frames) in the same order: a sequence is a translation of the English one.
        string Seqs(DescriptionData d) => string.Join(", ", d.Sequences.Select(s => $"{s.Name} ({s.Frames})"));
        if (Seqs(en) != Seqs(uk))
            problems.Add($"The descriptions differ in sequences: en has [{Seqs(en)}], uk has [{Seqs(uk)}]");
    }

    /// <summary>A link into the guide with a fragment must hit a heading of that file, by GitHub's rule for anchors; the build fails on a renamed or missing one.</summary>
    static void CheckGuideAnchors(DescriptionData en, DescriptionData uk, PageInput input, List<string> problems)
    {
        var anchors = new Dictionary<string, HashSet<string>>
        {
            [Contract.GuideEnPath] = GuideHeadingAnchors(input.GuideEn),
            [Contract.GuideUkPath] = GuideHeadingAnchors(input.GuideUk),
        };
        foreach (var (lang, d) in new[] { ("en", en), ("uk", uk) })
            foreach (var (file, anchor) in d.GuideAnchors)
            {
                if (!anchors.TryGetValue(file, out var set))
                    problems.Add($"Description ({lang}): a link to {file}#{anchor}: the generator knows only {Contract.GuideEnPath} and {Contract.GuideUkPath}");
                else if (!set.Contains(anchor))
                    problems.Add($"Description ({lang}): a link to {file}#{anchor}, but that file has no such heading");
            }
    }

    static HashSet<string> GuideHeadingAnchors(string markdown) =>
        Markdig.Markdown.Parse(markdown).Descendants<HeadingBlock>()
            .Select(h => GithubAnchor(Description.PlainText(h.Inline))).ToHashSet();

    /// <summary>GitHub's anchor of a heading: lower case, letters, digits, '-' and '_' kept, spaces become '-', the rest dropped.</summary>
    public static string GithubAnchor(string heading)
    {
        var kept = Regex.Replace(heading.Trim().ToLowerInvariant(), @"[^\p{L}\p{N}_ -]", "");
        return kept.Replace(' ', '-');
    }

    /// <summary>ModDB drops &lt;style&gt;, so what PreMailer cannot inline would be lost: no media queries, hover or pseudo-elements.</summary>
    static void CheckTemplateCss(string template, List<string> problems)
    {
        var m = Regex.Match(template, "<style[^>]*>(.*?)</style>", RegexOptions.Singleline);
        if (!m.Success) { problems.Add("The template has no <style>"); return; }
        foreach (var bad in new[] { "@media", ":hover", ":focus", ":active", "::", ":before", ":after", ":first-letter", ":first-line" })
            if (m.Groups[1].Value.Contains(bad))
                problems.Add($"The template's CSS uses '{bad}', which cannot be inlined (ModDB drops <style>)");
    }

    static string Fill(PageInput input, DescriptionData en, DescriptionData uk, List<string> problems)
    {
        var template = Template.Parse(input.Template, "template.html");
        if (template.HasErrors)
        {
            problems.AddRange(template.Messages.Select(m => "Template: " + m));
            return "";
        }

        var model = new ScriptObject();
        model.Import(new
        {
            version = input.Version,
            game_version = input.GameVersion,
            side = input.Side,
            early = input.Version.StartsWith("0."),
            lang_codes = input.LangCodes,
            raw_base = new Links(input.RawBase, input.BlobBase).RawBase,
            repo_url = input.RepoUrl,
            moddb_links = input.ModDbLinks,
            section_keys = Contract.SectionKeys,
            guide_url_en = new Links(input.RawBase, input.BlobBase).BlobBase + Contract.GuideEnPath,
            guide_url_uk = new Links(input.RawBase, input.BlobBase).BlobBase + Contract.GuideUkPath,
            readme_url = new Links(input.RawBase, input.BlobBase).BlobBase + Contract.ReadmePath,
            en,
            uk,
        });
        var context = new TemplateContext { StrictVariables = true };
        context.PushGlobal(model);
        try
        {
            return template.Render(context);
        }
        catch (Exception ex) when (ex is Scriban.Syntax.ScriptRuntimeException)
        {
            problems.Add("Template: " + ex.Message);
            return "";
        }
    }

    /// <summary>
    /// The size of the page is ModDB's limit (<see cref="Contract.MaxPageBytes"/>), and the styles are inline, repeated on every element:
    /// no space after the colon, and <c>margin</c>/<c>padding</c> in the shortest form that means the same (<c>0 0 6px 0</c> is <c>0 0 6px</c>).
    /// </summary>
    public static string Shorten(string style)
    {
        style = Regex.Replace(style, ":\\s+", ":");
        return Regex.Replace(style, "(?<![-\\w])(margin|padding):([^;]+)", m =>
        {
            var v = m.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (v.Count == 4 && v[3] == v[1]) v.RemoveAt(3);
            if (v.Count == 3 && v[2] == v[0]) v.RemoveAt(2);
            if (v.Count == 2 && v[1] == v[0]) v.RemoveAt(1);
            return m.Groups[1].Value + ":" + string.Join(' ', v);
        });
    }

    /// <summary>Classes of ModDB's native spoiler: <c>div.spoiler</c> with <c>spoiler-toggle</c> and <c>spoiler-text</c>.</summary>
    static readonly HashSet<string> SiteClasses = new() { "spoiler", "spoiler-toggle", "spoiler-text" };

    static string Inline(string html, List<string> problems)
    {
        var result = PreMailer.Net.PreMailer.MoveCssInline(html, removeStyleElements: true, removeComments: true);
        foreach (var w in result.Warnings) problems.Add("CSS could not be inlined: " + w);

        var body = Regex.Match(result.Html, "<body[^>]*>(.*)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var page = body.Success ? body.Groups[1].Value : result.Html;

        page = Regex.Replace(page, "<!--.*?-->", "", RegexOptions.Singleline);
        // Only ModDB's own spoiler keeps its classes (the site's CSS and script style it); ours are inlined and dropped.
        page = Regex.Replace(page, "\\sclass=\"([^\"]*)\"", m => SiteClasses.Contains(m.Groups[1].Value) ? m.Value : "");
        // PreMailer writes "prop: value;prop: value"; the space after the colon is 23 KB of bytes on the real page and means nothing.
        page = Regex.Replace(page, "\\sstyle=\"([^\"]*)\"", m => " style=\"" + Shorten(m.Groups[1].Value) + "\"");
        page = page.Trim() + Environment.NewLine;

        if (Regex.IsMatch(page, "<style", RegexOptions.IgnoreCase))
            problems.Add("The page still has a <style>: some CSS of the template cannot be inlined");
        foreach (Match m in Regex.Matches(page, "\\s(?:href|src)=\"([^\"]*)\""))
        {
            var url = m.Groups[1].Value;
            if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith('#') && !url.StartsWith("file:///"))
                problems.Add($"A relative or unknown link is left in the page: {url}");
        }
        return page;
    }
}
