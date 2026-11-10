# Architecture

How the mod is built and why. Terms are in `docs/domain.md`; game behaviour behind the decisions is in `docs/modding.md`.

Two parts. **Part 1** is the structure of the code: principles, layers and flows; it holds for any feature. The structural
decisions behind it are in `docs/decisions.md`. **Part 2** is how features were fitted into what the game allows and what the author wants the mod to be.

**Read** before adding a class, an adapter, a network message or a new kind of state. **Update** when a decision is made: write
what was chosen, the alternatives and the reason, and who decided ("author" = the mod's author; otherwise proposed by the AI
assistant and accepted by the author). Structural decisions go to `docs/decisions.md`. A decision that belongs to one feature goes to the
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
   needs a decision entry in `docs/decisions.md` (what was tried, why it did not work), prefers a Postfix over a Prefix, never uses a Transpiler without
   the author's agreement, and cites the patched method (`File.cs:line#Method`) so `tools/check-refs.ps1` notices when it moves.
7. **State lives in the items.** Books and invoices carry their data in item attributes; there is no world-level storage.
8. **Small pieces, one job each.** A file or class does one job and stays small enough to read whole. When a change is about to
   land in something big (several hundred lines, or several jobs in one place), first think whether to split it; the split is
   its own change, before the feature, so the feature's diff stays readable. The same holds for plan steps: a step fits one
   session (one layer or one flow, some tens of edits); a bigger one is split while planning.

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
  `CollectionResolution` (`Found | Disabled | Unknown`, the one answer to "which collection is this album's"); adapters `CollectionRegistry`,
  `CollectionDefinitionReader`, `AlbumRecipes` (plan `2026-11-07-collections-core.md`).
- **The album window as data:** `AlbumPages.Build` lays a level's groups in a layout out on pages by `PageMetrics` (whole groups,
  side by side then under each other; a group larger than a page cut by lines; tables of the values that occur; a picture's
  shape), with the tabs (each with pages of its own; the pages the page buttons turn, `PagerAt`), search (`NextPageWith`), what is under the mouse (`CellAt`, `HeadingAt`)
  and a cell's cycling stacks (`Slideshow`). Positions only: what is collected is read when
  drawing (`Album.StacksToShow`, `MemberGroup.CollectedCount`), so a refresh moves nothing (plan `2026-11-04-album-view.md`, A3).

#### Adapters (`curiocatalogue/Adapters`)

- `StackKeys`: `ItemStack` → `ItemKey` (resolves stacks read from bytes first; ignored attributes from the config).
- `LedgerStore`: books and invoices ↔ item attributes under `curiocatalogue`; reads an album of format 1 as format 2 (the migration).
- `ContainerReader`: classifies the aimed block (supported storage / other inventory / not ours), resolves multiblocks, reads slots,
  opens writable slots (bags on the ground through a wrapper inventory).
- `ContainerAccess`: claims and reinforcement, behind interfaces for tests.
- `InvoiceMaker`: compares a book with a container and makes an invoice.
- `InvoicePoster`: `Post` (an invoice) and `Collect` (album, want list): builds the plan, checks all of it, then moves with `TryPutInto`
  and updates the off-hand book and the sheet.
- `CollectionRegistry`: the collections known in this world, from the definitions of every domain and the config; a bad definition is logged once and skipped.
- `CollectionDefinitionReader`: reads and checks the definition assets (a bad one is `Bad`, never an exception).
- `AlbumRecipes`: at `AssetsFinalize` reads the definitions itself and removes the album recipe of every switched-off collection from the server's grid recipes.

#### Glue

- `Items/ItemLedger`, `Items/ItemInvoice`: the item classes; click handling (`OnHeldInteractStart`), tooltips, crafting hooks.
- `EmptyHandGesture`: the off-hand gesture, caught on the client in `Input.InWorldAction`.
- `LedgerHintsBehavior`: hints over containers, added to storage blocks on the client; no "take new" or "take from the list" hint for an album or want list whose collection is not `Found` (switched off or gone).
- `Gui/HeldItemDialog` (base: watches the item in the hand, refreshes or closes, names it in requests), `Gui/GridDialog` (base:
  a read-only slot grid), `Gui/LedgerDialog` (ledger, bill of materials), `Gui/InvoiceDialog`, `Gui/ReadOnlySlot`: client-only windows.
  `Gui/BookWindow` opens the window of a book in a hand, one at a time.
- `Gui/AlbumDialog`: the album's own window (plan `2026-11-04-album-view.md`, A3c): tabs (`Gui/AlbumTabs`, vanilla's vertical
  tabs with a check mark and an icon), a page, the row of controls (`Gui/AlbumControls`, L9: level, tabs, layout as `Gui/IconDropDown`, L2c, our dropdown
  with icons; search, page buttons, want list, start over) and the
  album's own row (`Gui/AlbumHeader`, A3d: the name and its pencil, the progress bar, the sheets). A refresh flips cells and
  sets the bar and the sheets; turning a page, a layout or a level composes. An album whose collection the game lacks shows
  `Album.CollectedOnly`.
- `Gui/AlbumPageElement`: one page of the album window drawn by us from `AlbumPages` (stacks, flat tapestry images, headings,
  the rarity corner, cycling stacks, search dimming); `Show(album)` flips what is collected without composing again; hover
  gives the game's tooltip through `Gui/AlbumSlot`.
- `ModNetwork`: one channel. Client → server: `MakeInvoiceRequest`, `RenameLedgerRequest`, `ClearLedgerRequest`, `CancelInvoiceRequest`,
  `MakeWantListRequest`, `ChooseLevelRequest`. Server → client: `MissingItemsRefusal` (the client names the items in its own language), `CollectionSettingsPacket` (the server's `Collections` and `ExtraIgnoredAttributes`, on join).
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

In `docs/decisions.md`.

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
