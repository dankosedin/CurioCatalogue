using System.Reflection;
using System.Runtime.CompilerServices;

namespace CurioCatalogue.TestServer;

/// <summary>The paths MSBuild wrote into this assembly (the project file, target <c>TestPathsAsMetadata</c>).</summary>
static class TestPaths
{
    /// <summary>The game the tests are built against (<c>$(VINTAGE_STORY)</c> at build time).</summary>
    public static readonly string GameDir = Get("GameDir");

    /// <summary>The built mod's DLL, e.g. <c>curiocatalogue\bin\Debug\Mods\mod\curiocatalogue.dll</c>.</summary>
    public static readonly string ModDll = Get("ModDll");

    /// <summary>The folder of mod folders that holds our build: a ModPaths entry, as <c>--addModPath</c> in the launch profiles.</summary>
    public static readonly string ModsPath = Path.GetDirectoryName(Path.GetDirectoryName(ModDll))!;

    /// <summary>The mod's assets: a Debug build has none, so they are an extra origin, as <c>--addOrigin</c> in the launch profiles.</summary>
    public static readonly string ModAssets = Get("ModAssets");

    /// <summary>A value the project file wrote into this assembly; a test project may write keys of its own.</summary>
    public static string Get(string key) =>
        typeof(TestPaths).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value
        ?? throw new InvalidOperationException($"assembly metadata {key} is missing: the project file sets it at build time");
}

/// <summary>
/// Makes this process look like the game's server executable before any game type is touched. Each line answers one trap
/// of the in-process server (docs/test-server.md).
/// </summary>
static class GameAssemblies
{
    [ModuleInitializer]
    internal static void Init()
    {
        string gameDir = TestPaths.GameDir;
        // Game DLLs are referenced with Private=false, so they are not in the test bin: load them from where the game has them.
        // Ours too, from the mod's output: once the mod loader has loaded them, the tests bind to that copy without asking
        // here; this entry serves a test that touches our types before the server starts, with the same file the loader takes.
        string[] dirs = { gameDir, Path.Combine(gameDir, "Lib"), Path.Combine(gameDir, "Mods"), Path.GetDirectoryName(TestPaths.ModDll)! };
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            string file = new AssemblyName(args.Name).Name + ".dll";
            foreach (string dir in dirs)
            {
                string path = Path.Combine(dir, file);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        // GamePaths.Binaries and AssetsPath, the game's own assembly resolver and ModCompilationContext all start from
        // AppDomain.BaseDirectory, which reads this key: point it at the game folder (with the trailing separator it has).
        AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", Path.TrimEndingDirectorySeparator(gameDir) + Path.DirectorySeparatorChar);
        // Mono.Cecil, which the mod loader reads mod DLLs with, resolves VintagestoryAPI from the current directory.
        Environment.CurrentDirectory = gameDir;
    }
}
