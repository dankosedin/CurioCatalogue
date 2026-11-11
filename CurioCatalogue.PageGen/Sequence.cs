using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CurioCatalogue.PageGen;

/// <summary>
/// The <c>::: sequence &lt;name&gt;</c> block of the description: frames, each an image paragraph and a caption paragraph under it,
/// rendered as a row of frames one per width with arrows on anchors (Decisions, "Sequences on ModDB", of
/// docs/plans/2026-11-04-mod-description.md). The look is the template's CSS (classes seq, seq-row, seq-frame, seq-nav).
/// </summary>
public static class Sequence
{
    /// <summary>A sequence as the cross-check of the two languages sees it.</summary>
    public sealed record Info(string Name, int Frames);

    /// <summary>The words of the arrows, by language; the page shows en and uk only.</summary>
    static readonly Dictionary<string, (string Back, string Next, string First)> Words = new()
    {
        ["en"] = ("← back", "next →", "first"),
        ["uk"] = ("← назад", "далі →", "на початок"),
    };

    /// <summary>Only the block parser of Markdig's custom containers: their inline form (<c>::text::</c>) is not wanted.</summary>
    public sealed class Extension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<CustomContainerParser>())
                pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new CustomContainerParser());
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) { }
    }

    /// <summary>
    /// Replaces every <c>:::</c> block of the document by the HTML of its sequence; anything else written with <c>:::</c> is a problem.
    /// Links must already be absolute. <paramref name="source"/> is the description's text, to see whether a block is closed.
    /// </summary>
    public static List<Info> RenderAll(MarkdownDocument doc, string source, string lang, string where, List<string> problems, Func<Block, string> render)
    {
        var infos = new List<Info>();
        var names = new HashSet<string>();
        foreach (var c in doc.Descendants<CustomContainer>().ToList())
        {
            int line = c.Line + 1;
            if (c.Info != "sequence")
            {
                problems.Add($"{where}: an unknown block '::: {c.Info}' (line {line}); only '::: sequence <name>' is known");
                continue;
            }
            string name = (c.Arguments ?? "").Trim();
            if (!Regex.IsMatch(name, "^[a-z0-9]+(-[a-z0-9]+)*$"))
                problems.Add($"{where}: the sequence on line {line} needs a name of lower-case letters, digits and '-': '::: sequence album-chest'");
            else if (!names.Add(name))
                problems.Add($"{where}: two sequences named '{name}'");
            if (!source.AsSpan(c.Span.Start, Math.Min(c.Span.Length, source.Length - c.Span.Start)).TrimEnd().EndsWith(":::"))
                problems.Add($"{where}: the sequence on line {line} is not closed with ':::'");

            var frames = Frames(c, where, line, problems);
            infos.Add(new Info(name, frames.Count));
            string prefix = "seq-" + (lang == "en" ? "" : lang + "-") + name + "-";
            var html = Html(frames, prefix, Words.GetValueOrDefault(lang, Words["en"]), render);
            Replace(c, html);
        }
        return infos;
    }

    /// <summary>The frames: image paragraph, caption paragraph, again and again; at least two.</summary>
    static List<(LinkInline Image, ParagraphBlock Caption)> Frames(CustomContainer c, string where, int line, List<string> problems)
    {
        var frames = new List<(LinkInline, ParagraphBlock)>();
        var blocks = c.ToList();
        for (int i = 0; i < blocks.Count; i += 2)
        {
            var img = blocks[i] is ParagraphBlock p ? Description.ImageOf(p) : null;
            var caption = i + 1 < blocks.Count ? blocks[i + 1] as ParagraphBlock : null;
            if (img == null || caption == null
                || caption.Inline?.Descendants<LinkInline>().Any(l => l.IsImage) == true)
            {
                problems.Add($"{where}: the sequence on line {line}: a frame is an image in a paragraph of its own and a caption paragraph under it (line {blocks[i].Line + 1})");
                return frames;
            }
            frames.Add((img, caption));
        }
        if (frames.Count < 2) problems.Add($"{where}: the sequence on line {line} has {frames.Count} frame(s); a sequence has two or more");
        return frames;
    }

    static string Html(List<(LinkInline Image, ParagraphBlock Caption)> frames, string prefix, (string Back, string Next, string First) w, Func<Block, string> render)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"seq\"><div class=\"seq-row\">\n");
        int n = frames.Count;
        for (int i = 0; i < n; i++)
        {
            var (img, caption) = frames[i];
            string alt = WebUtility.HtmlEncode(Description.PlainText(img));
            string src = WebUtility.HtmlEncode(img.Url ?? "");
            string captionHtml = Description.StripOuter(render(caption), "p");
            sb.Append($"<figure class=\"seq-frame\" id=\"{prefix}{i + 1}\"><img src=\"{src}\" alt=\"{alt}\">");
            sb.Append($"<figcaption><span class=\"seq-caption\">{captionHtml}</span><span class=\"seq-nav\">");
            if (i > 0) sb.Append($"<a href=\"#{prefix}{i}\">{w.Back}</a> ");
            sb.Append($"{i + 1} / {n} ");
            sb.Append(i < n - 1 ? $"<a href=\"#{prefix}{i + 2}\">{w.Next}</a>" : $"<a href=\"#{prefix}1\">{w.First}</a>");
            sb.Append("</span></figcaption></figure>\n");
        }
        sb.Append("</div></div>\n");
        return sb.ToString();
    }

    /// <summary>The container gives its place to a raw HTML block, which the renderer writes as it is.</summary>
    static void Replace(CustomContainer c, string html)
    {
        var parent = c.Parent!;
        int at = parent.IndexOf(c);
        var block = new HtmlBlock(null) { Type = HtmlBlockType.NonInterruptingBlock, Line = c.Line, Span = c.Span };
        var lines = new StringLineGroup(1);
        foreach (var l in html.TrimEnd('\n').Split('\n')) lines.Add(new StringSlice(l));
        block.Lines = lines;
        parent.RemoveAt(at);
        parent.Insert(at, block);
    }
}
