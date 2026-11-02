using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurioCatalogue.CompatTests;

/// <summary>
/// Tiny mods of our own that stand in for a kind of mod no real one fits (plan 2026-11-02-server-tests, T17): each a folder in
/// <c>testmods/</c> next to the project, in the repo, with its <c>modinfo.json</c>. Loaded with the pinned mods, the same way
/// (the fixture copies the folder and checks the mod is enabled with its version).
/// </summary>
static class TestMods
{
    public static string Folder => TestPaths.Get("TestModsFolder");

    sealed record Info(
        [property: JsonPropertyName("modid")] string ModId,
        [property: JsonPropertyName("version")] string Version);

    public static IReadOnlyList<OtherMod> All() =>
        [.. Directory.EnumerateDirectories(Folder).Order().Select(dir =>
        {
            string modinfo = Path.Combine(dir, "modinfo.json");
            var info = JsonSerializer.Deserialize<Info>(File.ReadAllText(modinfo))
                ?? throw new InvalidOperationException($"{modinfo} is empty");
            return new OtherMod(info.ModId, info.Version, dir);
        })];
}
