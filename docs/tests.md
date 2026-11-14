# Tests

How the automated tests are written: the core tests, the tests against a real game server, the compatibility tests and the
golden stacks. Building and running them is in `docs/development.md` ("Build and test"); how the server inside the tests works,
in `docs/test-server.md`.

**Read** before writing or changing a test. **Update when** a test project, a fixture or a test convention changes.

# Writing tests

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

## Server tests

`CurioCatalogue.ServerTests` starts the real game server (no client) inside the test process: a superflat creative world in a
throwaway folder `%TEMP%\cc-servertests-…`, with the game's mods and ours. What the engine does there needs no source comment:
the engine ran it. How it works and its traps: `docs/test-server.md`.

- **One server per process**, shared by every test through `[Collection(ServerCollection.Name)]` (`GameServerFixture`); a second
  instance throws. `dotnet test` runs each test project in its own process, so the compatibility tests get a server of their own.
- **The fixture is shared, not copied:** `CurioCatalogue.TestServer/` holds the fixture, `GameAssemblies`, `FakeClient`, `TestBlocks`,
  `BookTestBase`, `GoldenStacks`, `LogProblems` and two props: `TestServer.Game.props` (the game's references, the Newtonsoft copy, `GameDir`, and those
  `.cs` files as links) and `TestServer.props` (it, plus xUnit, our mod's current build, `TestPathsAsMetadata`, and the two files that use our types or only test them: `BookTestBase`, `GoldenStacks`). Each test project imports
  `TestServer.props`, so each test assembly compiles its own copy, with its own module initializer and paths. `CurioCatalogue.GoldenCapture`
  imports only the game's side (without those two) and takes our mod from a release zip ("Golden stacks" below).
  Each project declares its own `[CollectionDefinition]`.
- Game code runs on the server's main thread: `game.OnMain(() => …)`, as in the game.
- It needs the game in `$(VINTAGE_STORY)` and builds the mod first. The paths (the game, the built mod, its assets) are written
  into the test assembly at build time from MSBuild (target `TestPathsAsMetadata`), not hard-coded.
- **Tests call our code directly** (`OurCodeTests`): the project references the mod and Core with `Private=false`, so neither is
  copied into the test bin; the game's mod loader loads them from the mod's output and the tests bind to that same copy.
  `The_tests_and_the_game_share_one_copy_of_our_types` fails if a second copy is ever loaded. Why: `docs/test-server.md`.
- **The server log is captured from the start** (`GameServerFixture.LogEntries()`, a subscription to `ServerMain.Logger.EntryAdded`
  made before the mod loader runs). `AssetLoadTests` fails on any Warning, Error or Fatal line logged until RunGame (`StartupLog()`, the
  same whatever test ran before), except the lines of `LogProblems.KnownNoise`
  (a vanilla line that is not ours goes there with its reason and game version). Server only: textures and shapes are read by the
  client, so a missing one is still found in game.
- **A real player without a client:** `FakeClient.Join(game, "name")` joins through a fake socket with the packets a client
  sends and returns when the player is playing (`IServerPlayer` with inventories, rights, a position); `SendToMod(request)` sends
  a message to our channel, handled by the server at a later tick (wait with `WaitFor`); `Received` holds what the server sent.
  The fixture sets `VerifyPlayerAuth = false` for it. `FakeClient` reads the ids of our messages from the server's channel,
  so `ModNetwork.Register` may change freely. How and its traps: `docs/test-server.md`, "A player without a client".
- **`RecipeTests`** compares the server's grid recipes of our domain with expected sets written out in the test (output, ingredient
  codes, counts, tools, shape, group): 1115 recipes under 8 names. A recipe change in `recipes/grid/ledger.json` is made in two places;
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
  containers, reach and stale windows. `TestBlocks` hands out the cells for all test classes and claims them (`WithClaim`).
  The `functional.md` cases it covers are marked with ⚙ there.
- **`BookEdgeTests`** (helpers shared with `BookFlowTests` in `BookTestBase`) takes the cases the first one left: a full album, blank lists (post and
  cancel), a 60-character name through the channel, and the clicks that are not ours (a plain click opens the chest, Shift on the floor
  lays the book on the ground). The config is changed on the running server by `WithConfig` and put back in `finally`; it also drops the mod's
  lazily built services (every private field of ours, found by reflection) because they take the config when first used; the config is
  restored from a JSON copy, so a setting added later is restored too. `WithConfig` is in `BookTestBase`.
  `Diagnostics` is tested through the flag of `ModLog` (internal, by reflection), not through the config.
  The branches the coverage review found untested (T9 of the server tests plan) are here too: an album started over, a want list into the empty
  main hand, refused, or on the ground, a list on the ground, a full ledger. A full inventory needs the player in survival (`AsInSurvival`):
  a creative player's inventory is never full (`docs/test-server.md`). `HandOut` (F2 of the fixes plan) is called directly: a stack that fits
  in part leaves only the rest on the ground, and a full creative inventory is never offered the stack.
- **`CraftingTests`** crafts through a real player's grid (C1–C4 of `functional.md`): stacks put into the grid's slots, the output taken with
  `ActivateSlot` as the server does with the client's packet; it checks the recipe found, the book made (cover, sheets, entries, name) and
  what is left in the grid. How: `docs/test-server.md`, "Crafting on the server".
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
  reflection), in the server's English, expected as `en.json` says (`BookTestBase.LangText`, as in `LanguageTests`). The client calls them,
  but they need no client; the key names fall back to "Shift" and "Ctrl".
- **`VanillaContentTests`**: the server twins of assumptions the adapter tests make about vanilla stacks (a tapestry taken off a wall, a hot ingot).
- **`CrystalsTests`**: our collection `crystals.json` on the game's real items (V2 of the vanilla collections plan): the members of
  each level (16, 118, 353; none of the 54 that never generate), the richest grade per ore and per ore and rock, an album of the
  richest taking a crystal of any rock and leaving a poorer one, a want list saying "any rock" and filled by another rock.
- **`UpgradeTests`** (the golden stacks, "Golden stacks" below): every stack of every set under `Golden/v<version>/` is read by `GoldenStacks` (the reader in
  `CurioCatalogue.TestServer`: the bytes into a stack, each numeric id replaced by the id its code has in the test world, in the stack, in nested stacks and in
  tree arrays) and read by the current code (`LedgerStore`), and what it reads must equal the set's **inputs** (mode, sheets, name, entries, the album's
  collection and progress, the invoice's kind and lines; never what the current code reads from the stack: that would test nothing) and be "not newer"
  than `LedgerStore.Version`. An album's inputs give what was collected; what is missing and the total follow the collection of the test world, as a player's
  album follows its collection, so a game that adds a tapestry does not turn a correct read red. The reader resolves only the stack itself, as a world does;
  the nested stacks are left for the current code to resolve. The same stacks read the same with every id moved out of the test world's range (the tapestry
  block has the same id in both worlds, so without the move a block id left unreplaced would pass); an invoice's origin and time of making read as the
  bytes hold them (not in the 0.1.1 inputs). The set's records refuse a field they do not know, so a new input of a later capture is never skipped.
  One action each on the 0.1.1 stacks, so that "read" also means "works": an intake list posted on a real chest into the 0.1.1 ledger, a pick list into the
  0.1.1 bill of materials, a want list filled
  from a chest, an album taking a new tapestry, a ledger with spent sheets recharged in the grid; an album of 0.1.1 over a collection that grew (the stored
  `members` tell a new piece from a collected one); the albums of 0.1.1 written back by the current code (format 2: every piece
  collected in 0.1.1 is there by its member id, none more). The config file the release wrote goes through the mod's loader (as `ConfigFileTests`): no problem in the
  log, every key it wrote is still a setting with its value (a renamed setting loses its key at the write-back), a setting the file lacks has its default,
  and every stack reads the same with that config. A new set under `Golden/` is picked up by the data-driven tests with no change; the actions name the 0.1.1 stacks.
- The server listens on a free port chosen at start, so a game or a server running on this machine does not get in the way.
- After a passing run the temp folder is deleted. If the server fails to start, the folder stays and the error names its
  `Logs\server-main.log`; the next run deletes such folders once they are 7 days old. With other mods, `Cache` stays (their DLLs are loaded from it and locked:
  `docs/test-server.md`); the next run of either project deletes folders that hold only `Cache`.

## Compatibility tests

`CurioCatalogue.CompatTests` runs the server of the server tests (`CompatServerFixture`, a `GameServerFixture` given the other mods) with
other mods loaded, so their effects never reach the server tests. Now: Upgradeable Storage (`UpgradeableStorageTests`: both mods load with a
clean log; its six kinds of storage are supported; every gesture of K1 on each; the off-hand gesture; a padlock), and our test mod
`cctapestry` (`ModdedTapestryTests`: a picture patched into the game's tapestry `sizes` is in the collection, and the album takes it),
and `cccollections` (`CollectionsFromAssetsTests`: a collection of ingots of its own domain with two levels, and a patch of it; an
album of it chooses its level through the channel).

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

## Golden stacks

Items made by a released version of the mod, kept in the repo, so the server tests can check that the current code still reads
what players have (plan `docs/plans/2026-11-04-backward-compat.md`). A set per release: `CurioCatalogue.ServerTests/Golden/v<version>/`.

- **What a set holds:** `stacks.json`: the game's and the mod's versions, the release zip's SHA-256, the date, and per stack its name,
  code, bytes (`ItemStack.ToBytes`, base64, with the numeric ids of the capture's world), the code of every id it holds (`"item:4505"`,
  `"block:2053"`; nested stacks and those in tree arrays too), its attributes as text for people (never parsed), and its **inputs**: what
  went into it (mode, sheets, name, entries; an album's collected pieces; an invoice's lines), the expected values of the golden tests.
  `curiocatalogue.json`: the config the release wrote on its first start, untouched. `.gitattributes` keeps the folder byte for byte.
- **The 0.1.1 set:** a ledger that learnt a chest, an intake list and a pick list it made, a bill of materials and the same book
  recoloured, a ledger aged with acid, one of 32 sheets with 3 spent, one renamed in Cyrillic, an album with 3 pieces and its want list,
  an album with every piece.
- **`CurioCatalogue.GoldenCapture`** makes a set: a console program that starts the test server with our mod of the **release zip**
  (the GitHub release's asset, downloaded by its build into `obj/release/` and checked by its hash), not the current build. It compiles
  against that zip's DLLs, so it uses only the release's types: it has its own helpers (`Scenario`), and `BookTestBase`, which uses
  the current build's types, is compiled only by `TestServer.props`. The items are made the way a player makes them (the grid, the
  gestures, the release's own network requests), and the run stops if the release does not read an item back as its inputs say.
- **Run it by hand**, once per release: `dotnet run --project CurioCatalogue.GoldenCapture` (about 40 s). It makes the items, lays
  them out as the fixture world of the manual upgrade pass (`Fixture.cs`; chests, a shelf, the ground; `fixture-layout.txt` in the
  data folder), and last writes the set of its `ReleaseVersion` (a run that fails earlier leaves no set). It refuses to write over a
  set that exists: run again with `--out <folder outside the repo>` to compare two runs or to get the world once more. Two runs
  differ only in `created` of the three invoices (the game's time when the list was made) and in the date. The run keeps its world
  in `%TEMP%\cc-golden-…` (the path is printed; the test runs' sweep of `cc-servertests-…` leaves it alone); copy it out to
  `VintageStory-Fixtures\<version>\` at once, as the backup of the fixture.
- **Its build** downloads the release zip into `obj/release/` and checks its hash; a zip that does not match is deleted, so the next
  build downloads it again. It shares the fixture's files named in `TestServer.Game.props` (they use the game's types only):
  after a change to one of them, `dotnet build CurioCatalogue.GoldenCapture` checks that the capture still compiles.
- **Why `dotnet test` and CI skip it:** it is in `curiocatalogue.sln` without a Build entry, so `dotnet build` and `dotnet test` of the
  solution never build it: its build downloads the release, and a set is made once, not on every run. The sets it made are read by
  the server tests (`UpgradeTests`), which CI runs.

**At each release** (the process for every later plan):

- **A new set per release.** After the release zip is on GitHub, run the capture with that zip (`ReleaseVersion` and the zip's hash set for it;
  its helpers rewritten against the release's types), which writes `Golden/v0.2.0/` next to `v0.1.1/`. `UpgradeTests` picks the new set up by itself;
  its actions are added for the stacks the release changed.
- **Old sets are never re-captured or edited.** They are what players have; a set made again by newer code would test nothing. If an old set
  turns red, the code is wrong, or the change goes to `CHANGELOG.md` as something that does not survive (and the test then checks that outcome).
  The reader fails on a code the test world lacks, and the config test on a setting that lost its key: a release that removes or renames an
  item code or a setting decides then how the reader maps it (a world shows a removed item as "unknown") and keeps a table of renamed keys.
- **A change of the stored format raises `LedgerStore.Version` and keeps every old set green:** the new code reads `v` 1 (and every older
  version) and converts; a book of a newer version is refused, not wiped (`RefusalTests`). Whoever changes stored data, recipes, item codes,
  config or lang adds the line to the upgrade plan's table "Changes in … that need a case" in the same PR (`docs/testing/upgrade-*.md`).
