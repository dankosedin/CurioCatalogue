using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;
using VSPlatform;

namespace CurioCatalogue.TestServer;

/// <summary>
/// One real game server for all tests of a project, inside the test process, in a throwaway data folder: a superflat
/// creative world with the game's mods, ours and, for the compatibility tests, the other mods given to the constructor.
/// Started the way <c>ServerProgram.Main</c> does it (VintagestoryLib 1.22.7, decompiled; docs/modding.md, "A server inside
/// the test process"). Each test project declares its own <c>[CollectionDefinition]</c> with it.
/// </summary>
public class GameServerFixture : IDisposable
{
    const string ModId = "curiocatalogue";
    // A start takes 10–20 s (docs/development.md); a cold disk or a slow CI runner may take several times that.
    static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(3);
    // A task waits for the next tick (50 ms); a tick that sends a new player's spawn chunks may take seconds, not 30.
    static readonly TimeSpan MainThreadTimeout = TimeSpan.FromSeconds(30);
    // The stop saves the world: a few seconds for the small superflat test world.
    static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(60);
    // A kept data folder (a failed start or a crash) older than this is swept by the next run.
    static readonly TimeSpan KeepFailedFor = TimeSpan.FromDays(7);

    // The engine keeps its state in statics (ServerMain.Logger, GamePaths, Lang): one server per process, ever.
    static int instances;

    readonly Thread thread;
    readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ServerMain? server;
    volatile bool stopRequested;
    volatile Exception? crash;
    readonly List<LogLine> logLines = [];
    int startupLines; // how many of logLines the server wrote until RunGame

    /// <summary>The throwaway data folder (world, config, logs); deleted after the run unless the server failed to start.</summary>
    public string DataPath { get; } = Path.Combine(Path.GetTempPath(), DataFolderPrefix + Guid.NewGuid().ToString("N")[..8]);

    public string ServerLog => Path.Combine(DataPath, "Logs", "server-main.log");
    public ICoreServerAPI Api { get; private set; } = null!;
    public TimeSpan StartTime { get; private set; }
    public int Port { get; } = FreePort();

    /// <summary>The mods besides the game's and ours, as given to the constructor.</summary>
    public IReadOnlyList<OtherMod> OtherMods { get; }

    /// <summary>The folder the other mods' files are copied into, a ModPaths entry of its own.</summary>
    string OtherModsPath => Path.Combine(DataPath, "Mods");

    /// <summary>The game's mods and ours.</summary>
    public GameServerFixture() : this([]) { }

    /// <summary>
    /// The game's mods, ours and <paramref name="otherMods"/>: their files are copied into the data folder's <c>Mods</c>, so
    /// only the files given load (not a whole folder); each must be enabled after start, with the version given.
    /// </summary>
    protected GameServerFixture(IReadOnlyList<OtherMod> otherMods)
    {
        if (Interlocked.Increment(ref instances) != 1)
            throw new InvalidOperationException("a second game server in one process: the engine's state is static, so a process "
                + "runs one server once. Share the fixture through one [Collection]; to run the tests again, restart the test host.");
        OtherMods = otherMods;
        if (!File.Exists(TestPaths.ModDll))
            throw new InvalidOperationException($"our mod is not built at {TestPaths.ModDll}");
        if (!Directory.Exists(TestPaths.ModAssets))
            throw new InvalidOperationException($"our assets are not at {TestPaths.ModAssets}");
        SweepLeftCaches();
        Directory.CreateDirectory(DataPath);
        Directory.CreateDirectory(OtherModsPath);
        foreach (var mod in otherMods) CopyMod(mod.File, Path.Combine(OtherModsPath, Path.GetFileName(mod.File)));

        thread = new Thread(Run) { Name = "TestServerMain", IsBackground = true };
        thread.Start();
        try
        {
            Await(started.Task, StartTimeout, "the server did not reach RunGame"); // rethrows a start failure
            string? notLoaded = OnMain(NotLoaded);
            if (notLoaded != null) throw new InvalidOperationException(notLoaded);
        }
        catch (Exception e)
        {
            // xUnit does not dispose a fixture whose constructor threw: stop the server, keep the folder for diagnosis.
            stopRequested = true;
            thread.Join(StopTimeout);
            throw new InvalidOperationException($"the test server failed to start, see {ServerLog}: {e.Message}", e);
        }
    }

    /// <summary>A mod's zip, or a mod folder with all it holds.</summary>
    static void CopyMod(string from, string to)
    {
        if (!Directory.Exists(from)) { File.Copy(from, to); return; }
        foreach (string file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    /// <summary>Null when our mod and every other mod are enabled (the others with their version), else what is wrong and the mods loaded.</summary>
    string? NotLoaded()
    {
        var problems = new List<string>();
        if (!Api.ModLoader.IsModEnabled(ModId)) problems.Add("our mod is not enabled");
        foreach (var other in OtherMods)
        {
            var mod = Api.ModLoader.GetMod(other.Id);
            if (mod == null || !Api.ModLoader.IsModEnabled(other.Id)) problems.Add($"{other.Id} is not enabled");
            else if (mod.Info.Version != other.Version) problems.Add($"{other.Id} is {mod.Info.Version}, expected {other.Version}");
        }
        return problems.Count == 0 ? null : string.Join("; ", problems) + "; mods: "
            + string.Join(", ", Api.ModLoader.Mods.Select(m => $"{m.Info?.ModID} {m.Info?.Version} ({m.SourcePath})"));
    }

    void Run()
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            RequirePath("the base directory", Path.TrimEndingDirectorySeparator(AppDomain.CurrentDomain.BaseDirectory), TestPaths.GameDir);
            RequirePath("the game's assets", GamePaths.AssetsPath, Path.Combine(TestPaths.GameDir, "assets"));
            // GamePaths.DataPath defaults to %AppData%\VintagestoryData, the author's main game. ServerMain reads only this
            // static, not ServerProgramArgs.DataPath. Checked before anything is written (the logger creates its files).
            GamePaths.DataPath = DataPath;
            RequirePath("the data path", GamePaths.DataPath, DataPath);
            GamePaths.EnsurePathsExist();

            var args = new ServerProgramArgs
            {
                DataPath = DataPath,
                Port = Port.ToString(),
                AddOrigin = [TestPaths.ModAssets],
                WithConfig = JsonSerializer.Serialize(new
                {
                    ModPaths = new[] { Path.Combine(TestPaths.GameDir, "Mods"), TestPaths.ModsPath, OtherModsPath },
                    VerifyPlayerAuth = false,
                    // Every test joins its own player and none leaves: the default 16 clients would run out, and a fake client
                    // that never answers pings would be dropped after 150 s (ServerConfig.MaxClients, ClientConnectionTimeout).
                    MaxClients = 1000,
                    ClientConnectionTimeout = 3600,
                    WorldConfig = new { WorldType = "superflat", PlayStyle = "creativebuilding", AllowCreativeMode = true, WorldName = "servertests" },
                }),
            };

            ServerMain.xPlatInterface = XPlatformInterfaces.GetInterface();
            ServerMain.Logger = new ServerLogger(args);
            ServerMain.Logger.EntryAdded += OnLogEntry; // before the mod loader and the asset loader write anything
            Lang.PreLoad(ServerMain.Logger, GamePaths.AssetsPath, "en");

            server = new ServerMain(null, [], args, isDedicatedServer: true);
            server.exitState = new GameExitState();
            server.Standalone = true;
            server.PreLaunch();
            server.Launch();
            Api = (ICoreServerAPI)server.Api;

            while (!stopRequested && !server.exitState.Exit)
            {
                server.Process();
                if (!started.Task.IsCompleted && Api.Server.CurrentRunPhase == EnumServerRunPhase.RunGame)
                {
                    StartTime = clock.Elapsed;
                    lock (logLines) startupLines = logLines.Count;
                    started.SetResult();
                }
            }
            if (!stopRequested) throw new InvalidOperationException("the server stopped by itself");
        }
        catch (Exception e)
        {
            crash = e;
            started.TrySetException(e);
        }
        finally
        {
            // On the server's own thread, as ServerProgram.Main does.
            if (server != null)
            {
                try
                {
                    server.Stop("tests done", EnumExitMode.SoftExit);
                    server.Dispose();
                }
                catch (Exception e) { crash ??= e; }
            }
        }
    }

    void OnLogEntry(EnumLogType type, string message, object[] args)
    {
        // Called on any thread; the logger formats the same way (string.Format) and an unformattable text must not lose the line.
        string text;
        try { text = args is { Length: > 0 } ? string.Format(message, args) : message; }
        catch (FormatException) { text = message; }
        lock (logLines) logLines.Add(new LogLine(type, text));
    }

    /// <summary>Everything the server logged since it started (a snapshot), warnings and errors included.</summary>
    internal IReadOnlyList<LogLine> LogEntries() { lock (logLines) return [.. logLines]; }

    /// <summary>
    /// What the server logged until it reached RunGame: the loading of the assets and the mods' start. The same for every test,
    /// whatever ran before it.
    /// </summary>
    internal IReadOnlyList<LogLine> StartupLog() { lock (logLines) return [.. logLines.Take(startupLines)]; }

    /// <summary>The number of log lines so far: the mark for <see cref="LogSince"/>.</summary>
    internal int LogMark() { lock (logLines) return logLines.Count; }

    /// <summary>
    /// The lines logged after <paramref name="mark"/>, by any thread: a test reads what its own action logged, plus whatever the
    /// server logged meanwhile (<see cref="LogProblems.Since"/> leaves out the known noise).
    /// </summary>
    internal IReadOnlyList<LogLine> LogSince(int mark) { lock (logLines) return [.. logLines.Skip(mark)]; }

    static void RequirePath(string what, string actual, string expected)
    {
        if (!string.Equals(Path.GetFullPath(actual), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{what} is {actual}, expected {expected}");
    }

    static int FreePort()
    {
        // A dedicated server binds a port; a free one keeps the tests clear of a game or server running on this machine.
        var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Runs <paramref name="work"/> on the server's main thread, where game code runs in the game. Called on the main thread
    /// itself (from inside another <c>OnMain</c>), it runs at once: waiting for the queue there would wait for itself.
    /// </summary>
    public T OnMain<T>(Func<T> work)
    {
        if (crash != null) throw new InvalidOperationException("the test server crashed", crash);
        if (Thread.CurrentThread == thread) return work();
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        int abandoned = 0;
        server!.EnqueueMainThreadTask(() =>
        {
            // A task whose test has given up on it must not run later, in the middle of another test.
            if (Volatile.Read(ref abandoned) == 1) return;
            try { result.SetResult(work()); }
            catch (Exception e) { result.SetException(e); }
        });
        try { Await(result.Task, MainThreadTimeout, "the main thread did not run the task"); }
        catch (TimeoutException) { Volatile.Write(ref abandoned, 1); throw; }
        return result.Task.Result;
    }

    /// <summary>Waits for <paramref name="task"/> and rethrows its own exception, not an AggregateException around it.</summary>
    static void Await(Task task, TimeSpan timeout, string onTimeout)
    {
        try { task.WaitAsync(timeout).GetAwaiter().GetResult(); }
        catch (TimeoutException) when (!task.IsCompleted) { throw new TimeoutException($"{onTimeout} in {timeout}"); }
    }

    public void Dispose()
    {
        stopRequested = true;
        if (!thread.Join(StopTimeout))
            throw new TimeoutException($"the test server did not stop in {StopTimeout}; its data: {DataPath}", crash); // a crash may be why
        if (crash != null) throw new InvalidOperationException($"the test server crashed, see {ServerLog}", crash);
        KeepLogs();
        DeleteData();
    }

    const string DataFolderPrefix = "cc-servertests-";

    /// <summary>
    /// CI sets <c>CURIOCATALOGUE_KEEP_LOGS</c> to a folder: the server's <c>Logs</c> are copied there (into a subfolder per run) before the
    /// data folder is deleted, so a failed test leaves its log to upload. A start that failed or a crash keeps the whole data folder anyway.
    /// </summary>
    void KeepLogs()
    {
        string? target = Environment.GetEnvironmentVariable("CURIOCATALOGUE_KEEP_LOGS");
        string logs = Path.Combine(DataPath, "Logs");
        if (string.IsNullOrEmpty(target) || !Directory.Exists(logs)) return;
        string to = Path.Combine(target, Path.GetFileName(DataPath));
        Directory.CreateDirectory(to);
        foreach (string file in Directory.EnumerateFiles(logs)) File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
    }

    /// <summary>
    /// Deletes the data folder. Another code mod is unpacked into <c>Cache\unpack</c> and its DLL is loaded from there; Windows keeps
    /// a loaded DLL locked until the process ends, so with other mods <c>Cache</c> may stay: the next run sweeps it
    /// (<see cref="SweepLeftCaches"/>). Everything else must go, or the run fails.
    /// </summary>
    void DeleteData()
    {
        string cache = Path.Combine(DataPath, "Cache");
        foreach (string dir in Directory.EnumerateDirectories(DataPath).Where(d => d != cache)) Directory.Delete(dir, recursive: true);
        foreach (string file in Directory.EnumerateFiles(DataPath)) File.Delete(file);
        try { Directory.Delete(DataPath, recursive: true); }
        catch (UnauthorizedAccessException) when (OtherMods.Count > 0) { }
    }

    /// <summary>
    /// Deletes the data folders of earlier passing runs that kept only <c>Cache</c> (<see cref="DeleteData"/>), and the folders kept
    /// for diagnosis (a failed start, a crash) once they are older than <see cref="KeepFailedFor"/>. A newer folder with more in it
    /// is a failure to look at, or a run going on in another process: left alone.
    /// </summary>
    static void SweepLeftCaches()
    {
        foreach (string dir in Directory.EnumerateDirectories(Path.GetTempPath(), DataFolderPrefix + "*"))
        {
            try
            {
                if (Directory.EnumerateFileSystemEntries(dir).Select(Path.GetFileName).SequenceEqual(["Cache"])
                    || DateTime.UtcNow - Directory.GetLastWriteTimeUtc(dir) > KeepFailedFor)
                    Directory.Delete(dir, recursive: true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}

/// <summary>A mod loaded besides the game's and ours: its id and version (checked after start) and its file (a zip) or folder.</summary>
public sealed record OtherMod(string Id, string Version, string File);

/// <summary>One line of the server log: its level and its text as written.</summary>
// internal: xUnit lists the public types before the module initializer (GameAssemblies) has run, and a public type with a game
// enum as a field would load VintagestoryAPI too early ("Could not load file or assembly").
internal sealed record LogLine(EnumLogType Type, string Text)
{
    public override string ToString() => $"[{Type}] {Text}";
}
