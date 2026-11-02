# Tests against a real game server

Started 02.11.2026. A third test project starts the real game server (headless, no client) inside the `dotnet test` process,
in a throwaway superflat world, with our mod loaded, and gives the tests the server API. What our tests now mock, or what
is checked only in game (assets that fail to load, real slots and containers, books on real inventories), becomes testable.
No change to the mod itself (except a bug a test finds, T11). The same server, with other mods loaded, tests compatibility (T12).

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
- [x] **T4. How tests call our code.** Our DLL is loaded by the game's mod loader; a test project that references
      `curiocatalogue.dll` too gets a second copy of our types. Find the way (e.g. take our types from the mod's loaded
      assembly through `Api.ModLoader`, or make the mod loader use the assembly already loaded) and prove it with one call
      into an adapter. After: T1. Model: opus. Author: decides between the ways found. Done when: the decision with the
      rejected ways is in Decisions; one test calls our code on the server.
- [x] **T5. Real containers in the world.** Place a chest (and the other storage our books read) on the superflat, fill it,
      and test what our adapters read from it and write into it: the cases that `TestContainers` and `GameWorld` mock today.
      Each mock that a server test now covers is marked or removed. After: T4. Model: sonnet. Author: none.
      Done when: the container cases run on the server; the mocks' "checked in game" notes they replace are updated.
- [x] **T6. A player without a client.** `IPlayer` cannot be mocked (`docs/development.md`). Research whether the server can
      give a real `IServerPlayer` without a client (the engine has `DummyNetConnection`, `DummyTcpNetServer`; a player joined
      through them?), with its inventories, rights and position. A research step: no client means no network messages.
      After: T4. Model: opus. Author: decides whether to go on and how. Done when: the answer and the chosen way (or
      "not possible, because") are in Decisions.
- [x] **T7. Books and invoices end to end.** With T6's player: making an invoice, posting it, the album taking from a chest,
      the want list, each "all or nothing" (`docs/critical.md`) on real inventories, and the access checks (claims,
      reinforcement). After: T5, T6. Model: sonnet. Author: none (or splits the step if large).
      Done when: the cases of `docs/testing/functional.md` that need no client have a server test, marked there.
- [x] **T10. The cases T7 left that need no new means.** With the fixture, `FakeClient` and the gestures of T7: I4 (a full
      album), I11 (blank lists: "Nothing to fill", Cancel with and without a book); L5, W3, G2, G4 with the config changed in
      the test and put back (`CurioCatalogueConfig` has setters, the poster reads it through lambdas); G3 if the collection is
      built after start (find out; if at start only, say so in Decisions); A6 on the server's side (a 60-character name through
      the channel is cut by `ModNetwork.LimitName`); I9 and I10 on the server's side: a plain click on a chest and Shift + click
      on the floor go to `base` (`docs/critical.md`, "Clicks that are not ours"), the chest opens, the list lies on the ground.
      After: T7. Model: sonnet. Author: none. Done when: each case has a test or a reason in Decisions why not; the covered
      ones are marked ⚙ in `functional.md`; a mutation per group (the config ignored, `base` not called) fails its test.
- [x] **T11. Crafting on the server.** Research first: whether stacks put into a real player's crafting grid on the server and
      the output taken run the game's recipe matching and our crafting hooks as in game (the grid inventory, the output slot,
      `OnCreatedByCrafting`, what the client normally sends). Then C1–C4, C4 being the open bug "a book could not be crafted
      from an old one": a test that reproduces it, if it does. After: T7. Model: opus. Author: decides whether to go on after the
      research, and whether the bug's fix (if found) is made in the step or gets its own. Done when: the answer and the way (or
      "not possible, because") are in Decisions; C1–C4 marked ⚙ or the reason why not.
- [x] **T12. Compatibility tests: Upgradeable Storage.** A fourth project `CurioCatalogue.CompatTests` with its own server that
      loads our mod and Upgradeable Storage (`upgradablestorage` 1.1.9, Decisions "Before T10"). The fixture is shared with
      `CurioCatalogue.ServerTests`, not copied (the way chosen in the step). The mods come from a pinned list in the repo
      (mod id, version, file URL, SHA-256): the folder first, then a download into it, as on CI; neither, or a wrong hash,
      fails the run. Tests: both mods load with no warning or error in the log; our books on its chests, trunks and storage
      vessels (each gesture of K1, the album and a list end to end, as `BookFlowTests` do on vanilla chests).
      Docs: `docs/development.md` (the fourth project, the mods folder, how to add a mod); `docs/architecture.md` Part 1
      (the project and the mods' source); `functional.md` K2 marked ⚙. After: T10 (the shared fixture moves code that T10 uses).
      Model: opus. Author: decides how the fixture is shared. Done when: the tests pass from an empty folder (download) and from
      a full one; a mutation (a wrong hash, the mod missing from the list) fails clearly.
- [x] **T8. CI.** The `build` job (it has the game server) runs the server tests and the compatibility tests; how long they take;
      what CI keeps when they fail (the server log of the temp world as an artifact); the mods folder of T12 cached between runs.
      After: T12. Model: sonnet. Author: none.
      Done when: `ci.yml` changed; `docs/development.md` (CI) says so; checked after the push (the author).
      Done locally 02.11.2026; the run on GitHub is for the author to check (Where we stopped).
- [ ] **T9. Coverage review.** What the four test projects cover now and what they do not: a coverage report (coverlet is in
      the tests already) per class of the adapters; the mocks left and whether each still needs to be a mock; the cases of
      `functional.md` still only in game; the item classes (`ROADMAP.md`, "Tests for the item classes"). Findings fixed in
      the step if small, otherwise lines in `ROADMAP.md`. After: T8, T10, T11. Model: opus. Author: decides on each finding.
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
  the output (trap 2). It references our mod and Core with `Private=false` (T4, Decisions).

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

T4 (02.11.2026):

- **The premise was wrong:** the mod loader loads our DLL with `Assembly.UnsafeLoadFrom` into the default load context, and a
  reference by name then gets that copy, so tests and game share one set of types by themselves (`docs/modding.md`, "Our types in
  the tests"). The question left was which file the game runs.
- **Chosen (author): reference the mod and Core with `Private=false`**, nothing of ours in the test bin; the `AssemblyResolve` hook
  also looks in the mod's output, for a test that touches our types before the server starts. The game and the tests run the file
  in `ModPaths`. Core is referenced by name too, or it comes in transitively and is copied.
- Rejected: **a plain project reference** (works, one copy, but the game runs the test bin's copy, because `LoadFrom` returns the
  same-named assembly of the app's list: correct only by a quiet runtime rule, and not the file the config names);
  **reflection** through `Api.ModLoader.GetMod(...)` (no type checks at compile time, verbose tests).
- **Proof:** `OurCodeTests`: the game's mod system and `ItemLedger` come from the assembly the tests compiled against, one copy of
  each DLL loaded; `StackKeys.KeyOf` keys a real book. Mutation: a `LoadFile` of our DLL in the test fails it with "curiocatalogue:
  2 copies loaded".

T6 (02.11.2026):

- **Possible, and with network messages too.** A socket of our own in the empty `ServerMain.MainSockets[0]` takes the packets a
  client sends on join, and the server makes a real `IServerPlayer` through its own join path: playing, in creative, spawned,
  with hotbar and backpack and the default role's rights. Messages to our channel go the same way and reach the real handlers of
  `ModNetwork`, so T7 can test the client's requests with the server's checks. How and the traps: `docs/modding.md`, "A player
  without a client".
- **Chosen (author): this way**, as the helper `FakeClient` (join, `SendToMod`, the packets received) and `PlayerTests` (a player
  joins; a rename request through our channel renames a held ledger). The fixture sets `VerifyPlayerAuth = false`; the join's
  "Server overloaded" warning is in `AssetLoadTests.KnownNoise` (author).
- Rejected: **a player built by hand** (`ConnectedClient.LoadOrCreatePlayerData` and the steps of the private
  `FinalizePlayerIdentification` repeated: no join events, no spawn, fragile); **no player** (hands, access and channel requests
  stay untested).
- **Costs:** the packet ids and the network version ("1.22.6") are the engine's and change with it (a wrong version fails the
  join loudly); `FakeClient.OurMessageTypes` repeats the order of `ModNetwork.Register`.
- **Mutations:** our message types out of order fails the rename test ("waited 30 s for the ledger to be renamed"); auth
  verification left on fails both tests at identification. A run longer than 10 s after a join first crashed the test process
  (the UDP check, `docs/modding.md`); the join now sends packet 34 first, and the 30 s mutation runs end cleanly.
- **Left for T7:** the server's ping timeout (`ClientConnectionTimeout`, 150 s; the fake client does not answer pings) and a
  player leaving (packet 14), if a test needs a clean server or runs long.

T7 (02.11.2026):

- **Gestures are called, not simulated:** a click is the held item's `OnHeldInteractStart` on the server with Shift (Ctrl) on the player's
  controls; the off-hand gesture and the window requests go through the fake client's channel. No client side (hints, windows, `EmptyHandGesture`
  on the client) is tested; that stays a game check.
- **Checks are on state and on the codes of the refusals** (`IngameError.Code`), not on texts: texts and their language are the lang files'
  and the `S` cases' business.
- **The `functional.md` cases covered are marked ⚙** (27 cases); the game check stays for what a client shows. Not covered: C1–C4 (crafting
  through the grid), A1, A6, A8, A9, A11, L5 (`SoftEntryLimit`, a config change), W3 (`ConsumeSheets`), I4, I9–I11, G, T, K2–K4.
- **The fixture's limits:** `MaxClients = 1000` and `ClientConnectionTimeout = 3600` (the defaults, 16 and 150 s, ended the run after ~16 joins);
  the long timeout is set, not tried. Player names are at most 16 characters.
- **Mutations:** the access check dropped from `Post` fails the claim and the padlock tests; `KindFor` letting an album act on Shift + Ctrl fails
  its test; the fingerprint check of `ShownSlot` dropped fails the stale-window test. A fourth (the Missing refusal swapped for another) still
  refused, so it proved nothing: all-or-nothing of the plan itself is in Core's `Transfer` tests, the server tests show it at work.

T5 (02.11.2026):

- **Real blocks through `SetBlock`**, not hand-made block entities: the vanilla block entities initialize themselves with the server api (a trunk
  through `DoPlaceBlock` with a joined player, for its filler cell). Each placement takes its own cell near the spawn.
- **The mocks stay as fast logic tests** (they run without the game, in the core CI job), as `AssetsTests` did in T2; each says in a comment which
  server test is its twin. Rejected: removing `TestChest` cases (the overlap is small, the cost of keeping them is nothing).
- **Access with the mod's own wiring** (`CurioCatalogueModSystem.Access`) and two joined players, claims added and removed per test.
- **Mutations:** the lock check dropped, `Traverse` asked instead of `Use`, the crate removed from `IsVanillaStorage`, `MainPos` returning its
  argument: each fails exactly one server test.
- Not covered (still mocks): `MayBeStorage` (a type check, no engine).

T10 (02.11.2026):

- **Covered, marked ⚙ in `functional.md`:** I4, I9, I10, I11 (post and cancel, three kinds), L5, W3, G2, G3, G4, A6 (`BookEdgeTests`, 23 cases). The
  helpers of `BookFlowTests` moved to `BookTestBase`.
- **Config on a running server:** `SoftEntryLimit` and `ConsumeSheets` are read live (lambdas), the test sets and restores them. `ExtraIgnoredAttributes`,
  `TapestryExclude`, `TapestryAdd` are read when `Keys` and `Collections` are first used (G3 answer: **built on first use, after the world loads, not at
  start**; a change counts after a restart), so `WithConfig` drops the mod's cached parts by reflection and drops them again afterwards.
  Rejected: building `StackKeys` and `CollectionRegistry` by hand in the test (does not check the mod's wiring of the config).
- **G4 is partly covered:** the flag in `ModLog` is set by the test (reflection, `ModLog` is internal), so lines appear or not; the wiring
  `Config.Diagnostics -> ModLog.DebugEnabled` in `Start` runs once at start and is not tested. Rejected: `InternalsVisibleTo` for the server tests.
- **I9, I10:** the engine's order is called by hand (the item first, the block's `OnBlockInteractStart` only if the item left the click): the chest is in
  `OpenedInventories`, the list lies in `BlockEntityGroundStorage` above a granite block. A creative player keeps the item in the hand (ground storage
  does not use it up), so the hand is not checked.
- **Mutations:** `SoftEntryLimit` ignored fails L5; `ConsumeSheets` ignored fails W3 and the intake case; `ExtraIgnoredAttributes` ignored fails G2;
  `TapestryExclude` ignored fails G3; `LimitName` off fails both A6 cases; `ModLog.Debug` always silent fails G4; the Shift check dropped from `ItemLedger`
  fails I9 (3 rows); the `base` call dropped from `ItemInvoice` fails I10 (the invoice row). Not tried: the same for `ItemLedger`.

T11 (02.11.2026):

- **Possible, with no new means** (research, author: go on). The grid is the player's own inventory on the server, open or not; a stack put into a slot
  runs the recipe search with our `MatchesForCrafting` and `OnCreatedByCrafting`, and `ActivateSlot` on the output with the mouse slot is what the
  server does with the client's packet 7, consuming through our `ConsumeCraftingIngredients`. How: `docs/modding.md`, "Crafting on the server".
  Rejected: sending packet 7 through `FakeClient` (the same call after decoding; only the packet ids would be new to keep up with).
- **C4 is not reproduced on the server, and needs no fix:** its cause was found and fixed in 0.1.1 (aged covers had no handbook page, so their
  recipes did not show; `d02759d`). The handbook is the client's; the server tests show that books with entries and aged covers do craft
  (recolour, recharge, switch mode) with their data and no warning in the log.
- **Covered, marked ⚙ in `functional.md`:** C1 (normal and aged cover), C2 (both old books; less than a litre makes nothing; the jug keeps the
  rest), C3 (and no album from a ledger with entries), C4 (`CraftingTests`, 10 cases).
- **Mutations:** no sheets for the fallback fails C1; the base `ConsumeCraftingIngredients` for recharging and the recharged ledger not written
  each fail the recharge case; an album from any ledger fails C3; switching a ledger with entries fails the switch case.

T12 (02.11.2026):

- **The fixture is shared as source files** (author): `CurioCatalogue.TestServer/`, no project of its own, with `TestServer.props` that both
  test projects import (references, the Newtonsoft copy, `TestPathsAsMetadata`, the shared `.cs` files as links). Each test assembly
  compiles its own copy, so each has its own module initializer and paths; each project declares its own collection. Rejected: a class
  library (its module initializer runs only when one of its types is first touched, maybe after a game type is loaded; `InternalsVisibleTo`
  for the internal types). The shared files moved with `git mv` and the namespace `CurioCatalogue.TestServer`; `KnownNoise` moved from
  `AssetLoadTests` to `LogProblems`, so both projects filter the same noise.
- **Other mods into the fixture:** a protected constructor takes `OtherMod(Id, Version, File)`; their files are copied into the temp world's
  `Mods` (a ModPaths entry of its own), so only listed files load, and the start fails unless each is enabled with its version.
  `TestBlocks.Place` takes a code with a domain.
- **The pin:** `mods.json` (mod id, version, file, URL, SHA-256), the URL `https://mods.vintagestory.at/download/99303/…`; the same hash as
  the copy the author's game got from a server and as the CDN link of the mod database's API. The folder is `CurioCatalogue.CompatTests/mods/`
  (git-ignored; T8 caches it). A failed download leaves nothing behind (`.part`, deleted).
- **A loaded DLL cannot be deleted:** a code mod is unpacked into `Cache\unpack` and its DLL stays locked until the process ends. With other
  mods the fixture leaves `Cache`, and the next run of either project deletes data folders that hold only `Cache` (a failed start keeps
  `Logs`, so it is never swept). Rejected: tolerating any failure to delete (would hide a real leak); loading the mods from the pin folder
  directly (a stray file there would load too).
- **Covered (`UpgradeableStorageTests`, 15 cases):** both mods load with a clean log; its chest, trunk, storage vessel, labeled chest, labeled
  trunk and labeled vessel are supported (their block entity classes named in the test); on each, an intake list and a pick list made,
  the list posted with the ledger in the off-hand, the album taking, a want list filled; the off-hand gesture through the channel on a labeled
  trunk; a padlock on a labeled vessel. K2 marked ⚙. No change to the mod was needed.
- **Mutations:** a wrong hash with the file kept ("delete the file to download it again, or fix the list"); a wrong hash from an empty folder
  (the download refused, nothing kept); the mod missing from the list (every test fails: "no block upgradablestorage:…", the load test
  "(True, False)"); a wrong version ("upgradablestorage is 1.1.9, expected 1.1.8", the start refused).

Before T10 (02.11.2026, author):

- **More of `functional.md` on the server.** T7's "not covered" list was made before the fixture could do what it does now; T10 takes
  what needs no new means, T11 crafting (with research first). Stay in game: A1, A8, A9 (windows), T1, textures, shapes, rendering.
- **Compatibility tests start with positive integrations:** Upgradeable Storage first (README: "Tested with"; its chests are built on
  the vanilla ones, so books should work). Not now: Better Crates (not supported, `ROADMAP.md`, When pigs fly) and Carry On (its
  conflict with our gestures is client input).
- **A project of their own** (`CurioCatalogue.CompatTests`): one server per process, so other mods do not change what the server tests
  see. Rejected: other mods in the server tests' server (every failure would have two suspects).
- **Other mods are not in the repo** (their licences): a pinned list with SHA-256 is. Locally and on CI the same way: a folder first,
  then a download from the mod database; without the mod the run fails, not skips (a skip would hide a missing check).
- **The steps' order:** T10, T11, T12, then T8 (CI runs all four projects at once), then T9.

## Where we stopped

- 02.11.2026: T1 done: the project, the fixture and three smoke tests (six cases) pass in about 13 s; mutations checked (a
  wrong mod path: "our mod is not enabled; mods: …"; `GamePaths.DataPath` not set: "the data path is …\VintagestoryData",
  nothing written there). CI still runs `dotnet test curiocatalogue.sln`, which now includes the server tests (T8).
- 02.11.2026: T2 done: `GameServerFixture.LogEntries()` and `AssetLoadTests` (log captured from the start; fails on any warning or error); two mutations checked; `critical.md`, `modding.md`, `development.md` updated. Next: T3 or T4 (both After: T1).

- 02.11.2026: T3 done: `RecipeTests` (8 tests, 1067 recipes of our domain checked against expected sets); three mutations checked; `development.md` updated. Next: T4 (opus) or T8.
- 02.11.2026: T4 done: tests reference our DLLs without copying them; `OurCodeTests` (2 tests) call our code on the server; all
  three projects pass. Next: T5 (sonnet) or T6 (opus), both After: T4; T8 any time.
- 02.11.2026: T6 done: `FakeClient` joins a real player without a client and sends requests to our channel; `PlayerTests`
  (2 tests); all three projects pass (98 + 160 + 20). Next: T5 (sonnet, After: T4); T7 waits for T5; T8 any time.
- 02.11.2026: T5 done: `ContainerTests` (18 cases: real chest, trunk, labeled chest, vessel, crate, machines, sign, loot chest, backpack in a chest and on the ground, claims, padlock); four mutations checked; the mocks point to their server twins; `development.md`, `modding.md` updated. All three projects pass (98 + 160 + 38). Next: T7 (sonnet, After: T5, T6, both done); T8 any time.
- 02.11.2026: T7 done: `BookFlowTests` (24 cases: lists made and posted, album, want list, claims, padlock, reach, stale windows), `TestBlocks`, fixture limits; all three projects pass (98 + 160 + 62); `development.md`, `modding.md`, `functional.md` (⚙) updated. Next: T8 (sonnet, After: T1); T9 (opus) after T8.
- 02.11.2026: T10 done: `BookEdgeTests` (23 cases) and `BookTestBase` (helpers moved out of `BookFlowTests`); all three projects pass (98 + 160 + 85); eight mutations checked; `functional.md` (⚙), `development.md`, `modding.md` updated. Next: T11 (opus) or T12 (opus, After: T10).
- 02.11.2026: T11 done: `CraftingTests` (10 cases, C1–C4 through a real player's grid); all three projects pass (98 + 160 + 95); five mutations checked; `functional.md` (⚙), `development.md`, `modding.md` updated. Next: T12 (opus, After: T10).
- 02.11.2026: T12 done: the fixture shared from `CurioCatalogue.TestServer/`; `CurioCatalogue.CompatTests` with Upgradeable Storage 1.1.9
  (15 cases), from an empty folder (download) and a full one; four mutations checked; all four projects pass (98 + 160 + 95 + 15);
  `development.md`, `architecture.md`, `modding.md`, `functional.md` (K2 ⚙) updated. Next: T8 (sonnet, After: T12).
- 02.11.2026: T8 done in the repo: `ci.yml` caches the pinned mods, runs the four projects with durations, uploads `server-logs` on failure (the fixture copies `Logs` when `CURIOCATALOGUE_KEEP_LOGS` is set); `development.md` updated; locally all four pass (98 + 160 + 95 + 15, 1 min). **Waits for the author:** push and see the first run on ubuntu (the Linux questions of "Open questions": paths, port, the Linux server archive, the time). Next: T9 (opus, After: T8).
