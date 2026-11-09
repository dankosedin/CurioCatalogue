namespace CurioCatalogue.PageGen;

/// <summary>
/// What the READMEs must keep so the page can be generated from them (docs/development.md, "The README contract").
/// The keys are slugs of the English titles; the template (docs/moddb/template.html) shows each of them.
/// </summary>
public static class Contract
{
    /// <summary>The section that answers "what can it do": a list of <see cref="InsideItems"/> items, a paragraph, then ### subsections.</summary>
    public const string InsideKey = "whats-inside";

    public const int InsideItems = 6;
    public const int InsideSubsections = 3;

    /// <summary>Sections that are an accordion each, in the page's order.</summary>
    public static readonly string[] AccordionKeys =
    {
        "getting-started", "collections", "gestures", "fair-play-on-servers", "configuration",
        "compatibility-and-known-limitations", "removing-the-mod",
    };

    /// <summary>Sections merged into one accordion, "Other".</summary>
    public static readonly string[] OtherKeys = { "languages", "contributing", "ai-and-human-work", "credits", "license" };

    public static IEnumerable<string> AllKeys =>
        new[] { InsideKey }.Concat(AccordionKeys).Concat(OtherKeys);
}

/// <summary>The README or the template breaks the contract; <see cref="Problems"/> lists every break, not only the first.</summary>
public sealed class ContractException : Exception
{
    public IReadOnlyList<string> Problems { get; }

    public ContractException(IReadOnlyList<string> problems)
        : base("The ModDB page cannot be generated:" + Environment.NewLine + "  - " + string.Join(Environment.NewLine + "  - ", problems))
    {
        Problems = problems;
    }
}
