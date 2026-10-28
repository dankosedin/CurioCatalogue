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
- **Books:** `Entry` (key, count, opaque sample, kind `Exact`; `Pattern` reserved), `Ledger` (mode, entries, sheets, name, album
  info) with `CreateInvoice`, `Post`, `ReturnSheet`, `AddSheets`, `Clear`, `FromStored`; `Invoice`; `Multiset`.
  All immutable; operations return a new value. Refusals are values (`CreateInvoiceResult.Created | Refused`), not exceptions.
  Limits (`LedgerLimits`) come from the config, not from the book.
- **Moving items:** `Transfer.Plan` (all or nothing: `Ready | Missing | NoRoom`) and `Transfer.PlanWhatFits` (partial, in a given order),
  over plain `SourceSlot` / `TargetSlot` descriptions.
- **Collections:** `Collection` (members, groups, snapshot, progress, `Refresh` / `RefreshIfStale`), `TapestryPieces` (pieces from `sizes`).

#### Adapters (`curiocatalogue/Adapters`)

- `StackKeys`: `ItemStack` → `ItemKey` (resolves stacks read from bytes first; ignored attributes from the config).
- `LedgerStore`: ledger and invoice ↔ item attributes under `curiocatalogue`; reads every album up to date with its collection.
- `ContainerReader`: classifies the aimed block (supported storage / other inventory / not ours), resolves multiblocks, reads slots,
  opens writable slots (bags on the ground through a wrapper inventory).
- `ContainerAccess`: claims and reinforcement, behind interfaces for tests.
- `InvoiceMaker`: compares a book with a container and makes an invoice.
- `InvoicePoster`: `Post` (an invoice) and `Collect` (album, want list): builds the plan, checks all of it, then moves with `TryPutInto`
  and updates the off-hand book and the sheet.
- `CollectionRegistry`: the collections known in this world (tapestries from the block's `sizes` plus config).

#### Glue

- `Items/ItemLedger`, `Items/ItemInvoice`: the item classes; click handling (`OnHeldInteractStart`), tooltips, crafting hooks.
- `EmptyHandGesture`: the off-hand gesture, caught on the client in `Input.InWorldAction`.
- `LedgerHintsBehavior`: hints over containers, added to storage blocks on the client.
- `Gui/GridDialog` (base), `Gui/LedgerDialog`, `Gui/InvoiceDialog`, `Gui/ReadOnlySlot`: client-only read-only windows.
- `ModNetwork`: one channel. Client → server: `MakeInvoiceRequest`, `RenameLedgerRequest`, `ClearLedgerRequest`, `CancelInvoiceRequest`,
  `MakeWantListRequest`. Server → client: `MissingItemsRefusal` (the client names the items in its own language).
- `PlayerMessages` (refusals as red in-game errors, results in chat), `ModLog` (diagnostics), `HandbookIndexFix` / `HandbookDiagnostics`.

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
  depending on the game. The core tests build anywhere, also in CI without game DLLs.
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
  `ListBuffer` in a Scala method) stays, rather than being folded with `Aggregate`. Two steps further are in `ROADMAP.md`
  (Technical): an analyzer that makes `switch` over a record hierarchy exhaustive, and books as a sum type.
- **Services created lazily** in the mod system (on first use, not in `Start`), because the world may not be ready in `Start`.

#### State

- **Data in item attributes** (10-26). The book travels like any item; nothing to sync or clean up in the world.
- **Mode and invoice kind are the item's variant, not attributes** (10-26): two sources of truth could disagree.
- **Samples stored as `ItemstackAttribute`**, not raw bytes (10-26): vanilla stores bag contents this way and resolves the item by id on
  load. `Entry.Sample` in the core is therefore an opaque `object`.
- **Keys recomputed on read, not stored** (10-26): a change of ignored attributes in the config applies to existing books; an item
  from a removed mod is kept. The same book always serializes to the same bytes (entries sorted).
- **modid renamed before the release** (`warehouseledger` → `curiocatalogue`, 10-26, author): after a release a modid cannot change
  without breaking worlds.

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
- **Per-player loot chests are unsupported** for making and posting: their slots show the total, not the player's share.
- **Bags on the ground are writable** through a wrapper inventory (the fallback "read-only for bags" was not needed).
- **Album in the off-hand is ignored when posting an ordinary invoice** (10-26). Alternatives: take the sheet back but keep entries;
  keep the old behaviour without the absurd incoming branch. Addition by the author: cancelling any invoice returns its sheet to an album too.

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
- **The album follows its collection on every read** (`RefreshIfStale`, 10-26). Alternatives: a refresh recipe (tried; the player had
  to remember to re-craft after a new mod or config change), showing stale entries separately, or waiting for playtests. The author,
  after trying the recipe: "щось перемудрили".

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
