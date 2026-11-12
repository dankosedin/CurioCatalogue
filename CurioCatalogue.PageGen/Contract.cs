namespace CurioCatalogue.PageGen;

/// <summary>
/// What the descriptions must keep so the page can be generated from them (docs/releasing.md, "The description contract").
/// The keys are slugs of the English titles; the template (docs/moddb/template.html) shows each of them.
/// </summary>
public static class Contract
{
    /// <summary>The section that answers "what can it do": a list of <see cref="InsideItems"/> items, then a paragraph.</summary>
    public const string InsideKey = "whats-inside";

    public const int InsideItems = 6;

    /// <summary>ModDB refuses a description of about 65 000 bytes ("Excessive size"); it counts bytes, not characters (Cyrillic is two a letter). The reserve is for what the editor adds.</summary>
    public const int MaxPageBytes = 64_000;

    /// <summary>The guide's files in the repository (docs/guide/), by language; the page links to them and checks the headings its links point at.</summary>
    public const string GuideEnPath = "docs/guide/README.md";
    public const string GuideUkPath = "docs/guide/README.uk.md";
    public const string ReadmePath = "README.md";

    /// <summary>The page's sections in its order: the showcase (docs/plans/2026-11-04-mod-description.md, "The showcase and the guide: the split").</summary>
    public static readonly string[] SectionKeys =
    {
        "whats-new", InsideKey, "collections", "what-it-does-not-do", "similar-mods", "credits", "license",
    };
}

/// <summary>A description or the template breaks the contract; <see cref="Problems"/> lists every break, not only the first.</summary>
public sealed class ContractException : Exception
{
    public IReadOnlyList<string> Problems { get; }

    public ContractException(IReadOnlyList<string> problems)
        : base("The ModDB page cannot be generated:" + Environment.NewLine + "  - " + string.Join(Environment.NewLine + "  - ", problems))
    {
        Problems = problems;
    }
}
