# Structural decisions

The decisions behind the structure of the code (Part 1 of `docs/architecture.md`): what was chosen, the alternatives, the
reason and who decided ("author" = the mod's author; otherwise proposed by the AI assistant and accepted by the author).
Dates are 2026.

**Read** before adding a class, an adapter, a network message or a new kind of state: the section it belongs to. **Update**
when a structural decision is made. A decision that belongs to one feature goes to the "Decisions" section of its plan.

## Code

- **A separate core project without game references** (10-26). Like a separate Gradle/sbt module: the compiler stops the core from
  depending on the game. The core tests build anywhere; CI runs them first, before it downloads the game.
- **Our own attribute model and canonical identity** (10-26). Alternative: compare with the game's `ItemStack.Equals(world, …)`;
  it needs an `IWorldAccessor` and cannot be tested without the engine. Details: numeric types stay distinct as in the game
  (int 5 ≠ long 5); an empty tree equals no attributes; unknown attribute types (other mods) go in through their JSON form; a stack
  whose item is gone keys as `#<id>` and does not crash. Ignored paths are full paths (`tool/durability`), unlike vanilla's nested-tree check (a vanilla bug).
- **Immutable values, refusals as values** (10-26). Fits all-or-nothing: if something fails, the new state is simply not written
  into the item. Like `Either` / a sealed trait in Scala. Refusal order: "book full" → "nothing to list" → "no sheets", so the
  player hears about sheets only when an invoice could really be made.
- **Plan in the core, check everything, then execute** (10-26). `Transfer.Plan` decides where to take and where to put without game
  types; `InvoicePoster` checks the whole plan before the first move.
- **A functional core, as far as it reads well** (10-28, author). Functional core, imperative shell: the game's API is mutable
  and object-oriented (stacks, attributes and slots change in place, behaviour comes from overriding virtual methods), so only
  the core can be functional, and there it pays: all-or-nothing comes from planning before changing anything, and the core's tests
  need no mocks. The limit is readability: mutation local to a function (filling a `List` inside `Transfer.Plan`, like a
  `ListBuffer` in a Scala method) stays, rather than being folded with `Aggregate`. One step further is in `ROADMAP.md`
  (Technical): books as a sum type.
- **Exhaustive switch through an analyzer, with our own types** (10-31, author). The analyzer `ExhaustiveMatching.Analyzer`
  makes a missed case in a `switch` over a `[Closed]` record hierarchy or an enum a build error (like a sealed trait's match in
  Scala). It is a build-time dependency only: the attribute and the exception it looks for by full name are our own
  (`CurioCatalogue.Core/ExhaustiveMatching.cs`), written from the public API alone. Rejected: shipping the package's dll in the
  zip (S5's first choice). All mods share one process, and the game resolves a dependency by its file name from the first mod
  folder that has it (`docs/modding.md`, Other mods), so a library another mod ships in another version could stop a mod from
  loading. The rule: the mod ships no third-party dll.
- **Services created lazily** in the mod system (on first use, not in `Start`), because the world may not be ready in `Start`.

## State

- **Data in item attributes** (10-26). The book travels like any item; nothing to sync or clean up in the world.
- **Mode and invoice kind are the item's variant, not attributes** (10-26): two sources of truth could disagree.
- **Samples stored as `ItemstackAttribute`**, not raw bytes (10-26): vanilla stores bag contents this way and resolves the item by id on
  load. `Entry.Sample` in the core is therefore an opaque `object`.
- **Keys recomputed on read, not stored** (10-26): a change of ignored attributes in the config applies to existing books; an item
  from a removed mod is kept. The same book always serializes to the same bytes (entries sorted).
- **The album's snapshot as sample stacks** (10-28, code review; format 1, replaced by format 2 in 0.2, below in Collections):
  re-keyed on read like the entries, so the two always agree. Kept as key strings, any change of a key's text (our format, the
  config's ignored attributes, a new attribute on the item) reset every album to nothing collected, silently. The same reason keeps
  the stacks of format 2 as stacks: the migration of format 1 re-keys the snapshot, and a collected member's stack is re-keyed too.
- **Format version `v` at the root of every book and invoice** (10-28, code review): missing means 1. Data of a newer version (after
  a downgrade of the mod) is read as well as we can and never written: every server action on it, and our recipes, refuse
  (`LedgerStore.IsNewer`, `PlayerMessages.RefuseNewer`). Alternative: read and write as is, which silently drops what the older
  build does not know.
- **modid renamed before the release** (`warehouseledger` → `curiocatalogue`, 10-26, author): after a release a modid cannot change
  without breaking worlds.

## Collections

The shape of a collection (11-07, proposed by the AI assistant and accepted by the author; from the designs of tapestries, ore
crystals, seashells and butterflies, plan `2026-11-04-collections-design.md`, and the album view, plan `2026-11-04-album-view.md`,
A2). Being built by plan `2026-11-07-collections-core.md` (definitions, the registry, the album of format 2 and want lists by
members, the resolver, the packet and the recipe of a disabled collection are in).

- **A definition is data; the members come from a source in code.** A collection is defined in an asset
  `config/curiocatalogue/collections/<id>.json`, read from every domain, so another mod brings a collection without our release
  and an admin or a mod changes one with a JSON patch (C5, option D). The members are built by a **source**, one of a small closed
  set of kinds written in C# and parameterised by the definition: `variants` (the registered items or blocks of one code with
  variant groups, `game:crystalizedore-{grade}-{ore}-{rock}`: members follow the loaded game and mods, an item added by an update
  or a mod comes in by itself) and `tapestry` (blocks with a `sizes` attribute: picture, size, the piece's place x, y). Later
  kinds: the book itself (custom collections), a rule on an attribute (berry bush traits). Everything else is data: dimensions,
  levels, layouts, attribute rules, exclusions. Rejected: every collection in C# (no collection from another mod, no patch by an
  admin); every member listed in data (353 crystals by hand, and the list does not follow mods).
- **The JSON of a definition** (K3, 11-07; read by `CollectionDefinitionReader` into the core's records; `tapestries.json` is the
  example; the id is the file's name): `source` (`{"kind": "variants", "code": "game:crystalizedore-{grade}-{ore}-{rock}"}`, each
  `{name}` a dimension taking the text between dashes, the point's name the code without domain; or `{"kind": "tapestry", "block":
  "game:tapestry-north"}`), `dimensions` (`code`, `values`, `nameKey`, `icon`, `anyKey`: a phrase in lower case for what a member of several items leaves
free, "any colour"; `inLists`: `true` to say the member's value by its `nameKey` after the item's name in the want list sheet's lines, when the name does not say it, a crystal's rock; `shortNameKey`: a shorter name of a value for a table heading with little room, the full name stays the heading's tooltip),
`levels` (`id`, `keep`, `drop`: dimension → `"any"` /
  `"max"`, `nameKey`, `progressKey`: the whole line of an album's progress at the level, `{0}` collected, `{1}` of all, `{2}` groups
  complete, `{3}` groups, in each language's grammar; `layouts`: `id`, `groupBy`, `arrange`: `{"kind": "table", "rows", "columns"}` / `{"kind": "shape"}` / `{"kind": "order"}`),
  `rules` (`where`, `set`), `exclude`, `nameKey`: the collection's name in lower case, "tapestries", for "Album: {0}",
  `albumRecipe`: the grid recipe that makes its album, by name with the domain (`curiocatalogue:ledger-album`). A definition that
  does not read, or that the core refuses (`DefinedCollection.CheckDefinition`, also called by the reader: e.g. a level dropping
  as `max` a dimension with no `values`, an empty level id), is logged as an error and skipped; the other collections work. Of
  the good definitions of one id the first is used, ours (`curiocatalogue`) before other domains, then the game's asset order; the
  others are logged as errors (K7, 11-07: a broken copy must not shadow a good one, and the asset order is not ours to choose).
  Each collection is built inside a guard that logs whatever it throws and skips it, and each problem is logged once per session
  (`ProblemLog`), however often the registry is rebuilt. A config id no definition has, and `Add` on a `variants` collection,
  are warnings.
  The registry builds a collection on first use, and none when its source finds no point (no tapestry block, no such variants:
  a mod's collection without the mod); `Defined(id)` gives it. The config's `Exclude` joins the
  definition's own, so it also leaves out an `Add`ed piece it matches (0.1 did not); `Add` is for the `tapestry` source (a piece,
  its own picture, place 1, 1) and ignored by `variants`.
- **A member** is a point in the collection's dimensions (dimension → value), with a stable id made of its values (a tapestry
  piece's id is its name), what fills it (a set of item keys: one code, or any of several), the stacks to show (one, or a list
  the album cycles through) and attributes (rarity, a piece's x and y). Within a level a key fills at most one member, checked
  when the collection is built. A rule on an attribute is a further kind of filler, reserved.
- **Levels are projections of the full set.** A level keeps some dimensions; for each dimension it drops it says `any` (any value
  fills the member) or `max` (only the highest value that occurs, in the dimension's order): crystals keep `ore` with grade
  `max` (16), `ore` and `rock` with grade `max` (118), all three (353); seashells keep `type` (7, colour `any`) or both (42). A
  collection with one level shows no choice.
- **Dimensions** have their values in order, a list in the definition (K1, 11-07: the game keeps no order of a variant group at
  run time, and item ids follow the world's history; a mod adds its value to the list with a patch); a value the source finds
  but the list lacks comes after the listed ones, by code, and for `max` ranks below all of them, with one warning in the log.
  Rejected: the server reading the order from the item's asset and sending it (more code, tied to the shape of others' assets);
  the order of item ids (differs between worlds). A name per value as a lang key (the game's where
  it has one: `game:ore-{value}`, `game:rock-{value}`) and optionally an icon stack. A hierarchical dimension (a tapestry's size
  belongs to its picture) is one the source derives from another. **Layouts** belong to a level: which dimension groups (or none)
  and how a group is arranged: a table of two dimensions, the shape from the members' x and y, or in order; one is the default.
  Groups and order are computed, never stored; taking new follows the default layout's order (whole groups first, as pictures today).
- **Attributes by rules in the definition** (`where` some dimension values, `set` attributes): the rarity of seashell colours, and
  any later per-member property. The core carries them; what the album shows of them is decided per collection.
- **The config's `Collections.<id>`** (`Exclude`, `Add`, `Disabled`; C5) works on each source's own name of a member: the item
  code without domain for `variants` (`crystalizedore-*-uranium-*`), the piece name for `tapestry`, so the old `TapestryExclude`
  and `TapestryAdd` move into `Collections.tapestries` unchanged. The id is the one albums already store, `tapestries`.
  Combinations that never occur (54 crystals) are the definition's own default `exclude`; computing them from the loaded deposits
  was left for later.
- **An album stores what it collected, not what it misses** (format 2): its level (or none chosen) and, per collected member, the
  member's id and the stack that filled it. What is missing is the collection minus that. So the album remembers the code that
  filled a member of several codes (A2), needs no snapshot and no refresh on read (`RefreshIfStale` goes), and keeps the records of
  a member excluded or disabled for a while: it comes back collected. The data is about half of today's. The album becomes a core
  type of its own rather than a `Ledger` in album mode (a step towards books as a sum type, `ROADMAP.md`, Technical); format 1 is
  migrated on read (snapshot keys → member ids by the collection's index), and 0.1.1 refuses format 2 as any newer data. Rejected:
  keep the missing entries and the snapshot, with `Entry.Pattern` for members of several codes (fewer changes, but the code that
  filled a member would be a third list beside them). Built in K4 (11-07): what an album of format 1 collected but its collection
  lacks at the time of reading (unknown, disabled, a piece excluded) is kept as `unplaced` stacks and placed when the collection has
  them again, so no write loses a piece (the plan's Decisions, K4).
- **A want list's line names a member** (collection, level, member id), and taking by a want list goes the album's way, by members
  rather than keys, so a member of several codes is filled by any of them. Want lists of format 1 are read as exact entries, as now.
  Built in K5 (11-07): the list stores its album's collection and level once (`Invoice.From`), each line its member id
  (`Entry.Member`) beside the member's first item; a line whose member or collection the game lacks wants exactly that item.
- **One resolver** for the store, the items and the network: given an album (its id now, the book stack later for custom
  collections), it answers `Found | Disabled | Unknown`, a closed type, so "disabled on this server" and "no longer in the game"
  are different refusals. On it, one ladder for an album (V1 of the vanilla collections plan, 09.11.2026): `AlbumStanding` in
  the core says "at a level" or refused with an `AlbumRefusal` (`Disabled`, `NoCollection`, `NoLevel`); taking new, the want
  list, starting over, choosing a level, the tooltip and the window all ask it, and the refusal names the same message everywhere.
  Rejected: an `AlbumState` per use and the same three values in each result's enum (as built in K3–K6), which drifted apart.
- **The item for a member of several is chosen with the room** (V1, 09.11.2026; A5 of K7): `Album.Wanted` and `Invoice.Wanted`
  list every item the container has for a line, in the member's order; `Transfer.PlanWhatFits` takes the first that no earlier line
  took and that fits. Rejected: choosing the first item before room is known (an item that does not fit kept out one that would).
- **The server's `Collections` reach the client in a packet on join;** the client's registry is rebuilt when it arrives. Without
  it the client builds albums with its own config file (already true of `TapestryExclude` in 0.1) and cannot show `Disabled`.
  The handler only stores the section and drops the registry and the client's item keys at once (so nothing is read with the old
  ones); it is rebuilt at the client's `LevelFinalize`, which comes after it (K1:
  when the message arrives the grid recipes may not be loaded yet; `docs/modding.md`, Network). The definitions themselves the
  client reads from its own assets (K1, 11-07, the author): a mod that brings a collection must be universal and required on the
  client, and a patch of a definition must not be `"side": "server"`, or the client does not know the collection (to say in the
  README's part for modders). Rejected: sending the server's definitions in the packet too (one source, but more code for a
  mistake the modder can avoid).
  **The album recipe is named by the definition** (`albumRecipe`; K7, 11-07, replacing a table in C#), so a mod brings a collection
  with its album by JSON alone: its definition and a grid recipe of an empty book and any member (which stays). The registry maps
  recipes to collections from every good definition, switched-off ones included (`AlbumOf`; a recipe two definitions name is the
  first's, logged); the book asks it whether a recipe makes an album and of which collection, and the sample must be a member of
  that collection, or there is no output. The recipe of a disabled collection is removed from the server's recipes at load
  (K1: in `AssetsFinalize` or `StartServerSide`, out of `GridRecipes` and `Enabled = false`; `docs/modding.md`, Recipes), so
  clients never get it. `AlbumRecipes` reads the definitions itself there, choosing as the registry does, but builds no
  collection (that needs the world's blocks); it takes out recipes of any domain.

## Client and server

- **One network channel**, every request re-checked; a forged or stale request is ignored with a diagnostic line.
- **Item names in "missing" are made by the client** (10-27), the case that led to principle 2. The server sends the refusal and the
  stacks; the client names them in its language. Rejected: translating on the server by lang key (wrong for names built from
  attributes, e.g. tapestries); leaving the server's language. Our own items are named on the server with `Lang.GetL(player.LanguageCode, …)`.
- **Windows read the item, actions are requests** (10-26): the client shows the item's attributes, which the game syncs; rename,
  clear and cancel go to the server, which checks the item is still in the same hand. The windows check their item every 0.5 s.

## Diagnostics and tests

- **Diagnostics behind a config flag that also skips gathering the data** (10-26, author): a custom interpolated string handler
  (like a by-name parameter in Scala, without allocation) plus `if (ModLog.DebugEnabled)` blocks. Server lines start with the player's name.
- **Mocks as documented assumptions** (10-26, author: "наочна декларація якої поведінки ми очікуємо від рушія. документація кодом наших
  assumptions"). The first proposal was to test only the core and real data types and leave glue to in-game checks. Kept from it: the
  risk that a mock captures a wrong understanding, closed by a source on every mock. **NSubstitute rather than Moq**: Mockito-like
  syntax, and Moq's 2023 SponsorLink episode.
- **Harmony in tests is closer to a mock than to a patch** (10-28, author): like PowerMock or `mockito-inline` for final and static
  methods. It never reaches players, so the conflict risk is gone, but it still encodes our understanding of the engine: it needs a
  source comment like any mock. Try a small wrapper interface of our own first; it is cheaper and more explicit.
- **A real game server inside the test process** (11-02, plan `2026-11-02-server-tests.md`): a third test project starts the
  dedicated server on a thread of the test process, in a throwaway superflat world, and the tests use `ICoreServerAPI` directly
  on its main thread. One server per process (the engine keeps state in statics), shared by all tests of the project. Rejected:
  `VintagestoryServer.exe` as a separate process (it started, but stopping it through stdin was unreliable, and the tests could
  talk to it only through the log or a test mod); a client as well as the server (tests stay headless, what needs a client stays
  in game checks).
- **Compatibility tests in a project of their own** (11-02, author, plan `2026-11-02-server-tests.md`, T12): the same fixture, a
  server with other mods loaded, so their effects never reach the server tests. Other mods are not in the repo (their licences):
  a pinned list (mod id, version, file URL, SHA-256) is, and the zips come from a folder or a download into it, the same locally
  and on CI; a missing or changed zip fails the run. Rejected: other mods in the server tests' own server; skipping when a mod is
  missing. **The fixture is shared as source files** (11-02, author, T12): `CurioCatalogue.TestServer/` has no project; its `.cs` files
  and `TestServer.props` are imported by each test project, so each test assembly has its own module initializer, which must run
  before any game type loads. Rejected: a class library referenced by both (its module initializer runs only when one of its types is
  first touched, possibly after the test assembly has loaded a game type; internal types would need `InternalsVisibleTo`).
- **Golden stacks made by the release's own code** (11-04, author, plan `2026-11-04-backward-compat.md`): `CurioCatalogue.GoldenCapture`,
  a console program, starts the same fixture with our mod of a release zip (the GitHub asset, checked by its hash) instead of the
  current build, and compiles against that zip's DLLs. So the fixture is split in two props: `TestServer.Game.props` (the game's
  references and the fixture files that use only the game's types, named one by one) is what the capture imports;
  `TestServer.props` adds xUnit, the current build and the files that use our types (`BookTestBase`, the reader `GoldenStacks`).
  The capture is in the solution without a Build entry, so neither `dotnet build`, `dotnet test` nor CI builds it or downloads a
  release; it is run by hand once per release, and its code is frozen with its set. The sets (bytes plus the code of every id,
  and the inputs as expected values) are in the repo; the world it lays out is kept outside. Rejected: a dump tool run in game
  (not reproducible); a WorldEdit schematic of a chest (the engine remaps nested ids in trees, not in the arrays our entries are
  in); stacks written by the current code (they would change with it); sharing `BookTestBase` with the capture (it uses the
  current build's types).
