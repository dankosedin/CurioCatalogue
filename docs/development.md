# Development

How to build, test and run the mod. Writing tests is in `docs/tests.md`; pull requests, CI and releases in `docs/releasing.md`.
First-time machine setup is in `docs/onboarding.md`.

**Update when** the build, the launch profiles or the way to run the tests change.

## Repository layout

```
curiocatalogue/            the mod: game glue (items, adapters, GUI, network); assets/ and modinfo.json
CurioCatalogue.Core/       domain core: sets and multisets of items; never references game DLLs
CurioCatalogue.Core.Tests/ xUnit tests of the core
CurioCatalogue.Tests/      xUnit tests of the adapters, against the game API with mocks
CurioCatalogue.ServerTests/ xUnit tests against a real game server started inside the test process
CurioCatalogue.CompatTests/ the same server with other mods loaded (compatibility tests)
CurioCatalogue.TestServer/ what both share: the fixture, the fake client, test helpers, TestServer.props (no project of its own)
CurioCatalogue.GoldenCapture/ makes the golden stacks of a release (run by hand, not built with the solution)
CurioCatalogue.PageGen/    the ModDB page generator (Markdig, Scriban, PreMailer.Net); no game DLLs
CurioCatalogue.PageGen.Tests/ xUnit tests of the generator (no game needed)
CakeBuild/, build.ps1      release zip (from the official mod template) and the task ModDbPage
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
- Tests: `dotnet test curiocatalogue.sln`. All the test projects must pass before a commit; how long they take: "How long" in `docs/releasing.md`.
  Alone: `dotnet test CurioCatalogue.ServerTests`,
  `dotnet test CurioCatalogue.CompatTests`.

## Writing tests

In `docs/tests.md`; the server inside the tests in `docs/test-server.md`.

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
5. The server tests rest on the decompiled engine, which `check-refs.ps1` does not check: read `docs/test-server.md` (with "A player without
   a client"), against the new `VintagestoryLib.dll` (`ilspycmd`); re-check `LogProblems.KnownNoise` (each
   line names its game version); re-pin `CurioCatalogue.CompatTests/mods.json` to versions of the other mods made for the new game.

## Commits

- English. Subject `Area: what changed` (e.g. `Album: take new pieces straight from a chest`), body wraps at ~72 columns and says why.
- One logical step per commit; tests pass at every commit.
- Line endings are fixed by `.gitattributes` (LF in the repository).
- Case-only renames: Windows ignores case, so use `git mv -f old New`.

## Branches, PRs and releases

In `docs/releasing.md`.
