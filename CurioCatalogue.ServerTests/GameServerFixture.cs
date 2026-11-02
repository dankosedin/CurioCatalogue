using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;
using VSPlatform;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// One real game server for all tests of this project, inside the test process, in a throwaway data folder: a superflat
/// creative world with the game's mods and ours. Started the way <c>ServerProgram.Main</c> does it (VintagestoryLib 1.22.7,
/// decompiled; docs/modding.md, "A server inside the test process").
/// </summary>
public sealed class GameServerFixture : IDisposable
{
    const string ModId = "curiocatalogue";
    static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(3);
    static readonly TimeSpan MainThreadTimeout = TimeSpan.FromSeconds(30);
    static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(60);

    // The engine keeps its state in statics (ServerMain.Logger, GamePaths, Lang): one server per process.
    static int instances;

    readonly Thread thread;
    readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ServerMain? server;
    volatile bool stopRequested;
    volatile Exception? crash;

    /// <summary>The throwaway data folder (world, config, logs); deleted after the run unless the server failed to start.</summary>
    public string DataPath { get; } = Path.Combine(Path.GetTempPath(), "cc-servertests-" + Guid.NewGuid().ToString("N")[..8]);

    public string ServerLog => Path.Combine(DataPath, "Logs", "server-main.log");
    public ICoreServerAPI Api { get; private set; } = null!;
    public TimeSpan StartTime { get; private set; }
    public int Port { get; } = FreePort();

    public GameServerFixture()
    {
        if (Interlocked.Increment(ref instances) != 1)
            throw new InvalidOperationException("a second game server in one process: the engine's state is static; share the fixture through [Collection(ServerCollection.Name)]");
        if (!File.Exists(TestPaths.ModDll))
            throw new InvalidOperationException($"our mod is not built at {TestPaths.ModDll}");
        if (!Directory.Exists(TestPaths.ModAssets))
            throw new InvalidOperationException($"our assets are not at {TestPaths.ModAssets}");
        Directory.CreateDirectory(DataPath);

        thread = new Thread(Run) { Name = "TestServerMain", IsBackground = true };
        thread.Start();
        try
        {
            Await(started.Task, StartTimeout, "the server did not reach RunGame"); // rethrows a start failure
            string? notOurs = OnMain(() => Api.ModLoader.IsModEnabled(ModId) ? null :
                "our mod is not enabled; mods: " + string.Join(", ", Api.ModLoader.Mods.Select(m => $"{m.Info?.ModID} ({m.SourcePath})")));
            if (notOurs != null) throw new InvalidOperationException(notOurs);
        }
        catch (Exception e)
        {
            // xUnit does not dispose a fixture whose constructor threw: stop the server, keep the folder for diagnosis.
            stopRequested = true;
            thread.Join(StopTimeout);
            throw new InvalidOperationException($"the test server failed to start, see {ServerLog}: {e.Message}", e);
        }
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
                    ModPaths = new[] { Path.Combine(TestPaths.GameDir, "Mods"), TestPaths.ModsPath },
                    WorldConfig = new { WorldType = "superflat", PlayStyle = "creativebuilding", AllowCreativeMode = true, WorldName = "servertests" },
                }),
            };

            ServerMain.xPlatInterface = XPlatformInterfaces.GetInterface();
            ServerMain.Logger = new ServerLogger(args);
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

    /// <summary>Runs <paramref name="work"/> on the server's main thread, where game code runs in the game.</summary>
    public T OnMain<T>(Func<T> work)
    {
        if (crash != null) throw new InvalidOperationException("the test server crashed", crash);
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        server!.EnqueueMainThreadTask(() =>
        {
            try { result.SetResult(work()); }
            catch (Exception e) { result.SetException(e); }
        });
        Await(result.Task, MainThreadTimeout, "the main thread did not run the task");
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
        if (!thread.Join(StopTimeout)) throw new TimeoutException($"the test server did not stop in {StopTimeout}; its data: {DataPath}");
        if (crash != null) throw new InvalidOperationException($"the test server crashed, see {ServerLog}", crash);
        Directory.Delete(DataPath, recursive: true);
    }
}

[CollectionDefinition(Name)]
public class ServerCollection : ICollectionFixture<GameServerFixture>
{
    public const string Name = "game server";
}
