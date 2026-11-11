using CurioCatalogue.PageGen;

namespace CurioCatalogue.PageGen.Tests;

/// <summary>The generator on small descriptions that keep the contract, and on descriptions that break it one way at a time.</summary>
public class ModDbPageTests
{
    const string Raw = "https://raw.githubusercontent.com/dankosedin/CurioCatalogue/v9.9.9/";
    const string Blob = "https://github.com/dankosedin/CurioCatalogue/blob/v9.9.9/";

    static string Template()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "docs", "moddb", "template.html"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "docs", "moddb", "template.html"));
    }

    // The section titles of the sample; the Ukrainian description has other words at the same places.
    static readonly string[] EnTitles =
    {
        "What's inside", "Getting started", "Collections", "Gestures", "Fair play on servers", "Configuration",
        "Compatibility and known limitations", "Removing the mod", "Languages", "Contributing",        "AI and human work", "Credits", "License",
    };

    /// <summary>A description in docs/moddb/: head, then the sections with a line of text each; "What's inside" with its list and subsections.</summary>
    static string Doc(string lang, Func<string, string>? edit = null, int items = 6, int subs = 3, string head = "")
    {
        bool en = lang == "en";
        var sb = new System.Text.StringBuilder();
        sb.Append($"# Title {lang}\n\nThe hook {lang}.\n\n");
        sb.Append(head);
        sb.Append("> **Early development.** Back up your world.\n\n");
        sb.Append("![The hero](../images/hero.png)\n\n");
        for (int i = 0; i < EnTitles.Length; i++)
        {
            string title = en ? EnTitles[i] : $"Section {i} uk";
            sb.Append($"## {title}\n\n");
            if (i == 0)
            {
                for (int n = 1; n <= items; n++) sb.Append($"- **Item {n}** of {lang}.\n");
                sb.Append($"\nThe paragraph under the list ({lang}).\n\n");
                for (int s = 1; s <= subs; s++) sb.Append($"### Sub {s} {lang}\n\nText of sub {s}.\n\n");
            }
            else
            {
                sb.Append($"Text of {title}, see [the guide](../guide.md) and `CONTRIBUTING.md`.\n\n");
                if (i == 1) sb.Append("![A shot](../images/shot.en.png)\n\n");
            }
        }
        var text = sb.ToString();
        return edit == null ? text : edit(text);
    }

    static PageInput Input(string? en = null, string? uk = null, string? template = null, bool links = false) => new()
    {
        DescriptionEn = en ?? Doc("en"),
        DescriptionUk = uk ?? Doc("uk"),
        Template = template ?? Template(),
        Version = "0.1.1",
        GameVersion = "1.22.7",
        LangCodes = new[] { "en", "uk" },
        RawBase = Raw,
        BlobBase = Blob,
        RepoUrl = "https://github.com/dankosedin/CurioCatalogue",
        ModDbLinks = links,
    };

    static ContractException Fails(PageInput input) => Assert.Throws<ContractException>(() => ModDbPage.Generate(input));

    // --- the output --------------------------------------------------------------------------------------------------

    [Fact]
    public void Output_has_no_style_no_comments_and_no_relative_links()
    {
        var html = ModDbPage.Generate(Input());

        Assert.DoesNotContain("<style", html);
        Assert.DoesNotContain("<!--", html);
        Assert.DoesNotContain("class=", html);
        Assert.DoesNotContain("@media", html);
        Assert.DoesNotContain("<html", html);
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(html, "(?:href|src)=\"([^\"]*)\""))
            Assert.Matches("^(https://|#)", m.Groups[1].Value);
    }

    [Fact]
    public void Comments_in_the_template_body_are_removed()
    {
        var template = Template().Replace("<div class=\"hero\">", "<!-- a note --><div class=\"hero\">");
        Assert.Contains("a note", template);
        Assert.DoesNotContain("a note", ModDbPage.Generate(Input(template: template)));
    }

    [Fact]
    public void Css_is_inline()
    {
        var html = ModDbPage.Generate(Input());

        Assert.Contains("max-width:960px", html);          // the card
        Assert.Contains("flex-wrap:wrap", html);            // the hero
        Assert.Contains("scroll-margin-top", html);          // anchors
        Assert.Matches("<summary style=\"[^\"]*cursor:pointer", html);
    }

    [Fact]
    public void Relative_images_and_links_point_to_the_ref()
    {
        var html = ModDbPage.Generate(Input());

        Assert.Contains($"src=\"{Raw}docs/images/hero.png\"", html);
        Assert.Contains($"src=\"{Raw}docs/images/shot.en.png\"", html);
        Assert.Contains($"href=\"{Blob}docs/guide.md\"", html);
        Assert.Contains($"src=\"{Raw}docs/images/icons/book-album.png\"", html);   // the template's own icons
    }

    [Fact]
    public void Both_languages_have_anchors_and_the_ukrainian_one_is_nested()
    {
        var html = ModDbPage.Generate(Input());

        foreach (var key in new[] { "whats-inside", "getting-started", "gestures", "configuration", "other" })
        {
            Assert.Contains($"id=\"cc-{key}\"", html);
            Assert.Contains($"id=\"cc-uk-{key}\"", html);
            if (key != "other")
            {
                Assert.Contains($"href=\"#cc-{key}\"", html);
                Assert.Contains($"href=\"#cc-uk-{key}\"", html);
            }
        }
        Assert.Contains("id=\"cc-uk\"", html);
        Assert.True(html.IndexOf("id=\"cc-uk\"", StringComparison.Ordinal) < html.IndexOf("id=\"cc-uk-gestures\"", StringComparison.Ordinal));
        Assert.Contains("Title uk", html);
    }

    [Fact]
    public void Whats_inside_and_getting_started_are_open_and_the_rest_closed()
    {
        var html = ModDbPage.Generate(Input());

        Assert.Equal(4, System.Text.RegularExpressions.Regex.Matches(html, "<details open").Count);   // What's inside and Getting started, en and uk
    }

    [Fact]
    public void Hero_and_badges_come_from_data()
    {
        var html = ModDbPage.Generate(Input());

        Assert.Contains(">0.1.1<", html);
        Assert.Contains("VS 1.22.7", html);
        Assert.Contains("Server and client", html);
        Assert.Contains("English · Українська", html);
        Assert.Contains("Early development", html);
        Assert.Contains("The hook en.", html);
        Assert.Contains("Back up your world", html);
    }

    [Fact]
    public void Early_development_badge_goes_with_version_zero()
    {
        var input = new PageInput
        {
            DescriptionEn = Doc("en"), DescriptionUk = Doc("uk"), Template = Template(), Version = "1.0.0", GameVersion = "1.22.7",
            LangCodes = new[] { "en" }, RawBase = Raw, BlobBase = Blob, RepoUrl = "https://example.org/r",
        };
        var html = ModDbPage.Generate(input);

        Assert.DoesNotContain("Early development</span>", html);
        Assert.DoesNotContain("Українська", html.Replace("Title uk", "").Replace("Українською", ""));   // only English listed
    }

    [Fact]
    public void Icon_table_has_a_row_and_an_icon_per_item()
    {
        var html = ModDbPage.Generate(Input());

        Assert.Equal(12, System.Text.RegularExpressions.Regex.Matches(html, "docs/images/icons/").Count);   // 6 in each language
        foreach (var icon in new[] { "book-album", "invoice-wantlist", "book-ledger", "book-materials", "invoice-incoming", "invoice-outgoing" })
            Assert.Contains($"icons/{icon}.png", html);
    }

    [Fact]
    public void Source_and_bug_buttons_only_when_asked()
    {
        Assert.DoesNotContain("/issues", ModDbPage.Generate(Input(links: false)));
        Assert.Contains("https://github.com/dankosedin/CurioCatalogue/issues", ModDbPage.Generate(Input(links: true)));
    }

    // --- the contract ------------------------------------------------------------------------------------------------

    [Fact]
    public void Six_items_are_required()
    {
        var e = Fails(Input(en: Doc("en", items: 5)));
        Assert.Contains(e.Problems, p => p.Contains("5 items") && p.Contains("(en)"));
    }

    [Fact]
    public void Three_subsections_are_required_and_the_same_in_both_languages()
    {
        var e = Fails(Input(uk: Doc("uk", subs: 2)));
        Assert.Contains(e.Problems, p => p.Contains("3 ### subsections in en and 2 in uk"));
    }

    [Fact]
    public void A_section_the_template_does_not_know_is_an_error()
    {
        var e = Fails(Input(en: Doc("en", t => t + "## Surprise\n\nText.\n\n"), uk: Doc("uk", t => t + "## Сюрприз\n\nТекст.\n\n")));
        Assert.Contains(e.Problems, p => p.Contains("'surprise' is not known"));
    }

    [Fact]
    public void A_section_in_one_language_only_is_an_error()
    {
        var e = Fails(Input(en: Doc("en", t => t.Replace("## License\n", "## Licence\n").Replace("## Credits", "## Credits\n\n## Extra"))));
        Assert.Contains(e.Problems, p => p.Contains("differ") || p.Contains("not known"));
    }

    [Fact]
    public void A_missing_section_is_an_error()
    {
        var e = Fails(Input(en: Doc("en", t => t.Replace("## Gestures", "## Moves"))));
        Assert.Contains(e.Problems, p => p.Contains("'gestures' is missing"));
        Assert.Contains(e.Problems, p => p.Contains("'moves' is not known"));
    }

    [Fact]
    public void Html_in_the_description_is_an_error()
    {
        var e = Fails(Input(en: Doc("en", t => t.Replace("The hook en.", "The hook <b>en</b>."))));
        Assert.Contains(e.Problems, p => p.Contains("HTML in the description"));

        e = Fails(Input(uk: Doc("uk", t => t + "<!-- note -->\n\n")));
        Assert.Contains(e.Problems, p => p.Contains("HTML in the description"));
    }

    [Fact]
    public void The_head_needs_a_hook_a_warning_and_an_image()
    {
        var e = Fails(Input(en: Doc("en", t => t.Replace("The hook en.\n\n", ""))));
        Assert.Contains(e.Problems, p => p.Contains("hook"));

        e = Fails(Input(en: Doc("en", t => t.Replace("> **Early development.** Back up your world.\n\n", ""))));
        Assert.Contains(e.Problems, p => p.Contains("warning"));

        e = Fails(Input(uk: Doc("uk", t => t.Replace("![The hero](../images/hero.png)\n\n", ""))));
        Assert.Contains(e.Problems, p => p.Contains("(uk)") && p.Contains("image"));
    }

    [Fact]
    public void A_block_in_the_head_the_page_does_not_show_is_an_error()
    {
        var e = Fails(Input(uk: Doc("uk", head: "Download: [ModDB](https://mods.vintagestory.at/x).\n\n")));
        Assert.Contains(e.Problems, p => p.Contains("(uk)") && p.Contains("the head has a block the page does not show (line 5)"));

        e = Fails(Input(en: Doc("en", t => t.Replace("![The hero]", "> A second quote.\n\n![The hero]"))));
        Assert.Contains(e.Problems, p => p.Contains("(en)") && p.Contains("the head has a block"));
    }

    [Fact]
    public void A_path_above_the_repository_is_an_error()
    {
        var e = Fails(Input(en: Doc("en", t => t.Replace("](../guide.md)", "](../../../guide.md)"))));
        Assert.Contains(e.Problems, p => p.Contains("relative or unknown link") && p.Contains("../../../guide.md"));
    }

    [Fact]
    public void Every_problem_is_reported_not_only_the_first()
    {
        var e = Fails(Input(en: Doc("en", items: 4).Replace("> **Early development.** Back up your world.\n\n", ""), uk: Doc("uk", subs: 1)));

        Assert.True(e.Problems.Count >= 3, string.Join("; ", e.Problems));
        Assert.Contains("4 items", e.Message);
        Assert.Contains("warning", e.Message);
    }

    // --- sequences ---------------------------------------------------------------------------------------------------

    /// <summary>A sequence of <paramref name="frames"/> frames named <paramref name="name"/>, as the description writes it.</summary>
    static string Seq(string lang, int frames = 3, string name = "album-chest")
    {
        var sb = new System.Text.StringBuilder($"::: sequence {name}\n");
        for (int n = 1; n <= frames; n++)
            sb.Append($"![Frame {n} {lang}](../images/{name}-{n}.{lang}.png)\n\nCaption {n} of {lang}, **bold**.\n\n");
        return sb.Append(":::\n\n").ToString();
    }

    /// <summary>The description with <paramref name="seq"/> put into "Getting started" (the first section after "What's inside"), before its shot.</summary>
    static string WithSeq(string lang, string seq) => Doc(lang, t => t.Replace("![A shot]", seq + "![A shot]"));

    static PageInput SeqInput(string? en = null, string? uk = null) =>
        Input(en: WithSeq("en", en ?? Seq("en")), uk: WithSeq("uk", uk ?? Seq("uk")));

    [Fact]
    public void A_sequence_is_a_row_of_frames_with_captions_and_no_arrows()
    {
        var html = ModDbPage.Generate(SeqInput());

        for (int n = 1; n <= 3; n++)
        {
            Assert.Contains($"src=\"{Raw}docs/images/album-chest-{n}.en.png\" alt=\"Frame {n} en\"", html);
            Assert.Contains($"<figcaption style=\"padding:6px 2px 4px;font-size:14px\">Caption {n} of en, <strong>bold</strong>.</figcaption>", html);
            Assert.Contains($"Caption {n} of uk, <strong>bold</strong>.", html);
        }
        Assert.DoesNotContain(":::", html);
        // No arrows, no "n / N", no ids on the frames: they cost bytes (ModDB's size limit) and the scroll bar and the swipe are enough.
        Assert.DoesNotContain("seq-", html);
        Assert.DoesNotContain(" / 3", html);
        Assert.DoesNotContain("next →", html);
        Assert.DoesNotContain("далі", html);
        Assert.Equal(6, System.Text.RegularExpressions.Regex.Matches(html, "<figure ").Count);
    }

    [Fact]
    public void A_sequence_scrolls_one_frame_per_width()
    {
        var html = ModDbPage.Generate(SeqInput());

        Assert.Matches("<div style=\"[^\"]*max-width:900px[^\"]*\"><div style=\"[^\"]*overflow-x:auto[^\"]*scroll-snap-type:x mandatory", html);
        Assert.Matches("<figure style=\"[^\"]*flex:0 0 100%[^\"]*scroll-snap-align:start", html);
        Assert.Matches("<img src=\"[^\"]*album-chest-1[^>]*style=\"[^\"]*max-width:none", html);   // not the 420 px of the body's images
    }

    // --- size --------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_page_over_the_limit_is_an_error_with_its_size()
    {
        var html = ModDbPage.Generate(Input());
        int bytes = System.Text.Encoding.UTF8.GetByteCount(html);

        ModDbPage.Generate(Input() with { MaxBytes = bytes });     // exactly the limit passes
        var e = Fails(Input() with { MaxBytes = bytes - 1 });
        Assert.Contains(e.Problems, p => p.Contains($"{bytes} bytes") && p.Contains($"{bytes - 1}"));
    }

    [Fact]
    public void The_limit_counts_bytes_not_characters()
    {
        // Cyrillic is two bytes a letter: ModDB refused a page of 58 841 characters and 66 176 bytes (G3b).
        var html = ModDbPage.Generate(Input());
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(html) > html.Length);
        var e = Fails(Input() with { MaxBytes = html.Length });
        Assert.Contains(e.Problems, p => p.Contains(" bytes"));
    }

    [Theory]
    [InlineData("margin: 0 0 6px 0", "margin:0 0 6px")]
    [InlineData("margin: 8px 0 8px 0", "margin:8px 0")]
    [InlineData("padding: 0 16px 12px 16px", "padding:0 16px 12px")]
    [InlineData("padding: 24px 28px", "padding:24px 28px")]
    [InlineData("margin: 0 auto", "margin:0 auto")]
    [InlineData("margin: 4px 4px", "margin:4px")]
    [InlineData("margin: 1px 2px 3px 4px", "margin:1px 2px 3px 4px")]
    [InlineData("scroll-margin-top: 56px;padding: 0 16px 12px 16px", "scroll-margin-top:56px;padding:0 16px 12px")]
    [InlineData("box-shadow: 0 2px 10px rgba(60, 40, 20, 0.12);font-size: 15px", "box-shadow:0 2px 10px rgba(60, 40, 20, 0.12);font-size:15px")]
    public void Styles_are_written_in_the_shortest_form_that_means_the_same(string style, string expected) =>
        Assert.Equal(expected, ModDbPage.Shorten(style));

    [Fact]
    public void A_table_in_the_text_has_borders_and_padding()
    {
        var table = "| Collection | Made from |\n|---|---|\n| Tapestries | a tapestry piece |\n\n";
        var html = ModDbPage.Generate(Input(en: Doc("en", t => t.Replace("![A shot]", table + "![A shot]")), uk: Doc("uk", t => t.Replace("![A shot]", table + "![A shot]"))));

        Assert.Matches("<table style=\"[^\"]*border-collapse:collapse", html);
        Assert.Matches("<th style=\"[^\"]*border:1px solid[^\"]*padding:5px 12px|<th style=\"[^\"]*padding:5px 12px[^\"]*border:1px solid", html);
        Assert.Matches("<td style=\"[^\"]*padding:5px 12px[^\"]*\">Tapestries", html);
    }

    [Fact]
    public void The_page_has_no_space_after_a_colon_in_styles()
    {
        var html = ModDbPage.Generate(Input());
        Assert.DoesNotMatch("style=\"[^\"]*: ", html);
    }

    [Fact]
    public void A_description_without_sequences_has_no_sequence_markup()
    {
        var html = ModDbPage.Generate(Input());
        Assert.DoesNotContain("<figure", html);
    }

    [Theory]
    [InlineData("::: note\n\nText.\n\n:::\n\n", "unknown block '::: note'")]
    [InlineData("::: sequence\n\n![a](../images/a.png)\n\nA.\n\n![b](../images/b.png)\n\nB.\n\n:::\n\n", "needs a name")]
    [InlineData("::: sequence Album Chest\n\n![a](../images/a.png)\n\nA.\n\n![b](../images/b.png)\n\nB.\n\n:::\n\n", "needs a name")]
    [InlineData("::: sequence one\n\n![a](../images/a.png)\n\nA.\n\n:::\n\n", "1 frame(s)")]
    [InlineData("::: sequence nocap\n\n![a](../images/a.png)\n\n![b](../images/b.png)\n\nB.\n\n:::\n\n", "a frame is an image")]
    [InlineData("::: sequence textfirst\n\nA.\n\n![a](../images/a.png)\n\n:::\n\n", "a frame is an image")]
    [InlineData("::: sequence inline\n\n![a](../images/a.png)\nA, glued to the image.\n\n![b](../images/b.png)\n\nB.\n\n:::\n\n", "a frame is an image")]
    [InlineData("::: sequence icon\n\n![a](../images/a.png)\n\nA with ![an icon](../images/i.png) in it.\n\n![b](../images/b.png)\n\nB.\n\n:::\n\n", "a frame is an image")]
    public void A_broken_sequence_is_an_error(string seq, string message)
    {
        var e = Fails(SeqInput(en: seq));
        Assert.Contains(e.Problems, p => p.Contains("(en)") && p.Contains(message));
    }

    [Fact]
    public void An_unclosed_sequence_is_an_error()
    {
        var e = Fails(Input(en: Doc("en") + Seq("en").Replace(":::\n\n", ""), uk: Doc("uk") + Seq("uk")));
        Assert.Contains(e.Problems, p => p.Contains("(en)") && p.Contains("not closed"));
    }

    [Fact]
    public void Two_sequences_with_one_name_are_an_error()
    {
        var e = Fails(SeqInput(en: Seq("en") + Seq("en"), uk: Seq("uk") + Seq("uk")));
        Assert.Contains(e.Problems, p => p.Contains("(en)") && p.Contains("two sequences named 'album-chest'"));
    }

    [Fact]
    public void Sequences_differ_between_languages_is_an_error()
    {
        var e = Fails(SeqInput(uk: Seq("uk", frames: 2)));
        Assert.Contains(e.Problems, p => p.Contains("differ in sequences") && p.Contains("album-chest (3)") && p.Contains("album-chest (2)"));

        e = Fails(SeqInput(uk: Seq("uk", name: "other")));
        Assert.Contains(e.Problems, p => p.Contains("differ in sequences"));
    }

    // --- the template ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("@media (max-width: 600px) { .cc { padding: 0; } }")]
    [InlineData(".cc a:hover { color: red; }")]
    [InlineData(".cc p::first-line { color: red; }")]
    public void Css_that_cannot_be_inlined_is_an_error(string css)
    {
        var template = Template().Replace("</style>", css + "\n</style>");
        var e = Fails(Input(template: template));
        Assert.Contains(e.Problems, p => p.Contains("cannot be inlined"));
    }

    [Fact]
    public void A_broken_template_is_reported()
    {
        var e = Fails(Input(template: "<style>.cc { color: red; }</style>{{ if }}"));
        Assert.Contains(e.Problems, p => p.StartsWith("Template:"));
    }

    [Fact]
    public void An_unknown_variable_in_the_template_is_reported()
    {
        var e = Fails(Input(template: "<style>.cc { color: red; }</style><div class=\"cc\">{{ no_such_thing }}</div>"));
        Assert.Contains(e.Problems, p => p.StartsWith("Template:") && p.Contains("no_such_thing"));
    }

    // --- pieces ------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("What's inside", "whats-inside")]
    [InlineData("What’s inside", "whats-inside")]
    [InlineData("Compatibility and known limitations", "compatibility-and-known-limitations")]
    [InlineData("AI and human work", "ai-and-human-work")]
    public void Slug_of_a_title(string title, string slug) => Assert.Equal(slug, Description.Slug(title));

    [Theory]
    [InlineData("docs/a b.png", true, Raw + "docs/a%20b.png")]
    [InlineData("./CONTRIBUTING.md", false, Blob + "CONTRIBUTING.md")]
    [InlineData("https://example.org/x.png", true, "https://example.org/x.png")]
    [InlineData("#gestures", false, "#gestures")]
    [InlineData("mailto:a@b.c", false, "mailto:a@b.c")]
    public void Relative_urls_are_made_absolute(string url, bool image, string expected) =>
        Assert.Equal(expected, new Links(Raw, Blob).Absolute(url, image));

    [Theory]
    [InlineData("../images/a.png", true, Raw + "docs/images/a.png")]
    [InlineData("./template.html", false, Blob + "docs/moddb/template.html")]
    [InlineData("../../CONTRIBUTING.md", false, Blob + "CONTRIBUTING.md")]
    [InlineData("../../../x.md", false, "../../../x.md")]
    [InlineData("#gestures", false, "#gestures")]
    public void Relative_urls_are_read_from_the_descriptions_folder(string url, bool image, string expected) =>
        Assert.Equal(expected, new Links(Raw, Blob, "docs/moddb").Absolute(url, image));
}
