using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurioCatalogue.CompatTests;

/// <summary>
/// The other mods of the compatibility tests. Their files are not in the repo (their licences); <c>mods.json</c> pins each one
/// (mod id, version, file, URL, SHA-256). A file is taken from the folder <c>mods/</c> next to the project (git-ignored, cached by
/// CI), or downloaded into it from the URL; either way its hash must match. A mod that cannot be had fails the run: a skip would
/// hide a missing check (plan 2026-11-02-server-tests, Decisions "Before T10").
/// </summary>
static class PinnedMods
{
    static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(2);

    public static string ListPath => TestPaths.Get("PinnedModsList");
    public static string Folder => TestPaths.Get("PinnedModsFolder");

    sealed record Entry(
        [property: JsonPropertyName("modid")] string ModId,
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("file")] string File,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("sha256")] string Sha256);

    /// <summary>Every mod of the list, its file in the folder with the pinned hash; throws on the first one that cannot be had.</summary>
    public static IReadOnlyList<OtherMod> Fetch()
    {
        var entries = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(ListPath))
            ?? throw new InvalidOperationException($"{ListPath} is empty");
        Directory.CreateDirectory(Folder);
        return [.. entries.Select(Fetch)];
    }

    static OtherMod Fetch(Entry entry)
    {
        if (Path.GetFileName(entry.File) != entry.File) throw new InvalidOperationException($"{ListPath}: the file of {entry.ModId} must be a bare file name, not {entry.File}");
        string path = Path.Combine(Folder, entry.File);
        if (File.Exists(path))
        {
            string kept = Sha256Of(path);
            if (!kept.Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{path} has SHA-256 {kept}, {ListPath} pins {entry.Sha256} for {entry.ModId} {entry.Version}: delete the file to download it again, or fix the list");
            return new OtherMod(entry.ModId, entry.Version, path);
        }

        string partial = path + ".part";
        try
        {
            using (var http = new HttpClient { Timeout = DownloadTimeout })
            using (var response = http.GetAsync(entry.Url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult().EnsureSuccessStatusCode())
            using (var file = File.Create(partial))
                response.Content.CopyToAsync(file).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            File.Delete(partial);
            throw new InvalidOperationException($"{entry.ModId} {entry.Version} is not in {Folder} and the download from {entry.Url} failed: {e.Message}", e);
        }

        string downloaded = Sha256Of(partial);
        if (!downloaded.Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(partial);
            throw new InvalidOperationException($"the download of {entry.ModId} {entry.Version} from {entry.Url} has SHA-256 {downloaded}, {ListPath} pins {entry.Sha256}");
        }
        File.Move(partial, path);
        return new OtherMod(entry.ModId, entry.Version, path);
    }

    static string Sha256Of(string path)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(file));
    }
}
