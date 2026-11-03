using System.Text.RegularExpressions;
using PreMailer.Net;
using Scriban;
using Scriban.Runtime;

namespace CurioCatalogue.PageGen;

/// <summary>Everything the generator needs, as values: reading files is the caller's job.</summary>
public sealed class PageInput
{
    public required string ReadmeEn { get; init; }
    public required string ReadmeUk { get; init; }
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
    /// <summary>Source and Report a bug in the row of buttons (ModDB's card has them too).</summary>
    public bool ModDbLinks { get; init; }
}

/// <summary>README + template → the HTML that is pasted into ModDB's editor (docs/plans/2026-11-01-mod-page.md, "The generator's shape").</summary>
public static class ModDbPage
{
    public static string Generate(PageInput input)
    {
        var problems = new List<string>();
        var links = new Links(input.RawBase, input.BlobBase);
        var en = Readme.Parse(input.ReadmeEn, "en", links, problems);
        var uk = Readme.Parse(input.ReadmeUk, "uk", links, problems, en.Keys.IndexOf(Contract.InsideKey));
        CrossCheck(en, uk, problems);
        CheckTemplateCss(input.Template, problems);
        if (problems.Count > 0) throw new ContractException(problems);

        var html = Fill(input, en, uk, problems);
        if (problems.Count > 0) throw new ContractException(problems);

        html = Inline(html, problems);
        if (problems.Count > 0) throw new ContractException(problems);
        return html;
    }

    static void CrossCheck(ReadmeData en, ReadmeData uk, List<string> problems)
    {
        var known = Contract.AllKeys.ToHashSet();
        foreach (var k in en.Keys.Where(k => !known.Contains(k)))
            problems.Add($"README (en): the section '{k}' is not known to the template; add it to the template and to Contract, or rename it");
        foreach (var k in known.Where(k => !en.Keys.Contains(k)))
            problems.Add($"README (en): the section '{k}' is missing, the template shows it");

        if (en.Keys.Count != uk.Keys.Count)
        {
            problems.Add($"The READMEs differ: en has {en.Keys.Count} sections, uk has {uk.Keys.Count}");
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

        if (en.Sections.TryGetValue(Contract.InsideKey, out var ei) && uk.Sections.TryGetValue(Contract.InsideKey, out var ui))
        {
            if (ei.Subs.Count != Contract.InsideSubsections)
                problems.Add($"README (en): '{ei.Title}' has {ei.Subs.Count} ### subsections, the page shows {Contract.InsideSubsections}");
            if (ei.Subs.Count != ui.Subs.Count)
                problems.Add($"The READMEs differ: '{ei.Title}' has {ei.Subs.Count} ### subsections in en and {ui.Subs.Count} in uk");
        }
        if (en.Items.Count != uk.Items.Count)
            problems.Add($"The READMEs differ: {en.Items.Count} list items in en and {uk.Items.Count} in uk");
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

    static string Fill(PageInput input, ReadmeData en, ReadmeData uk, List<string> problems)
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
            known_keys = new { accordions = Contract.AccordionKeys, other = Contract.OtherKeys },
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

    static string Inline(string html, List<string> problems)
    {
        var result = PreMailer.Net.PreMailer.MoveCssInline(html, removeStyleElements: true, removeComments: true);
        foreach (var w in result.Warnings) problems.Add("CSS could not be inlined: " + w);

        var body = Regex.Match(result.Html, "<body[^>]*>(.*)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var page = body.Success ? body.Groups[1].Value : result.Html;

        page = Regex.Replace(page, "<!--.*?-->", "", RegexOptions.Singleline);
        page = Regex.Replace(page, "\\sclass=\"[^\"]*\"", "");
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
