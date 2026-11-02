# Development

How to build, test, run and ship the mod, and how changes reach `main`.
First-time machine setup is in `docs/onboarding.md`.

**Update when** the build, the tests, the launch profiles, CI or the branch flow change.

## Repository layout

```
curiocatalogue/            the mod: game glue (items, adapters, GUI, network); assets/ and modinfo.json
CurioCatalogue.Core/       domain core: sets and multisets of items; never references game DLLs
CurioCatalogue.Core.Tests/ xUnit tests of the core
CurioCatalogue.Tests/      xUnit tests of the adapters, against the game API with mocks
CurioCatalogue.ServerTests/ xUnit tests against a real game server started inside the test process
CurioCatalogue.CompatTests/ the same server with other mods loaded (compatibility tests)
CurioCatalogue.TestServer/ what both share: the fixture, the fake client, test helpers, TestServer.props (no project of its own)
CakeBuild/, build.ps1      release zip (from the official mod template)
tools/                     scripts that generate assets (see below)
docs/                      documentation; docs/plans/ holds the implementation plans
```

## Build and test

- Build: `dotnet build curiocatalogue.sln`. Output: `curiocatalogue\bin\Debug\Mods\mod`. CakeBuild pins
  `System.Security.Cryptography.Pkcs` over the version Cake brings (`NU1903`); drop the pin when Cake brings a fixed one
  (`dotnet list CakeBuild package --vulnerable --include-transitive`).
- The projects reference the game from `$(VINTAGE_STORY)`: `VintagestoryAPI.dll`, `Mods/VSSurvivalMod.dll`, `Mods/VSEssentials.dll`
  (`Private=false`, never copied into the output). No game DLL is committed.
- While the game runs, a build compiles but cannot copy `curiocatalogue.dll` into the output (file locked, MSB3027).
  Harmless: the next build after closing the game copies it.
- Tests: `dotnet test curiocatalogue.sln`. All four test projects must pass before a commit. The server tests take about
  45 s (the server starts in about 10 s), the compatibility tests about 20 s, side by side; alone: `dotnet test CurioCatalogue.ServerTests`,
  `dotnet test CurioCatalogue.CompatTests`.

## Writing tests

- `CurioCatalogue.Core.Tests` tests the pure core: plain values, no mocks needed. Set and multiset logic belongs there.
- `CurioCatalogue.Tests` tests the adapters. It references `VintagestoryAPI.dll` plus `Lib/protobuf-net.dll` and `Lib/Newtonsoft.Json.dll`
  from the game. Game data types (`TreeAttribute`, `ItemStack`, `new Block { Code = ... }`) work without the engine; the rest is
  mocked with NSubstitute (Mockito-like: `Substitute.For<IWorldAccessor>()`).
- **Mocks document our assumptions about the engine.** Every mocked engine behaviour, and every test that relies on how a game
  type behaves, carries a comment with its source: a file and line in vsapi/vssurvivalmod, a section of the docs, or
  "checked in game <date>". A mock without a source is a hypothesis: confirm it by reading the source, decompiling, or in game.
- `IPlayer` (and `IServerPlayer`) cannot be mocked or implemented outside the game: it has an `internal` member. Code under test
  only passes the player on, so tests use `null!`. Prefer code that takes what it needs (e.g. a list of player slots, see
  `InvoicePoster.Post`) over code that takes an `IPlayer`. A real player is in the server tests (`FakeClient`, below).
- Real slot operations (`TryPutInto`, `MarkDirty`, stack `Equals`) need an api: give inventories `game.Api` (the test helper
  `GameWorld`), call `LateInitialize(id, game.Api)` on inventories made by a block entity constructor; `GameWorld` also sets the
  collectibles' protected `api` field.
- `AssetsTests` checks the asset traps of `docs/critical.md` (JSON parses, lang keys and braces, `game:` in recipes, comments in
  recipe arrays) without starting the game. A new trap of this kind gets a test there.
- Concrete game classes without virtual members (e.g. `ModSystemBlockReinforcement`) cannot be mocked: wrap them behind a small
  interface of our own.

### Server tests

`CurioCatalogue.ServerTests` starts the real game server (no client) inside the test process: a superflat creative world in a
throwaway folder `%TEMP%\cc-servertests-…`, with the game's mods and ours. What the engine does there needs no source comment:
the engine ran it. How it works and its traps: `docs/modding.md`, "A server inside the test process".

- **One server per process**, shared by every test through `[Collection(ServerCollection.Name)]` (`GameServerFixture`); a second
  instance throws. `dotnet test` runs each test project in its own process, so the compatibility tests get a server of their own.
- **The fixture is shared, not copied:** `CurioCatalogue.TestServer/` holds the fixture, `GameAssemblies`, `FakeClient`, `TestBlocks`,
  `BookTestBase`, `LogProblems` and `TestServer.props` (the references, the Newtonsoft copy, `TestPathsAsMetadata`, and those `.cs` files as
  links). Each test project imports the props, so each test assembly compiles its own copy, with its own module initializer and paths.
  Each project declares its own `[CollectionDefinition]`.
- Game code runs on the server's main thread: `game.OnMain(() => …)`, as in the game.
- It needs the game in `$(VINTAGE_STORY)` and builds the mod first. The paths (the game, the built mod, its assets) are written
  into the test assembly at build time from MSBuild (target `TestPathsAsMetadata`), not hard-coded.
- **Tests call our code directly** (`OurCodeTests`): the project references the mod and Core with `Private=false`, so neither is
  copied into the test bin; the game's mod loader loads them from the mod's output and the tests bind to that same copy.
  `The_tests_and_the_game_share_one_copy_of_our_types` fails if a second copy is ever loaded. Why: `docs/modding.md`, "A server
  inside the test process".
- **The server log is captured from the start** (`GameServerFixture.LogEntries()`, a subscription to `ServerMain.Logger.EntryAdded`
  made before the mod loader runs). `AssetLoadTests` fails on any Warning, Error or Fatal line logged until RunGame (`StartupLog()`, the
  same whatever test ran before), except the lines of `LogProblems.KnownNoise`
  (a vanilla line that is not ours goes there with its reason and game version). Server only: textures and shapes are read by the
  client, so a missing one is still found in game.
- **A real player without a client:** `FakeClient.Join(game, "name")` joins through a fake socket with the packets a client
  sends and returns when the player is playing (`IServerPlayer` with inventories, rights, a position); `SendToMod(request)` sends
  a message to our channel, handled by the server at a later tick (wait with `WaitFor`); `Received` holds what the server sent.
  The fixture sets `VerifyPlayerAuth = false` for it. `FakeClient` reads the ids of our messages from the server's channel,
  so `ModNetwork.Register` may change freely. How and its traps: `docs/modding.md`, "A player without a client".
- **`RecipeTests`** compares the server's grid recipes of our domain with expected sets written out in the test (output, ingredient
  codes, counts, tools, shape, group): 1067 recipes in 7 groups. A recipe change in `recipes/grid/ledger.json` is made in two places;
  a new recipe name fails `All_our_recipes_are_covered_by_a_test_above` until it has a test.
- **`ContainerTests`** puts real vanilla blocks into the world (`BlockAccessor.SetBlock`, a trunk with `DoPlaceBlock` and a joined player)
  near the spawn and runs `ContainerReader` and the mod's own `ContainerAccess` (`CurioCatalogueModSystem.Access`) on them: chest, trunk,
  labeled chest, vessel, crate, barrel, quern, firepit, sign; a backpack in a chest and on the ground (open, save, read back); a per-player
  loot chest; claims and a padlock with two real players. The hand-made `TestChest` cases in `CurioCatalogue.Tests` stay as fast logic
  tests and point to their server twin. A block code in a test is the vanilla code (`chest-east`, `crate`, `barrel`, `backpack-normal`).
- **`BookFlowTests`** runs the books end to end with real players and chests: a gesture is the held item's `OnHeldInteractStart` called on the
  server with Shift (and Ctrl) set on the player's controls; the off-hand gesture and the window requests (rename, clear, want list, cancel)
  go through `FakeClient.SendToMod` into the real handlers. It checks the world's state (inventory, chest, book, list) and the *codes* of the
  red refusals the fake client received (`Errors(client)`), not their texts. Cases: making intake, pick and by-count lists; posting (moves,
  missing, no room, parchment); the album taking, the want list (made, filled, partly filled, cancelled); claims, a padlock, unsupported
  containers, reach and stale windows. `TestBlocks` hands out the cells for all test classes.
  The `functional.md` cases it covers are marked with ⚙ there.
- **`BookEdgeTests`** (helpers shared with `BookFlowTests` in `BookTestBase`) takes the cases the first one left: a full album, blank lists (post and
  cancel), a 60-character name through the channel, and the clicks that are not ours (a plain click opens the chest, Shift on the floor
  lays the book on the ground). The config is changed on the running server by `WithConfig` and put back in `finally`; it also drops the mod's
  lazily built services (every private field of ours, found by reflection) because they take the config when first used; the config is
  restored from a JSON copy, so a setting added later is restored too. `WithConfig` is in `BookTestBase`.
  `Diagnostics` is tested through the flag of `ModLog` (internal, by reflection), not through the config.
  The branches the coverage review found untested (T9 of the server tests plan) are here too: an album started over, a want list into the empty
  main hand, refused, or on the ground, a list on the ground, a full ledger. A full inventory needs the player in survival (`AsInSurvival`):
  a creative player's inventory is never full (`docs/modding.md`, "A server inside the test process").
- **`CraftingTests`** crafts through a real player's grid (C1–C4 of `functional.md`): stacks put into the grid's slots, the output taken with
  `ActivateSlot` as the server does with the client's packet; it checks the recipe found, the book made (cover, sheets, entries, name) and
  what is left in the grid. How: `docs/modding.md`, "Crafting on the server".
- **`LanguageTests`** (T2 of `functional.md`): `FakeClient.Join(game, "name", "uk")` joins with a client language (the player's `LanguageCode`) on the
  fixture's English server. A red refusal, the chat line after making a list, after a post (the sheet back in the album) and after the album
  taking come in the player's language; the expected texts are read from the mod's `lang/*.json`, not written in the tests. Join also
  greets the player in the chat ("Welcome …", the server's language): the tests skip that line.
- **`ConfigFileTests`** (G1): calls the mod's private `LoadConfig` by reflection on the running server with a config file the test writes into
  the temp world's `ModConfig` (put back afterwards): corrections are logged as warnings and written back, a good file gets the settings it
  lacked, a broken file stays as it is. The mod's own `Config` is not touched. A test reads what its action logged with `GameServerFixture.LogMark()` and
  `LogSince(mark)`; `LogProblems.Since` leaves out the known noise (also in the C4 crafting tests).
- **`RefusalTests`** (the section "Not reproducible in game" of `functional.md`): `newerversion` (the test raises `v` of a book's or invoice's data above
  `LedgerStore.Version`: a click in hand, an invoice with the book in the off-hand, the off-hand gesture through the channel, the four window requests and
  the cancel with a newer book in the off-hand are refused, and the stacks' attributes are the same afterwards) and `nocollection` (an album of a
  collection the server does not have). `movefailed` stays a unit test: no container of the server accepts only part of what is put in.
- **`CreativeInventoryTests`** (C6): the server fills `CreativeInventoryTabs` of every item from the JSON (`ItemType.GetCreativeTabs`), and the client's creative dialog and the handbook read that field: 24 books per kind (13 normal, 11 aged), 72 in the `general` tab, the three blank lists there too (`ROADMAP.md` wants them out; the test changes then), nothing else of ours.
- **`TooltipTests`**: the names and tooltips of the books and lists and the "Missing: …" text of a refused post (`ModNetwork.MissingText`, internal, by
  reflection), in the server's English. The client calls them, but they need no client; the key names fall back to "Shift" and "Ctrl".
- **`VanillaContentTests`**: the server twins of assumptions the adapter tests make about vanilla stacks (a tapestry taken off a wall, a hot ingot).
- The server listens on a free port chosen at start, so a game or a server running on this machine does not get in the way.
- After a passing run the temp folder is deleted. If the server fails to start, the folder stays and the error names its
  `Logs\server-main.log`; delete such folders by hand. With other mods, `Cache` stays (their DLLs are loaded from it and locked:
  `docs/modding.md`, "A server inside the test process"); the next run of either project deletes folders that hold only `Cache`.

### Compatibility tests

`CurioCatalogue.CompatTests` runs the server of the server tests (`CompatServerFixture`, a `GameServerFixture` given the other mods) with
other mods loaded, so their effects never reach the server tests. Now: Upgradeable Storage (`UpgradeableStorageTests`: both mods load with a
clean log; its six kinds of storage are supported; every gesture of K1 on each; the off-hand gesture; a padlock), and our test mod
`cctapestry` (`ModdedTapestryTests`: a picture patched into the game's tapestry `sizes` is in the collection, and the album takes it).

- **Test mods of our own** stand in for a kind of mod no real one fits: a folder each in `CurioCatalogue.CompatTests/testmods/` (in the
  repo) with its `modinfo.json`. `TestMods` lists them; the fixture copies each folder into the temp world's `Mods` and checks it is
  enabled with its version, as for the pinned mods. A new one needs only its folder.

- **The mods are not in the repo** (their licences). `CurioCatalogue.CompatTests/mods.json` pins each one: `modid`, `version`, `file`, `url`,
  `sha256`. `PinnedMods` takes the file from `CurioCatalogue.CompatTests/mods/` (git-ignored) or downloads it there from `url`; a wrong hash
  or a failed download fails the run (never a skip). The fixture copies the files into the temp world's `Mods` (so only listed files load)
  and fails to start unless each mod is enabled with the listed version.
- **Adding a mod** (or a new version): its file on the mod database (`https://mods.vintagestory.at/api/mod/<modid>` lists the releases;
  the download URL is `https://mods.vintagestory.at/download/<fileid>/<filename>`), its SHA-256 (`sha256sum`, or `Get-FileHash` in
  PowerShell), a line in `mods.json`, and a test class of its own. A block of another mod is placed with its domain:
  `TestBlocks.Place(game, "upgradablestorage:chest-east")`.
- Every mod in the list is loaded for every compatibility test: two mods that conflict with each other would need a project each.

## Exhaustive `switch`

The closed hierarchies of records (`TransferPlan`, `CreateInvoiceResult`, `MakeInvoiceResult`, `PostInvoiceResult`, `CollectResult`,
`AttrValue`) carry `[Closed(typeof(A), typeof(B), …)]` and every `switch` over them ends in `default: throw ExhaustiveMatch.Failed(x)`.
The analyzer `ExhaustiveMatching.Analyzer` then turns a missed case into error `EM0003` at build time, locally and in CI. A new subtype
of a closed record is added to its `[Closed]` list; a new hierarchy of the same kind gets the attribute too.

- **Enums too:** a switch over an enum that ends in `_ => throw ExhaustiveMatch.Failed(x)` must name every value (`EM0001`). Use it
  wherever an enum maps to another enum or to a text; keep `_ =>` only for a deliberate fallback (e.g. `Reason.ToString()` as a
  lang key, which `AssetsTests` guards). Tuples and game types are not covered.
- The analyzer rejects `when` guards and property patterns (`Refused { Reason: X }`) in such a switch (`EM0100`, `EM0101`): switch on the
  type, then on the property inside the case.
- The analyzer is referenced in both `CurioCatalogue.Core.csproj` and `curiocatalogue.csproj`, with `ExcludeAssets="compile;runtime"`:
  only the analyzer. `ClosedAttribute` and `ExhaustiveMatch` are our own (`CurioCatalogue.Core/ExhaustiveMatching.cs`); the mod ships no
  third-party dll (`docs/architecture.md`, Exhaustive switch). Check after changing the package: remove a case, see `EM0003` / `EM0001`.

## Run and debug in game

Launch profiles are in `curiocatalogue\Properties\launchSettings.json` (F5 in Visual Studio):

| Profile | What it runs |
|---|---|
| **Client** | the dev game with our mod (`--addModPath` to the build output, `--addOrigin` to `curiocatalogue\assets`) |
| **Client (no mod)** | the same data folder without our mod: compatibility checks, other mods' bugs, joining a dedicated server |
| **Server** | a dedicated server with our mod, on the client's data folder |
| **Server (dedicated)** | a dedicated server on its own data folder, with the release zip in its `Mods` |

- `--dataPath` must come **after** the `Vintagestory.dll` argument, otherwise `dotnet` takes it as its own option.
- The `--addOrigin` folder must exist, or the game crashes on start. Disabling the mod in the game's Mods menu does not remove
  this origin: our item JSON still loads and logs "no such class registered". Use **Client (no mod)** instead.
- Other mods for tests go into the dev data folder's `Mods`.
- Logs are in the data folder's `Logs\`: `client-main.log`, `server-main.log` (the singleplayer server too), `client-crash.log`, `client-chat.log`.

### Diagnostics

- Config `Diagnostics` (off by default) turns on our debug lines: `"Diagnostics": true` in `<data folder>\ModConfig\curiocatalogue.json`,
  restart. Lines look like `[CurioCatalogue] Server|Client debug: ...` in `*-main.log`; server lines start with the player's name.
- Write them with `ModLog.Debug(api, $"...")`. Its interpolated string handler skips the whole message, values included, when off.
  Data gathered only for the log (loops, lookups) goes inside `if (ModLog.DebugEnabled) { ... }`.
- Keep useful diagnostics in the code instead of adding and removing temporary logging.

### Dev commands

The `/curio` chat commands (`DevCommands.cs`) live on the branch `dev-commands`, never in `main`; the list and how to use the branch are in
`docs/testing/commands.md`.

### Dedicated server test

- Profile **Server (dedicated)**: its own data folder (superflat creative world, `ServerLanguage: "en"`, so item names in the
  server's language show up as English), the mod as the release zip in its `Mods`.
- The client (profile **Client (no mod)**) needs the same zip in its own `Mods`: a server does not send mods, the client downloads
  missing ones from the ModDB, and an unpublished build is not there. Remove that zip before using **Client** again, or the mod loads twice.
- Rights, whitelist, claims and padlocks: `docs/testing/commands.md`. A second player on the LAN joins `<this PC>:42420`.
- On Windows, a Wi-Fi network marked Public blocks inbound connections: mark it Private and allow `dotnet` for private networks.

## Tools

Python and Node are not required; scripts are PowerShell (or Perl from Git for Windows).

- `tools/textures.ps1` draws our item textures as whole PNGs (no vanilla pixels copied).
- `tools/modicon.ps1` makes `curiocatalogue/modicon.png` from `tools/modicon-source.png` (a pixelated in-game screenshot).
- `tools/lang-table.pl` prints all texts of `lang/en.json` and `lang/uk.json` as Markdown tables, side by side, for proofreading.
- For an AI agent: make multi-line or quote-heavy edits (backslashes, `\n` in JSON, apostrophes in Ukrainian text) with an
  editor tool, not perl or shell heredocs; those corrupted files several times.
- `ConvertFrom-Json` validates strict JSON (lang files, recipes). Item and block types are JSON5 (unquoted keys, comments): it
  rejects them, as it does vanilla's.

- `tools/coverage.ps1` measures line coverage by all four test projects (coverlet) and writes a table per source file into
  `TestResults/coverage/report.md` (git-ignored): unit and server tests apart, and the lines no test runs. For the server tests it copies
  `VintagestoryAPI.dll` into the mod's output for the run (coverlet must resolve it; the game does not load it from there). About 2 minutes.
- `tools/check-refs.ps1` checks every reference into the game source (`File.cs:line#Symbol`, see `docs/modding.md`): the symbol
  must be on that line at the game version in `modinfo.json`. It reads the clones named by `VINTAGE_STORY_SRC` (see
  `docs/onboarding.md`) at their "Updated to Version X" commits, without checking anything out. `-GameVersion x.y.z` checks another version.

## Updating the game version

1. `git fetch` the three source clones; make sure each has the commit "Updated to Version <new>".
2. `tools/check-refs.ps1 -GameVersion <new>`. For each failure, read the new source: fix the line number if the code only moved,
   otherwise re-check the behaviour and the code that relies on it.
3. Re-check what was confirmed only in game ("checked in game <date>" in tests and docs) where the changelog touches it.
4. Update the dev copy of the game, `dependencies.game` in `modinfo.json`, and the version in `docs/onboarding.md`; build and run all tests.

## Commits

- English. Subject `Area: what changed` (e.g. `Album: take new pieces straight from a chest`), body wraps at ~72 columns and says why.
- One logical step per commit; tests pass at every commit.
- Line endings are fixed by `.gitattributes` (LF in the repository).
- Case-only renames: Windows ignores case, so use `git mv -f old New`.

## Branches, PRs and releases

The repository: github.com/dankosedin/CurioCatalogue. Its rulesets enforce the rules below for `main` and `release`.

- Work on `feature/<name>`. `main` changes only through a pull request: squash merge only, linear history, the branch must be
  up to date with `main`, CI must pass. No force pushes, no deletion.
- The squash commit on `main` takes the PR's title (plus ` (#N)`) and description, so write them as a commit message. Its author
  is whoever merges, under their GitHub profile name. GitHub itself appends the `Co-authored-by` lines of the branch's commits:
  keep such trailers in the commits, not in the PR description, or they appear twice.
- Reviews: GitHub does not let anyone approve their own pull request, so the `main` ruleset requires 0 approvals while one
  person has write access; that person reviews before merging. With a second one, set 1 required approval in the `main` ruleset.
  How a change is reviewed (a players' round, then a code review, both by role subagents): `docs/review.md`.
- CI (GitHub Actions) runs the core tests first (they need no game), then builds against the game server and runs all four test
  projects (`dotnet test curiocatalogue.sln`, including the server and the compatibility tests, which start a real server on the
  Linux runner); on `feature/*`, `main`, `release` and on every PR (on the merge result).
  It downloads the game's server archive for the version in `modinfo.json` to get the DLLs, cached between runs.
  - **The pinned mods** (`CurioCatalogue.CompatTests/mods/`) are cached with the key `pinned-mods-<hash of mods.json>`; a changed pin
    downloads again, a wrong hash still fails.
  - **When a test fails**, the step "Server logs of a failed run" uploads the artifact `server-logs` (14 days): the fixture copies the
    `Logs` of each server into the folder named by the environment variable `CURIOCATALOGUE_KEEP_LOGS` (set only in CI) before it
    deletes the temp world; a start that failed or a crash keeps the whole data folder, whose `Logs` are uploaded too.
  - **How long:** locally the four projects take about 1 minute (server tests 43 s, compatibility tests 6 s); on GitHub (ubuntu, game cached) the Test step took 78 s
    and the whole job about 2 minutes (first run, 02.11.2026); the Test step prints each project's duration.
- **Release:** `release` only moves forward to a commit of `main` (`git push origin main:release`, by the author). CI then builds,
  tests, makes the zip (CakeBuild), tags `vX.Y.Z` from `modinfo.json` (fails if the tag exists) and creates a GitHub Release
  with the zip; the release notes are that version's section of `CHANGELOG.md`. The author uploads the same zip and notes to the
  ModDB by hand (it has no release API). This is the job `release` in `ci.yml`, after `build`.
- **ModDB** (https://mods.vintagestory.at/curiocatalogue) takes HTML, not Markdown: paste it in the editor's source code mode
  (`<>`). The description is `README.md` with `README.uk.md` inside a spoiler
  (`<div class="spoiler"><div class="spoiler-toggle">Caption </div><div class="spoiler-text">…</div></div>`), images from raw
  GitHub at the release tag, relative links made absolute. The release's changelog is the `CHANGELOG.md` section. Uploaded
  screenshots: at most 1920×1080 and 2 MB; the logo is one of them, 480×480 or 480×320.
- **Changelog:** a version's heading is `## X.Y.Z (unreleased)` while it is being made and `## X.Y.Z (YYYY-MM-DD)` from the
  release day; the release job fails on a missing, undated or empty section. Before a release: the version in `modinfo.json`,
  the date in `CHANGELOG.md` and the version in both READMEs' early-development warning. A fresh Fendragon invite in both READMEs'
  thanks (an invite expires in 30 days); the author asks for it on the server.
- **Before a release**, with the release zip:
  - a minor release (0.x.0), or a patch that changes stored data, network messages or access checks: all of
    `docs/testing/functional.md` and the release's `docs/testing/upgrade-<from>-to-<to>.md`, in single player and on the
    dedicated server;
  - any other patch release (0.x.y): the smoke test in `functional.md`; its new cases were checked in game in their steps.
  The results go into the release PR's description (`functional.md`, Results).
- Release zip by hand: `build.ps1` (CakeBuild) → `Releases\` (it replaces only its own folder and zip there).
