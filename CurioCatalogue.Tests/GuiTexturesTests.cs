using System.Text.RegularExpressions;

namespace CurioCatalogue.Tests;

// The GUI cannot run here (no client), so this guards the rule of L10 in the source: the game disposes a GUI element more than
// once, at different times (the composer holds an interactive element in two lists, vsapi GuiComposer.cs:795-796, :868-878;
// GuiComposerManager.Create disposes the last composer of a name again, decompiled), so a texture freed by a bare id is freed
// again after GL has given that id to another element's texture (a tooltip's): it then draws garbage or the client crashes on
// GL_INVALID_VALUE. A LoadedTexture forgets its id when freed (vsapi LoadedTexture.cs:77), so our textures are LoadedTextures
// and nothing of ours deletes a texture by its id. docs/modding.md, GUI.
public class GuiTexturesTests
{
    private static readonly Regex DeleteById = new(@"\b(GLDeleteTexture|DeleteTexture)\s*\(");

    [Fact]
    public void NoTextureIsFreedByItsId()
    {
        string mod = Path.Combine(AssetsTests.RepoRoot(), "curiocatalogue");
        var found = Directory.GetFiles(mod, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (File: Path.GetRelativePath(mod, f), Line: i + 1, Text: line)))
            .Where(l => !l.Text.TrimStart().StartsWith("//") && !l.Text.TrimStart().StartsWith("///") && DeleteById.IsMatch(l.Text))
            .Select(l => $"{l.File}:{l.Line}: {l.Text.Trim()}")
            .ToList();
        Assert.True(found.Count == 0, "Textures freed by id (keep them as LoadedTexture):\n" + string.Join("\n", found));
    }
}
