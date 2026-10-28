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
CakeBuild/, build.ps1      release zip (from the official mod template)
tools/                     scripts that generate assets (see below)
docs/                      documentation; docs/plans/ holds the implementation plans
```

## Build and test

- Build: `dotnet build curiocatalogue.sln`. Output: `curiocatalogue\bin\Debug\Mods\mod`. The `NU190x` warnings come from the
  template's CakeBuild project and are harmless.
- The projects reference the game from `$(VINTAGE_STORY)`: `VintagestoryAPI.dll`, `Mods/VSSurvivalMod.dll`, `Mods/VSEssentials.dll`
  (`Private=false`, never copied into the output). No game DLL is committed.
- While the game runs, a build compiles but cannot copy `curiocatalogue.dll` into the output (file locked, MSB3027).
  Harmless: the next build after closing the game copies it.
- Tests: `dotnet test curiocatalogue.sln`. Both test projects must pass before a commit.

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
  `InvoicePoster.Post`) over code that takes an `IPlayer`.
- Real slot operations (`TryPutInto`, `MarkDirty`, stack `Equals`) need an api: give inventories `game.Api` (the test helper
  `GameWorld`), call `LateInitialize(id, game.Api)` on inventories made by a block entity constructor; `GameWorld` also sets the
  collectibles' protected `api` field.
- `AssetsTests` checks the asset traps of `docs/critical.md` (JSON parses, lang keys and braces, `game:` in recipes, comments in
  recipe arrays) without starting the game. A new trap of this kind gets a test there.
- Concrete game classes without virtual members (e.g. `ModSystemBlockReinforcement`) cannot be mocked: wrap them behind a small
  interface of our own.

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

The `/curio` chat commands (`DevCommands.cs`, privilege `gamemode`) live on the branch `dev-commands`, never in `main`:
`sheets <n>`, `tapestries [count]`, `tapestry <piece> [qty]`, `jug [lime|acid] [litres]`, `missing`.
For a test session, merge or cherry-pick them into a scratch branch; never merge them back into `main`.
CI does not build this branch, so it may lag behind `main`: rebase it and fix what broke when you need it.

### Dedicated server test

- Profile **Server (dedicated)**: its own data folder (superflat creative world, `ServerLanguage: "en"`, so item names in the
  server's language show up as English), the mod as the release zip in its `Mods`.
- The client (profile **Client (no mod)**) needs the same zip in its own `Mods`: a server does not send mods, the client downloads
  missing ones from the ModDB, and an unpublished build is not there. Remove that zip before using **Client** again, or the mod loads twice.
- Rights: `/op <name>` in the server console. A second player on the LAN joins `<this PC>:42420`; the server is whitelisted by
  default (`WhitelistMode: 0`): `/player <name> whitelist add`.
- On Windows, a Wi-Fi network marked Public blocks inbound connections: mark it Private and allow `dotnet` for private networks.
- Land claims: create with `/land claim new|start|end|add|save`. To change one, `/land claim load <id>` (id from `/land list`) before
  `grant <name> use` / `revoke <name>`, then `/land claim save`. The game's hint suggests `/land claim modify`, which does not exist.
  A padlock needs the block reinforced first (plumb and square, stones in the inventory).

## Tools

Python and Node are not required; scripts are PowerShell (or Perl from Git for Windows).

- `tools/textures.ps1` draws our item textures as whole PNGs (no vanilla pixels copied).
- `tools/modicon.ps1` makes `curiocatalogue/modicon.png` from `tools/modicon-source.png` (a pixelated in-game screenshot).
- `tools/lang-table.pl` prints all texts of `lang/en.json` and `lang/uk.json` as Markdown tables, side by side, for proofreading.
- For an AI agent: make multi-line or quote-heavy edits (backslashes, `\n` in JSON, apostrophes in Ukrainian text) with an
  editor tool, not perl or shell heredocs; those corrupted files several times.
- `ConvertFrom-Json` validates strict JSON (lang files, recipes). Item and block types are JSON5 (unquoted keys, comments): it
  rejects them, as it does vanilla's.

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
- Reviews: GitHub does not let anyone approve their own pull request, so the `main` ruleset requires 0 approvals while one
  person has write access; that person reviews before merging. With a second one, set 1 required approval in the `main` ruleset.
- CI (GitHub Actions) builds and runs both test projects on `feature/*`, `main`, `release` and on every PR (on the merge result).
  It downloads the game's server archive for the version in `modinfo.json` to get the DLLs, cached between runs.
- **Release:** `release` only moves forward to a commit of `main` (`git push origin main:release`, by the author). CI then builds,
  tests, makes the zip (CakeBuild), tags `vX.Y.Z` from `modinfo.json` (fails if the tag exists) and creates a GitHub Release
  with the zip; the release notes are that version's section of `CHANGELOG.md`. The author uploads the same zip and notes to the
  ModDB by hand (it has no release API).
- Release zip by hand: `build.ps1` (CakeBuild) → `Releases\`.
