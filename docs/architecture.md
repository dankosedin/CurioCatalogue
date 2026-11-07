# Architecture

How the mod is built and why. Terms are in `docs/domain.md`; game behaviour behind the decisions is in `docs/modding.md`.

Two parts. **Part 1** is the structure of the code: principles, layers, flows and the decisions behind them; it holds for any
feature. **Part 2** is how features were fitted into what the game allows and what the author wants the mod to be.

**Read** before adding a class, an adapter, a network message or a new kind of state. **Update** when a decision is made: write
what was chosen, the alternatives and the reason, and who decided ("author" = the mod's author; otherwise proposed by the AI
assistant and accepted by the author). Structural decisions go to Part 1. A decision that belongs to one feature goes to the
"Decisions" section of that feature's plan in `docs/plans/`; Part 2 keeps the decisions made before plans existed (the MVP) and
those that outlive their feature. Dates are 2026.

## Part 1. Structure

### Principles

1. **The server decides, all or nothing.** Every change of items happens on the server and either completes or changes nothing.
   The client asks; every request is re-checked on the server.
2. **The server decides, the client presents.** The server sends what happened: codes, numbers, item stacks. The client turns them
   into text in the player's language, with the player's key bindings and the game's own item names; the server cannot (it names
   items in the server's language). Server-side translation (`Lang.GetL(player.LanguageCode, …)`) is a shortcut allowed only for our
   own texts and our own item names; anything the game names goes to the client (`MissingItemsRefusal`). Learned on a dedicated server, 10-27.
3. **The mod gives no more access than hands do.** Claims, padlocks and reinforcement are checked by us (the item takes the click before the block).
4. **Logic lives in a pure core, written in a functional style.** Set and multiset operations, identity and transfer planning know
   nothing about the game and are unit-tested. Values are immutable, operations return new values, outcomes are values. The style
   holds as long as it keeps the code readable and the tests simple; the game glue stays imperative (the game's API is).
5. **Thin game glue.** Adapters translate between game types and the core; game classes that cannot be mocked go behind small interfaces.
6. **Harmony is the last resort.** Use the API's own extension points first, even when that means a workaround. Runtime patches of
   the game (like aspects woven into a JVM program) conflict with other mods' patches and break silently on game updates. A patch
   needs a decision entry below (what was tried, why it did not work), prefers a Postfix over a Prefix, never uses a Transpiler without
   the author's agreement, and cites the patched method (`File.cs:line#Method`) so `tools/check-refs.ps1` notices when it moves.
7. **State lives in the items.** Books and invoices carry their data in item attributes; there is no world-level storage.

### Layers

```
CurioCatalogue.Core           pure C#: no game references (separate project, so the compiler enforces it)
    ↑
curiocatalogue/Adapters       game types <-> core: stacks, containers, access, storage in attributes, invoices
    ↑
curiocatalogue/ (glue)        items, gestures, hints, windows, network, messages
    ↑
CurioCatalogueModSystem       composition root: config, lazily created services, registration
```

#### Core (`CurioCatalogue.Core`)

- **Identity:** `AttrValue` (a small attribute model: typed numbers, strings, bytes, arrays, trees), `AttrCanonical` (a canonical,
  locale-independent, key-sorted string of attributes minus the ignored ones), `ItemKey` (kind + code + canonical attributes), `ItemIdentity`.
- **Books:** `Book` (sheets, name, `ReturnSheet`, `AddSheets`), closed: `Ledger` (types or counts: entries, with
  `CreateInvoice`, `Post`, `Clear`, `FromStored`) or `Album` (collection, level, collected members, with `Missing`, `Wanted`,
  `Collect`, `ChooseLevel`, `StartOver`, `MakeWantList`, `FromFormat1`); `Entry` (key, count, opaque sample, kind `Exact`;
  `Pattern` reserved; on a want list the member it names); `Invoice` (a want list's `From` and `Wanted`, `CrossOff`); `Multiset`.
  All immutable; operations return a new value. Refusals are values (`CreateInvoiceResult.Created | Refused`, `LevelChoice`), not
  exceptions.
  Limits (`LedgerLimits`) come from the config, not from the book.
- **Moving items:** `Transfer.Plan` (all or nothing: `Ready | Missing | NoRoom`) and `Transfer.PlanWhatFits` (partial, in a given order),
  over plain `SourceSlot` / `TargetSlot` descriptions, with merge classes (`Stack`) besides keys.
- **How an invoice ends:** `InvoiceFates` (`Ended`, `WantListPosted`): the one table of sheet returned / parchment / consumed / kept,
  for posting and cancelling alike.
- **Collections:** `CollectionDefinition` (dimensions, levels, layouts, rules, exclusions; `SourcePoint`s from the adapters) and
  `DefinedCollection.Build` (`Built | Invalid`): per level the members (`Member`: id, values, fillers, stacks, attributes), the
  member a key fills, groups and order per layout, `LevelProgress`; `TapestryPieces` (pieces from `sizes`); adapters
  `CollectionRegistry`, `CollectionDefinitionReader` (plan `2026-11-07-collections-core.md`).

#### Adapters (`curiocatalogue/Adapters`)

- `StackKeys`: `ItemStack` → `ItemKey` (resolves stacks read from bytes first; ignored attributes from the config).
- `LedgerStore`: books and invoices ↔ item attributes under `curiocatalogue`; reads an album of format 1 as format 2 (the migration).
- `ContainerReader`: classifies the aimed block (supported storage / other inventory / not ours), resolves multiblocks, reads slots,
  opens writable slots (bags on the ground through a wrapper inventory).
- `ContainerAccess`: claims and reinforcement, behind interfaces for tests.
- `InvoiceMaker`: compares a book with a container and makes an invoice.
- `InvoicePoster`: `Post` (an invoice) and `Collect` (album, want list): builds the plan, checks all of it, then moves with `TryPutInto`
  and updates the off-hand book and the sheet.
- `CollectionRegistry`: the collections known in this world, from the definitions of every domain and the config.

#### Glue

- `Items/ItemLedger`, `Items/ItemInvoice`: the item classes; click handling (`OnHeldInteractStart`), tooltips, crafting hooks.
- `EmptyHandGesture`: the off-hand gesture, caught on the client in `Input.InWorldAction`.
- `LedgerHintsBehavior`: hints over containers, added to storage blocks on the client.
- `Gui/GridDialog` (base), `Gui/LedgerDialog`, `Gui/InvoiceDialog`, `Gui/ReadOnlySlot`: client-only read-only windows.
- `ModNetwork`: one channel. Client → server: `MakeInvoiceRequest`, `RenameLedgerRequest`, `ClearLedgerRequest`, `CancelInvoiceRequest`,
  `MakeWantListRequest`, `ChooseLevelRequest`. Server → client: `MissingItemsRefusal` (the client names the items in its own language).
- `PlayerMessages` (refusals as red in-game errors, results in chat), `ModLog` (diagnostics), `HandbookIndexFix`.

### Main flows

- **Make an invoice (book in hand):** `ItemLedger.OnHeldInteractStart` (both sides classify the block the same; the server acts) →
  `InvoiceMaker` (`ContainerReader`, `ContainerAccess`, `LedgerStore`, core `Ledger.CreateInvoice`) → `PlayerMessages`.
- **Make an invoice (empty hand, book in the off-hand):** client `EmptyHandGesture` (swallows the click with `PreventDefault`,
  guards against the held-button repeats) → `MakeInvoiceRequest` → server re-checks hands, reach, access → `ItemLedger.MakeInvoice`;
  the invoice goes straight into the empty hand.
- **Post an invoice:** `ItemInvoice.OnHeldInteractStart` → `InvoicePoster.Post`: plan (`Transfer.Plan`) → check all → move →
  update the off-hand book (`Ledger.Post`) → return the sheet or turn the invoice into parchment.
- **Album, want list:** the same entry points → `InvoicePoster.Collect` with `Transfer.PlanWhatFits` (as much as fits, in the
  collection's order; exactly what moved is crossed off).
- **Window actions:** the client reads the item's attributes directly (the game syncs them); an action is a request; the server
  checks the item is still in the same hand, then changes it.

### Structural decisions

#### Code

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

#### State

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

#### Collections

The shape of a collection (11-07, proposed by the AI assistant and accepted by the author; from the designs of tapestries, ore
crystals, seashells and butterflies, plan `2026-11-04-collections-design.md`, and the album view, plan `2026-11-04-album-view.md`,
A2). Being built by plan `2026-11-07-collections-core.md` (definitions, the registry, the album of format 2 and want lists by
members are in; the resolver and the packet are its K6).

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
free, "any colour"), `levels` (`id`, `keep`, `drop`: dimension → `"any"` /
  `"max"`, `layouts`: `id`, `groupBy`, `arrange`: `{"kind": "table", "rows", "columns"}` / `{"kind": "shape"}` / `{"kind": "order"}`),
  `rules` (`where`, `set`), `exclude`, `nameKey`. A definition that does not read, or that the core refuses, is logged as an error
  and skipped; the other collections work. A second definition with an id already seen (another domain) is ignored, with an error.
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
  are different refusals.
- **The server's `Collections` reach the client in a packet on join;** the client's registry is rebuilt when it arrives. Without
  it the client builds albums with its own config file (already true of `TapestryExclude` in 0.1) and cannot show `Disabled`.
  The handler only stores the section; the registry is rebuilt at the client's `LevelFinalize`, which comes after it (K1:
  when the message arrives the grid recipes may not be loaded yet; `docs/modding.md`, Network). The definitions themselves the
  client reads from its own assets (K1, 11-07, the author): a mod that brings a collection must be universal and required on the
  client, and a patch of a definition must not be `"side": "server"`, or the client does not know the collection (to say in the
  README's part for modders). Rejected: sending the server's definitions in the packet too (one source, but more code for a
  mistake the modder can avoid).
  The recipe of a disabled collection is removed from the server's recipes at load (K1: in `AssetsFinalize` or `StartServerSide`,
  out of `GridRecipes` and `Enabled = false`, found by its file's `Name`; `docs/modding.md`, Recipes), so clients never get it;
  else it makes nothing. Each vanilla collection has its album recipe in our assets (an empty book and any
  member, which stays); another mod's collection brings its own, and the collection is found by the sample.

#### Client and server

- **One network channel**, every request re-checked; a forged or stale request is ignored with a diagnostic line.
- **Item names in "missing" are made by the client** (10-27), the case that led to principle 2. The server sends the refusal and the
  stacks; the client names them in its language. Rejected: translating on the server by lang key (wrong for names built from
  attributes, e.g. tapestries); leaving the server's language. Our own items are named on the server with `Lang.GetL(player.LanguageCode, …)`.
- **Windows read the item, actions are requests** (10-26): the client shows the item's attributes, which the game syncs; rename,
  clear and cancel go to the server, which checks the item is still in the same hand. The windows check their item every 0.5 s.

#### Diagnostics and tests

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

## Part 2. Fitting features into the game

### Clicks and gestures

- **Shift + RMB, with the item taking the click first** (`HeldPriorityInteract`). Plain RMB must keep opening containers.
- **Three kinds of block** (10-26): supported storage → ours; another inventory → "not supported yet"; anything else → the game's.
  A whitelist of storage classes (`BlockEntityGenericTypedContainer` and descendants, `BlockEntityCrate`), not "anything with an
  inventory": otherwise posting would pull items out of ovens. Taking Shift + RMB everywhere also broke putting the book on the
  ground (`GroundStorable`). "Not a container" is answered before the access check, since it reveals nothing.
  Accepted trade-offs: the book cannot be put down in a cell that holds a bag on the ground (author); our items cannot go into a
  vanilla crate by hand ("one gesture on all containers matters more").
- **Empty-hand gesture via client input** (10-26, the author's idea after forgetting to swap hands). Rejected: a block behavior (chests
  run vanilla's `Container` behavior first and stop; crates and ground storage go straight to the block entity), Harmony patches.
  Chosen: catch `InWorldRightMouseDown` on the client, send a request, re-check everything on the server. The old gesture stays (author).
- **Hints over containers only while holding a book** (10-26, author). Rejected: always (noise over every chest for everyone), only at
  the bottom of the screen. At the bottom only "RMB: open" (10-27): the off-hand never shows item hints, and a custom HUD element for one hint was too much.
- **Key names in tooltips from the player's bindings** (10-27), matching vanilla even where vanilla is wrong (see `docs/modding.md`).

### Posting

- **Partial stacks first** (10-26, author chose this over plain slot order): whole stacks stay in the chest.
- **Real stacks move**, with their wear and temperature, not copies of samples.
- **The plan follows the game's merge rule; what the game did not move is never recorded** (10-28, code review, author: the hybrid).
  `Transfer` sees merge classes (`SourceSlot.Stack`) besides keys. If the game still moves less than planned (a bug), an invoice,
  being all or nothing, puts every touched slot back from copies taken before and refuses (`movefailed`); a want list and an album,
  which take what fits anyway, cross off only what really moved. Alternatives: record what moved for the invoice too (that is the
  ROADMAP's partial posting, a feature with its own questions: the rest of the lines, the sheet); trust the plan (the old way: the book
  and the album could record items left in the chest). When invoices post partially, they move to the want list's path.
- **Per-player loot chests are unsupported** for making and posting: their slots show the total, not the player's share.
- **Bags on the ground are writable** through a wrapper inventory (the fallback "read-only for bags" was not needed).
- **Album in the off-hand: posting an ordinary invoice returns the sheet, the entries stay** (28.10.2026, author: less inventory
  juggling; was "ignored", 10-26). The album changes only by taking new items. Cancelling any invoice returns its sheet to an album
  too (author, 10-26). A want list returns no sheet to any book; under `ConsumeSheets` a finished one disappears like any invoice.

### Album and want list

- **The album has its own mode and takes items directly, without an invoice** (10-26, author). First the design had an outgoing
  invoice with the album in the off-hand; then the items had to be put back. Also proposed and dropped: a gesture that crosses off
  without moving (author: "can be skipped"), marking tapestries on walls, a copy of the album as co-op. An invoice exists to show a
  delivery before posting; the album always takes the same thing. A types book's outgoing invoice takes *all* copies, while the cycle
  needs "duplicates stay", hence the separate mode taking one of each. No undo (accepted): access checks still apply.
- **Partial take, in the collection's order** (10-26): picture by picture, so whole pictures land in the inventory; what moved is
  exactly what is crossed off.
- **Want list instead of a copy of the album** (10-26, author: "не хочеться щоб це була книга"; costs sheets, which gives a reason to
  refill them). Made by a button in the album window rather than a recipe (no need to get the album back from the grid). It does not
  touch the holder's own off-hand book: it is meant for another player.
- **"Clear" on an album means "Start over"** (author): one button must not "collect" the whole collection.
- **The album follows its collection by itself** (10-26). Alternatives: a refresh recipe (tried; the player had to remember to
  re-craft after a new mod or config change), showing stale entries separately, or waiting for playtests. The author, after trying
  the recipe: "щось перемудрили". Until 0.2 by a refresh on every read (`RefreshIfStale`); since format 2 (K4) with no refresh:
  the album stores what it collected, and what is missing is computed from the collection.

### Messages and texts

- **Refusals as red in-game errors, results in chat** (10-26): short refusals disappear; results stay in the chat history. May change after playtests.
- **One mod name in all languages** (10-26): `modinfo.json` `name` is not localizable; overwriting `Info.Name` on the client was rejected
  as meddling with game data for cosmetics, a bilingual name as too long. The Ukrainian name appears where we control the text.
- **Behaviour as the explanation** (author): texts do not say what is obviously kept (name, sheets); no design jargon ("fallback" is
  just "the recipe" for players; "Add sheets", not "recharge").

### Assets

- **Cover colour as a variant, not an attribute** (10-26, author). For the server the cost is the same; a variant registers ~70 items
  instead of 3, but shelves, bookcases and the ground draw it by item code, while an attribute would need our own meshes and a mesh
  cache. Risk: the colour list must stay stable. Revisit if the book gets its own model (see `ROADMAP.md`).
- **Whole PNG textures from our script, our own copy of the book shape** (10-27). Cause: textures with `overlays` have no file of their
  own, and Better Crates broke the chunk mesh on them. Rejected: baked PNGs with vanilla covers inside (copies vanilla images, goes stale),
  inserting our composite textures into the block atlas (fixes one lookup path of one mod, depends on its internals).
  Fixing on our side because players would blame the mod they just added. The generator uses a fixed noise seed, so PNGs are reproducible.
- **Mod icon: a pixelated in-game screenshot** (10-27, author), reproducible by `tools/modicon.ps1` from the committed source image.

### Compatibility

- **Better Crates support deferred** (10-27, author: it breaks nothing; nobody stores books in it). When it comes, the preferred design
  is a config list of storage block entity classes with `BEBetterCrate` by default, so admins can add other mods without a release.
