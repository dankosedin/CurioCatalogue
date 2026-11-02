# Tests against a real game server

Started 02.11.2026. A third test project starts the real game server (headless, no client) inside the `dotnet test` process,
in a throwaway superflat world, with our mod loaded, and gives the tests the server API. What our tests now mock, or what
is checked only in game (assets that fail to load, real slots and containers, books on real inventories), becomes testable.
No change to the mod itself.

Some mods are known to declare such tests; we found the way ourselves (engine 1.22.7 decompiled with `ilspycmd`), without
looking at how they do it. The findings and the skeleton are in "Findings" below; the steps go from the smallest to the
most advanced; the last one reviews what the tests cover.

## How to work on this plan

As in the previous plans: one step per short session with the project skill `/step <id>` (`.claude/skills/step/`). Each step
says **Branch**, **After**, **Model**, **Author** (a decision only the author can take; "none" means the session goes ahead on
its own) and **Done when**. A session that stops in the middle writes one line under "Where we stopped".

Every server test that relies on engine behaviour still says why (`docs/development.md`, "Mocks document our
assumptions"), but its source is now the test itself: the engine ran it.

## Steps

- [x] **T1. The project and the fixture in the repo.** A new project `CurioCatalogue.ServerTests` built from the skeleton
      (Findings, "Skeleton"): the fixture, its guards, the three smoke tests (our mod is enabled; our items are registered; a
      book goes into a real slot). It is in `curiocatalogue.sln`, so `dotnet test curiocatalogue.sln` runs it; it builds the mod
      first (a project reference with `ReferenceOutputAssembly=false`) and takes the mod's paths from MSBuild, not hard-coded.
      Docs: `docs/development.md` (the third project, what it needs, how long it takes, how to run it alone);
      `docs/modding.md` (the engine traps of Findings); `docs/architecture.md` Part 1 (in-process, with the rejected
      alternatives). Branch: `test/server-tests`. After: none. Model: opus (the fixture is delicate). Author: none.
      Done when: all three test projects pass locally; a mutation (our mod path wrong, `DataPath` not set) fails clearly;
      the temp world folder is gone after a passing run.
- [x] **T2. Assets load without errors.** The fixture collects the server log (`ILogger.EntryAdded`, or `server-main.log`
      after start); a test fails on an error or warning about our domain or our files ("Failed to parse grid recipe", "cannot
      be resolved", JSON errors, missing textures or shapes, lang). Vanilla noise that is not ours is filtered by a short,
      commented list. Then decide with the author which checks of `AssetsTests` stay (they run without the game, in the core
      CI job) and which the server makes redundant. After: T1. Model: sonnet. Author: decides what stays in `AssetsTests`.
      Done when: a broken recipe and a broken item JSON (mutations) each fail the test with the log line in the message; the
      "check `server-main.log`" item of `docs/critical.md` says the test does it.
- [x] **T3. Our recipes in the game.** Every grid recipe of our assets is in the server's recipe list with the output and the
      ingredients we wrote (codes resolved, wildcards matched, counts), including the book recipes from an old book.
      After: T2. Model: sonnet. Author: none. Done when: one test per recipe file or a table-driven test; a mutation of an
      ingredient fails it.
- [ ] **T4. How tests call our code.** Our DLL is loaded by the game's mod loader; a test project that references
      `curiocatalogue.dll` too gets a second copy of our types. Find the way (e.g. take our types from the mod's loaded
      assembly through `Api.ModLoader`, or make the mod loader use the assembly already loaded) and prove it with one call
      into an adapter. After: T1. Model: opus. Author: decides between the ways found. Done when: the decision with the
      rejected ways is in Decisions; one test calls our code on the server.
- [ ] **T5. Real containers in the world.** Place a chest (and the other storage our books read) on the superflat, fill it,
      and test what our adapters read from it and write into it: the cases that `TestContainers` and `GameWorld` mock today.
      Each mock that a server test now covers is marked or removed. After: T4. Model: sonnet. Author: none.
      Done when: the container cases run on the server; the mocks' "checked in game" notes they replace are updated.
- [ ] **T6. A player without a client.** `IPlayer` cannot be mocked (`docs/development.md`). Research whether the server can
      give a real `IServerPlayer` without a client (the engine has `DummyNetConnection`, `DummyTcpNetServer`; a player joined
      through them?), with its inventories, rights and position. A research step: no client means no network messages.
      After: T4. Model: opus. Author: decides whether to go on and how. Done when: the answer and the chosen way (or
      "not possible, because") are in Decisions.
- [ ] **T7. Books and invoices end to end.** With T6's player: making an invoice, posting it, the album taking from a chest,
      the want list, each "all or nothing" (`docs/critical.md`) on real inventories, and the access checks (claims,
      reinforcement). After: T5, T6. Model: sonnet. Author: none (or splits the step if large).
      Done when: the cases of `docs/testing/functional.md` that need no client have a server test, marked there.
- [ ] **T8. CI.** The `build` job (it has the game server) runs the server tests; how long they take; what CI keeps when
      they fail (the server log of the temp world as an artifact). After: T1 (may wait for T2–T7). Model: sonnet. Author: none.
      Done when: `ci.yml` changed; `docs/development.md` (CI) says so; checked after the push (the author).
- [ ] **T9. Coverage review.** What the three test projects cover now and what they do not: a coverage report (coverlet is in
      the tests already) per class of the adapters; the mocks left and whether each still needs to be a mock; the cases of
      `functional.md` still only in game; the item classes (`ROADMAP.md`, "Tests for the item classes"). Findings fixed in
      the step if small, otherwise lines in `ROADMAP.md`. After: T7, T8. Model: opus. Author: decides on each finding.
      Done when: the review is in Decisions, its findings fixed, moved to `ROADMAP.md` or rejected with a reason.

## Open questions (for the steps that name them)

- **Parallel runs** (T8): answered for local runs in T1 (Decisions); CI runs the solution the same way, still to see there.
- **Linux** (T8): CI runs on ubuntu with the Linux server archive. The fixture builds paths with `Path` and the config with a
  JSON serializer, but it has run only on Windows.

## Findings (02.11.2026)

How the server starts (decompiled `VintagestoryServer.dll` and `VintagestoryLib.dll`, 1.22.7):

- `VintagestoryServer.exe` → `ServerProgram.Main` (VintagestoryLib): sets `GamePaths.DataPath` / `CustomLogPath` from the
  arguments; `ServerMain.Logger = new ServerLogger(args)`; `Lang.PreLoad`; `new ServerMain(null, args, progArgs)`;
  `exitState = new GameExitState()`, `Standalone = true`; `PreLaunch()`, `Launch()`; loop `Process()` until
  `exitState.Exit`; `Stop(...)`, `Dispose()`, `Environment.Exit`. All of these are public, so a test can do the same.
- The server's arguments useful for tests: `--dataPath`, `--logPath`, `--addModPath`, `--addOrigin` (extra assets),
  `--withconfig "{ ... }"` (any `serverconfig.json` value at launch, e.g. `WorldConfig`, `ModPaths`, `Port`).
- `VintagestoryAPI.dll` has `InternalsVisibleTo("VSTests")`: the developers have a test assembly of their own (not shipped).

Two ways tried:

- **A separate process** (`VintagestoryServer.exe` with a temp `--dataPath`, our build in `--addModPath`, a superflat world in
  `--withconfig`): the world is created and our mod starts; "Dedicated Server now running" after 9–22 s. Stopping it through
  stdin was not solved: `/stop` took effect minutes later in one run (cause unknown). The server console reads stdin in
  chunks and stops reading for good after a chunk without a line end. The line after "now running" is a random lore line.
  The tests would have to talk to the server through the log or through a test mod. **Rejected for now** in favour of:
- **The server inside the test process** (works): ready in 8.6 s, four tests in about 10 s, the world saved and the server
  stopped cleanly; the tests get `ICoreServerAPI` and run code on the server's main thread.

Traps of the in-process way, in the order they showed up (each blocked the next):

1. **`GamePaths.DataPath` defaults to `%AppData%\VintagestoryData`**, the author's main game. Set it first, and fail if it
   points there.
2. **Two Newtonsoft.Json.** The test SDK brings 13.0.3, the game needs 13.0.4; both have assembly version 13.0.0.0, and only
   one is loaded (`MissingMethodException: JToken.WriteTo` in `ServerConfig.Save`). Copy the game's DLL over the test bin
   after the build.
3. **The base directory.** `GamePaths.Binaries`, `AssetsPath`, the game's `AssemblyResolver` and `ModCompilationContext`
   ("Referenced library not found: SkiaSharp.dll") all work from `AppDomain.BaseDirectory`, the test bin. Fix: in a
   `[ModuleInitializer]`, `AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", <game folder with a trailing \>)` before any game
   type is touched. This also makes `RuntimeEnv.IsDevEnvironment` false, as for the real exe.
4. **The current directory.** Mono.Cecil (the mod loader reads mod DLLs with it) resolves `VintagestoryAPI` from the current
   directory; without it "Failed to resolve assembly", then "game - Missing" and a null reference in the playstyle. Set
   `Environment.CurrentDirectory` to the game folder.
5. **Our assets.** The Debug build has no assets; add our `assets` folder with `AddOrigin`, as the launch profiles do.

### Skeleton

The fixture (an xUnit collection fixture, one server for all tests of the project):

- **Module initializer** (runs before any type of the test assembly is used): an `AssemblyResolve` hook that loads game
  assemblies from the game folder, `Lib` and `Mods` (game DLLs are referenced with `Private=false`, so there is one copy, the
  game's); the base directory and the current directory set to the game folder (traps 3, 4); the game's `Lib` on `PATH`.
- **Constructor:** a temp data folder (`%TEMP%\cc-…`), the guard against the main game's data; a dedicated thread runs the
  server; the constructor waits (up to 3 min) until the run phase is `RunGame`, or rethrows the start failure with the path to
  the server log (xUnit does not dispose a fixture whose constructor threw, so that folder stays for diagnosis).
- **The server thread:** `GamePaths.DataPath`; checks that the base directory and `AssetsPath` point at the game;
  `ServerProgramArgs` with `DataPath`, `AddOrigin` (our assets) and `WithConfig` (port, `ModPaths` = the game's `Mods` and our
  build output, `WorldConfig` superflat creative); `ServerMain.xPlatInterface`, `ServerMain.Logger`, `Lang.PreLoad`;
  `new ServerMain`, `PreLaunch`, `Launch`; loop `Process()` until a stop flag; then `Stop` and `Dispose` on the same thread.
- **`OnMain(work)`:** `EnqueueMainThreadTask` with a `TaskCompletionSource`, waits up to 30 s; game code runs on the
  server's main thread, as in the game.
- **Dispose:** sets the stop flag, joins the thread, deletes the temp folder.
- **Project file:** xUnit as in the other test projects; references `VintagestoryAPI`, `VintagestoryLib`,
  `xplatforminterface` (`Lib`) with `Private=false`; an after-build target that copies the game's `Newtonsoft.Json.dll` into
  the output (trap 2). It does not reference our mod's DLL (T4).

## Decisions

Before the plan (02.11.2026):

- **No client.** Tests run the server only; what needs a client (network messages, GUI, rendering) stays in game checks.
- **From the smallest to the most advanced,** the last step reviewing the coverage.
- **In-process server** for the start (Findings): no stdin or log protocol between processes, the tests use the server API
  directly, one start per run.

T1 (02.11.2026):

- **A free port** chosen at start (a `TcpListener` on port 0), passed as `ServerProgramArgs.Port`, rather than a fixed one: a game
  or a server running on this machine does not get in the way. Rejected: a fixed port (clashes; 42420 is the game's default).
- **Parallel runs:** one server per process; the fixture throws on a second instance. `dotnet test` runs each test project in
  its own process, so a second project with a server would get its own server (and its own free port).
- **No `PATH` change:** the server starts and runs without the game's `Lib` on `PATH`.
- **The guard checks `GamePaths.DataPath` itself**, after setting it and before the logger writes: decompiled `ServerMain` reads
  only that static, not `ServerProgramArgs.DataPath`.
- **The fixture checks our mod is enabled** after start, so a wrong mod path fails every test with the list of loaded mods,
  not only the smoke test. Our assets still load from the origin without the mod (modding.md, A server inside the test process).
- **Paths from MSBuild as assembly metadata:** the target `TestPathsAsMetadata` asks the mod project for its `TargetPath`
  (`GetTargetPath`) and writes the game folder, the mod's DLL and its `assets` into `AssemblyMetadata` attributes.
- **On a failed start the temp folder stays** for diagnosis (the error names its log); after a passing run it is deleted, and a
  failure to delete fails the run.

T2 (02.11.2026):

- **`AssetsTests` stays whole** (author). The server catches a broken JSON, a comment in a recipe array and an unregistered item
  class too, but the tests without the game are fast, run in the core CI job and name the file and the path; the overlap is three
  tests. Rejected: removing `Json_file_parses` and `Recipe_arrays_have_no_comments`.
- **No `KnownNoise` yet:** a clean run on 1.22.7 has no warning or error at all (1409 log lines), so the filter list is empty and
  every problem line fails the test, not only lines that name our domain (a "Failed to parse" line names a file, not always a domain).
- **Mutations:** a comment in `recipes/grid/ledger.json` fails with "Failed to parse grid recipe: curiocatalogue:recipes/grid/ledger.json";
  a wrong item class in `invoice.json` fails with "no such class registered" (and the smoke test). Textures and shapes are read by the
  client, so the server does not log a missing one: still an in-game check.

T3 (02.11.2026):

- **Expected sets are written in the test, not read from the JSON** (a copy of the table, so a change of a recipe is made twice and cannot slip by);
  compared as sets with the diff in the message. The server's count matches the JSON by hand: 1067 = 72 + 2 + 48 + 72 + 48 + 792 + 33.
- **Mutations:** another leather in one recolour recipe, parchment quantity 2, and a typo in an ingredient code each fail the matching test
  (a typo drops the recipe: "missing (72)").

## Where we stopped

- 02.11.2026: T1 done: the project, the fixture and three smoke tests (six cases) pass in about 13 s; mutations checked (a
  wrong mod path: "our mod is not enabled; mods: …"; `GamePaths.DataPath` not set: "the data path is …\VintagestoryData",
  nothing written there). CI still runs `dotnet test curiocatalogue.sln`, which now includes the server tests (T8).
- 02.11.2026: T2 done: `GameServerFixture.LogEntries()` and `AssetLoadTests` (log captured from the start; fails on any warning or error); two mutations checked; `critical.md`, `modding.md`, `development.md` updated. Next: T3 or T4 (both After: T1).

- 02.11.2026: T3 done: `RecipeTests` (8 tests, 1067 recipes of our domain checked against expected sets); three mutations checked; `development.md` updated. Not committed yet (waits for "комічь"). Next: T4 (opus) or T8.
