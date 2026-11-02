using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace CurioCatalogue.TestServer;

/// <summary>
/// The warnings and errors of a server log that are ours to look at. The game logs a broken asset and goes on without it
/// (docs/critical.md, Recipes), so a clean start is what the asset tests of each test project check.
/// </summary>
internal static class LogProblems
{
    // Vanilla noise that is not ours and that we cannot fix: a regex per line, each with the reason and the game version
    // it was seen in. A start on 1.22.7 with the game's own mods logs none (checked 02.11.2026): every other warning or error
    // is ours to look at.
    static readonly Regex[] KnownNoise =
    [
        // 1.22.7: a player joining (FakeClient) makes the server send its spawn chunks in one tick; the next tick logs this.
        new(@"^Server overloaded\. A tick took \d+ms to complete\.$"),
    ];

    static bool IsProblem(LogLine line) => line.Type is EnumLogType.Warning or EnumLogType.Error or EnumLogType.Fatal;

    static List<LogLine> Of(IEnumerable<LogLine> lines) => lines.Where(IsProblem).Where(l => !KnownNoise.Any(n => n.IsMatch(l.Text))).ToList();

    /// <summary>Every Warning, Error or Fatal line the server logged until RunGame, except the known noise: whatever ran before.</summary>
    public static List<LogLine> AtStart(GameServerFixture game) => Of(game.StartupLog());

    /// <summary>Every Warning, Error or Fatal line logged after <paramref name="mark"/> (<see cref="GameServerFixture.LogMark"/>), except the known noise.</summary>
    public static List<LogLine> Since(GameServerFixture game, int mark) => Of(game.LogSince(mark));
}
