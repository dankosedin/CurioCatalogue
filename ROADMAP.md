# Roadmap

What may come after 0.1.0, and roughly when. Why the mod exists and what it will not do: `DESIGN.md`.

**Horizons:**
- **Next:** what we work on, grouped by release, in this order within a release.
- **Later:** wanted; order not decided.
- **Maybe:** ideas without promises, often because the design space or the research is too large to judge yet.
- **When pigs fly:** ideas we would rather not build; only on strong demand.
- **Done:** at the very bottom; what was released, with links to its plans.

**Update** when an idea comes up (put it under a horizon, with a line on why) or a decision moves it. When an idea gets its own plan
in `docs/plans/`, replace its line with a link. When a release or a plan is done, move its section to Done. Priorities follow
playtests and players' feedback.

## Next

### 0.2: collections

Order decided 04.11.2026: the risky and heavy work first, the tails last. Each item below becomes a plan of its own, planned
separately when its turn comes. Code review comes after a feature that changes code (as the last step of
its plan) or once at the end of the release; each plan says which. Butterflies were promised for 0.2 on the ModDB page (29.10.2026).

1. **Backward compatibility, before anything changes the stored data:** [the plan](docs/plans/2026-11-04-backward-compat.md).
   Golden stacks made by the v0.1.1 code in the test server and read by the server tests in CI; the fixture world for the
   manual pass (`docs/testing/upgrade-0.1-to-0.2.md`). Each later plan of 0.2 that changes stored data, recipes, item codes,
   config or lang fills that file's table "Changes in 0.2.0 that need a case".
   Left by its code review (B6, 04.11.2026), for the capture of 0.2.0:
   - **One file for the golden format:** the capture's records and id walk (`Golden.cs`) and the reader's (`GoldenStacks.cs`)
     use only the game's types, so they can be one shared file; decided when the capture is rewritten for 0.2.0 and the format changes.
   - **The tooltip of every golden stack** built by a test (`GetHeldItemInfo`, as `TooltipTests`): today U3 and U4 check the data
     the tooltip and the window show, not that they are built from an old item without an error.
   - **The capture built by CI** (not run): one cached download of the release zip, so that a change to a shared fixture file
     that breaks the capture shows in the pull request, not at the release.
2. **Collections designed first:** [the plan](docs/plans/2026-11-04-collections-design.md) (04.11.2026). Ore crystals,
   seashells and butterflies on paper, the config of mods researched, the cost of reading a book measured; the result is the
   shape of a collection and the plan of the core (item 3), so that the core is built once.
3. **The core of collections:** [the plan](docs/plans/2026-11-07-collections-core.md) (07.11.2026). Definitions as assets
   built by sources, levels, members of several codes, the album storing what it collected, want lists by members, one
   resolver, the server's `Collections` sent to clients. Fills the upgrade table.
4. **The collections of 0.2:** [the plan](docs/plans/2026-11-09-vanilla-collections.md) (09.11.2026), in this order, by what
   they give the core (item 2, Decisions); each a definition asset, its album recipe and its lang, and fills the upgrade table.
   What each needs (collections design plan, Decisions):
   - **Ore crystals** (29.10.2026, players on the author's server collect them): `crystalizedore-{grade}-{ore}-{rock}`, source
     `variants`; three levels (16 / 118 / 353: ore with the richest grade, ore × rock with the richest grade, every combination);
     the 54 combinations that never generate as the definition's `exclude`; rocks' stones as icons; grade names of our own; the
     rock shown wherever a missing piece is listed (pieces of two rocks share a name).
   - **Seashells** (asked on the ModDB page, 30.10.2026): block `seashell-{type}-{color}`, source `variants`; two levels
     (7: type, any colour; 42: type × colour); the rare colours (seafoam, turquoise) as an attribute rule, shown or not as item 6
     decides.
   - **Butterflies** (collected by a player on the author's server; promised for 0.2 on the ModDB page, 29.10.2026):
     `butterfly-dead-<type>`, 169 types, source `variants` (members from the loaded items: 169, not the entity's 170); one level,
     no dimensions until grouping by species is decided in item 6; dead only, live ones of other mods would be another
     collection. Collected butterflies lie on displays more than in chests (Exhibits, Later).
   - **For the core, when these land** (code review K7, A8): `Member.Fills` and `Member.Shown` are one thing today; a collection where
     what a member accepts differs from what the album shows (a berry bush: any cutting fills, one is drawn) needs them apart.
5. **Stamp-album view:** [the plan](docs/plans/2026-11-04-album-view.md) (04.11.2026). Pages by group, collected pieces in place,
   missing ones as dim silhouettes. Designed before the shape of collections (C7 of item 2), built after the core (item 3).
6. **Every vanilla collection, polished in the album:** [the plan](docs/plans/2026-11-09-album-polish.md) (09.11.2026). Back to tapestries, ore crystals, seashells and butterflies (last), each one's look
   in the stamp-album view (grouping, order, names), and its album recipe if it needs a change. Among them:
   - **Tapestry pieces told apart in lists** (seen 28.10.2026): a want list's tooltip shows "1 × Nightfall (tapestry)" three
     times, since the pieces of one picture share a name; the album window tells them apart only by the game's "Section" line.
     Show the piece's place in the picture (e.g. "Nightfall, 2 of 3") wherever we list pieces.
   - **Album window: width set by the control row, not the cells** (seen 09.11.2026, after V4): the window is as wide as the bottom
     rows of controls need, which shows with butterflies (a narrow grid under wide controls, a plain look). Add another column of
     butterflies and spread the controls a little, so the window reads as one clean block; check the other collections too. L6 of the plan.
   - **Want list window: a line cycles through its items** (asked 09.11.2026, after V3): a line that any of several items fills
     (a shell of any colour, a crystal of any rock) changes the item shown, as the album's cells do. L7 of the plan.
   - **Want list window: a grid with no empty slots** (seen 10.11.2026, L6): its grid is the game's, always 10 columns with the
     last row filled up by empty slots, while the album's cells now spread evenly over their lines with none empty. Whether the
     want list follows the album (lines spread evenly, no empty slots) or keeps the look of the game's chests.
   - **The in-game guide and the album's handbook text name only tapestries** (seen 09.11.2026, V5): `guide-text` and
     `handbook-use-album-text` in `lang/*.json` still say "the first collection is tapestries" and describe the album by
     "pictures"; say there are four collections (en and uk). L8 of the plan.
   - **Screenshots taken again with the stamp-album view:** [the release plan](docs/plans/2026-11-04-release-0.2.md), R3, by
     the shot list of [the description plan](docs/plans/2026-11-04-mod-description.md), G5.
7. **Fixes:** [the plan](docs/plans/2026-11-04-fixes-0.2.md) (04.11.2026). Stale game-source references (and a symbol for
   every reference), one helper for handing out items (a new list no longer vanishes in creative), lists out of the creative
   inventory but still in the handbook (last, after the recipes of the collections).
8. **The release,** two plans (04.11.2026):
   - [the description plan](docs/plans/2026-11-04-mod-description.md), started early: the ModDB description as a document of
     its own instead of the READMEs, sequences of screenshots with captions, the shot list of 0.2;
   - [the release plan](docs/plans/2026-11-04-release-0.2.md), at the end: code review, texts for players, screenshots, the
     release zip in game (functional and upgrade), a players' round, the release.

### After 0.2

Proposed (31.10.2026), not decided: 0.3 for invoices and comfort (item 1, the hotkey for the book's window, the server's config
sent to clients, messages built by the client), then recipes into a bill of materials with books as a sum type.

1. **Invoices:**
   - **Names from the book.** A want list from the album "Danko's tapestries" is called "Want list for: Danko's tapestries"; the same
     for invoices from a named book.
   - **No coordinates on an invoice** (players' round, 31.10.2026): its tooltip shows where it was made (coordinates from spawn)
     to whoever holds it, which leaks a base's place on servers that hide coordinates. Show the chest's label if it has one,
     else nothing.
   - **Partial posting for every invoice,** as the want list already does: what is there and fits moves, the invoice stays shorter.
     Open: by default or with another gesture (today "missing, nothing moves" is a useful signal for an outgoing invoice); a partly
     posted incoming invoice records only what moved.
2. **Recipes into a bill of materials** (the killer feature of the bill of materials):
   - **from the handbook:** a button on a recipe's page puts its ingredients into the bill of materials; pages of other mods' items
     need a Harmony patch of the handbook (`docs/modding.md`, Handbook);
   - **into raw materials:** "5 barrels" becomes boards and hoops; needs a choice when an item has several recipes;
   - **template entries** ("any boards"): vanilla ingredients are often wildcards, so expanding recipes needs them. The deepest
     change to the core (exact `ItemKey` matching, which stack a transfer takes), so it is done early, with this feature.
3. **Custom collections.** The player writes what a full set is (search in the book's window, the handbook) and turns the book into
   an album of that collection: progress, taking only new items, want lists. Covers everything the mod has no built-in collection
   for. New: the collection lives in the item, not in `CollectionRegistry`. Brings **search in the book's window** with it.
4. **Chests on boats** (an entity's inventory). The author loves boats. Costlier than it looks (review, 28.10.2026): "a container"
   is a block position everywhere (the network request, `InvoiceMaker` / `InvoicePoster`, `ContainerAccess`, hints as a
   `BlockBehavior`, the invoice's saved position). First a `ContainerTarget` (block | entity) behind `ContainerReader` and
   `ContainerAccess`; the access rule for an entity's inventory needs research.
5. **Settings in ConfigLib's window** (the author, 04.11.2026; research in the collections design plan, C5). A soft integration
   with no code dependency: an asset `config/configlib-patches.json` with `"file": "curiocatalogue.json"` (ConfigLib edits our
   file in place), the scalars first (`MaxSheets`, `SoftEntryLimit`, `ConsumeSheets`, `Diagnostics`), and a listener on the game's
   event bus (`configlib:curiocatalogue:config-saved`) that rebuilds what the config feeds. After the server's config sent to
   clients (Later, Technical): ConfigLib does not change the file a client reads. A test that every path in the asset exists in
   the config class. To check first: two writers of one file (our write-back on start, ConfigLib's) and what ConfigLib does
   with a file that does not parse.

## Later

**Albums and collections**
- **Exhibits:** record a collection where it is displayed, without moving it: tapestries on walls, butterflies on shelves and in
  display cases, other wall-hung things, and mods that add display blocks. A lot of work: many block kinds. Pieces displayed by
  others count too: the collection is the items, the album only a way to look at it. Butterflies lie mostly in such blocks: a
  collector on the author's server keeps them both in chests and on wall plaques (Butterfly Displays, 04.11.2026); vanilla shelves,
  display cases and all three butterfly display mods are `BlockEntityDisplay` inventories (the collections design plan, C3).
  Plants too: a planted berry bush or fruit tree is "a cutting on the wall", it keeps the cutting's type and traits (the author,
  04.11.2026; the collections design plan, C4).
- **More collections,** from players' feedback. Noted by the author (31.10.2026): **fish**, **fruit trees** and **berry
  bushes**. Open for each: what one piece is (a species? a fruit or a sapling?) and whether it can lie in a chest at all.
- **Can the tapestry collection be completed?** (review, 28.10.2026) Some pieces in the game's `sizes` have index `-1` and are not
  in `tapestryGroups` (lore tapestries: `aid-*`, `glider`, `symptoms` and others). If survival never gives them, "the collection
  is complete" is out of reach, while README promises 88 pieces. Research in game and in the worldgen schematics, together with
  butterflies (Next, 0.2) and decorating a complete album.
- **Tapestries of other mods** (found 02.11.2026, plan 2026-11-02-server-tests, T17). The album counts what extends `sizes` of
  `game:tapestry`, but the mods that add tapestries today add blocks of their own, class `BlockTapestry` with their own `sizes`:
  Craftable Companion (`craftablecompanion:tapestry-*`, one 2×2 picture sold by the artisan) and Astria Porta
  (`astriaporta:astriaportatapestry-*`, two pictures found in its ruins). Open: count every `BlockTapestry` block (one collection,
  or one per mod?), what such a stack drops as, and whether players want it. A pinned compatibility test then.
- **Decorating a complete album:** golden corners or a seal on the cover for a full collection, a decorated page for a complete
  picture, maybe steps (25 / 50 / 100 %). On the book itself it depends on our own book model (see Spin-off ideas); in the window it does not.

**Books**
- **Editing a bill of materials in its window:** add, remove, change counts. An album is never edited (its collection decides).
- **The ledger's last operations, with undo,** instead of editing it by hand. Open: undo forgets that an invoice was posted; items
  that moved stay where they are.

- **Books from the creative inventory** (review, 28.10.2026). They have no data: a book has 0 sheets, an album follows no
  collection and says "the collection is complete". Defaults with sheets would break how the handbook shows the recipes, so 0.1.0
  leaves it as it is. Open: hide them from the creative inventory, or defaults that keep the handbook working. Lists: see Next,
  0.2, item 7 (a blank list is refused and cancels to nothing since 0.1.1).

**Containers**
- **More vanilla containers** for invoices: cabinets, shelves and other blocks with an inventory that we now refuse as "not supported yet".

**Interface**
- **Highlighting in a container's window:** with an invoice in hand, the slots it lists; with an album, what is new for the
  collection; with a want list, what it asks for. One mechanism for all three.
- **Knowing what the album lacks without carrying the book** (first ModDB comment, 29.10.2026: "must I carry the book to check
  a piece?"). The highlighting above needs the book in the inventory, which costs the materials for the book and an inventory slot,
  too much to carry only for a hint. Wanted: the same hint ("in the album: have / missing") for a cheap price. Ideas to weigh
  (not decided):
  - **a list of the want lists** the player has made, readable somewhere other than the inventory;
  - **a dynamic page in the handbook** with the state of the collections (needs a Harmony patch of the handbook, `docs/modding.md`;
    the handbook knows only recipes today);
  - **a cheap thing that gives the hint,** e.g. a worn item or a one-time resource instead of the whole book ("magic"); open whether
    it takes an inventory slot or a clothing slot, and what it needs to be made.
  Open: where the album's data comes from when the book is not in the inventory (the data lives in the item, `DESIGN.md`).

- **A new window for the ledger** (the album view plan, A2, 07.11.2026): the album gets a window of its own in 0.2, the ledger
  keeps today's. Its turn: a compact header as the album's (the name with a pencil, the sheets as parchment, facts as elements),
  and what else a ledger's window needs, designed for it.
- **A new window for the bill of materials** (the same): as the ledger's, with what a bill of materials needs (counts, sorting by
  count, editing: Books above).

- **A hotkey for the book's window** (review, 28.10.2026): today it opens only on right click into the air, which is hard to
  find in a cellar or at a workbench.

**Access and ownership**
- **Locks on books.** Protect the book itself, e.g. a guild ledger on a shelf: everyone reads, only the owner and those allowed post
  and edit. Open: what is protected (reading, changing, making invoices), how access is given (a vanilla padlock, owner = who put
  it on, as with chests), how the lock looks.

**Technical**
- **The server's config sent to clients** (review, 28.10.2026). Each side reads its own `ModConfig` file and the game does not
  sync them (vsapi `ICoreAPI.cs:150`). The client then builds its own tapestry collection, item identity and sheet limits: album
  progress, missing lists, tooltips and the recharge recipe differ from what the server does. Fix: one packet on join with the
  gameplay settings, used by the client instead of its file. Until then README asks admins to hand out the same file.
- **All player messages built by the client,** in its language and with its key bindings: the server sends a code, numbers and
  stacks (as `ModNetwork.MissingItemsRefusal` does). Today the server translates our texts itself (`docs/architecture.md`,
  principle 2); only rebound keys show wrong. Do it when a feature touches these messages anyway. Review, 28.10.2026: messages
  and handing out new items are spread over `ItemLedger`, `ItemInvoice`, `ModNetwork` and `EmptyHandGesture` ("into the empty
  hand, else the inventory, else the ground" twice); one `Outcome` packet and one helper for handing out items. Code review of
  0.1.1: the messages after making a list (`invoice-made`, `wantlist-made`) name "Shift" as written, while the tooltip follows a
  rebinding; research how the client could name the key there.
- **Books as a sum type** (28.10.2026): `LedgerBook | MaterialsBook | Album` instead of one `Ledger` with a `Mode` and an `AlbumInfo`
  that only an album has. An album then cannot be asked for an invoice at all, and the `InvalidOperationException`s in `Ledger`
  go. A deep change of the core and of how books are stored: do it together with template entries (Next, after 0.2), which change
  the data model anyway (README warns that the model may change). Review, 28.10.2026: then also the variant (`book-album-*`) as the
  only sign of an album (today the `album` attribute tree says it too, `docs/architecture.md` rejected two sources), and a typed
  sample instead of `Entry.Sample` as `object` with casts to `ItemStack` all over the adapters.
- **Tests for the client code** (review, 28.10.2026; narrowed by the coverage review, 03.11.2026: the gestures, crafting hooks and tooltips
  of the item classes now have server tests): the windows (`Gui/`), the hints over containers (`LedgerHintsBehavior`), `HandbookIndexFix` and
  the client side of the gestures run only in game (A1, A8, A9, C7–C9, T1 of `functional.md`). Pull their decisions out into testable code
  first (as the invoice's fate in the 0.1.0 fixes).
- **A clean log over the whole test run** (code review of the server tests, T18, 03.11.2026): today the server tests check the log of the
  start and of a few actions; an error the mod logs in the middle of another test (e.g. `InvoicePoster`'s "Planned to move …") goes
  unseen. A check at the end of the run, with the tests that make the mod log on purpose declaring their lines. First find out how xUnit
  reports a failure in a collection fixture's `Dispose` and whether `dotnet test` turns red on it.
- **Known log noise per test project** (the same review): `LogProblems.KnownNoise` is shared by the server and the compatibility tests.
  When a second pinned mod brings noise of its own, give each fixture its own list, so another mod's pattern cannot hide our warning.
- **A flaky server test** (plan backward-compat, B1, 04.11.2026): one `dotnet test curiocatalogue.sln` run had 2 failures in
  `CurioCatalogue.ServerTests` (2 min 22 s instead of 1 min 21 s, the compatibility tests running alongside); the next runs, alone
  and of the whole solution, were green. Which tests failed was not seen. Next time: keep the output (a `trx` logger) and look for a
  timeout under load. Seen again in B6 (04.11.2026): `BookEdgeTests.L1_an_intake_list_lands_on_the_ground_when_the_inventory_is_full`
  failed after 30 s in a whole-solution run (server tests 1 min 41 s instead of 1 min 12 s); green alone right after.
  Seen again after T4's fix (A4 of the album view, 08.11.2026): twice in whole-solution runs, 30 s, green alone. The column search did
  not end it; next: keep the `trx` output and log the ground items near the player when the wait ends (where the list landed, or
  whether it was dropped at all).
- **`HandbookIndexFix` touches shared index entries** (review, 28.10.2026): it replaces the sample stack of every ingredient entry
  that one of our recipes uses, so another mod's clutter-based recipe may show a plain sample in the handbook. A known risk; look
  at it when someone reports it or when the game changes the index.
- **The review procedure, next ideas** (`docs/review.md`): tests from the C# expert as proof of a bug; a security role first in
  line for outside pull requests (calls out of the game, processes, build scripts, new dependencies) before anyone builds them;
  a check that the code does what `DESIGN.md`, the READMEs and the lang texts say.

## Maybe

- **Rarity in the album:** a frame by rarity in the stamp-album view, as real stamp and coin catalogues show it. Large design space:
  computed from the game (loot table weights, butterfly spawn by climate; needs research) or set in the collection's config.
  Rarity is a property of a cell, not a dimension; perhaps a sort key. Several tiers (e.g. rare, epic, legendary), decided with
  the feature (the author, 04.11.2026). Butterflies carry it in their assets: six steps of spawn chance (the collections design
  plan, C3). The mark on a cell is a coloured corner, the tier its colour (the album view plan, A2, 07.11.2026); a frame or a foil
  effect, as on MTG cards, are ideas for the feature.
- **An album that improves:** one cell per kind holding the best specimen, with a quality order: e.g. per berry bush type the
  cutting with the most good traits and no bad ones; a better one found replaces the one kept (the author, 04.11.2026). Another
  kind of album beside "is it collected"; the berry bush traits in the collections design plan, C4.
- **Catch and Release: Butterflies** (integration target): an album of live butterflies, `game:creature-butterfly-{type}`, which
  that mod's net gives and its cages hold. A collection of its own, never mixed with the dead one (the collections design plan,
  Decisions). Not on the author's server (04.11.2026).
- **Parcels:** our own container, in several sizes. Posting an invoice packs the items into it instead of the inventory, and the
  invoice goes with the parcel; at the destination a book posts it and unpacks. Put on a cart or into a boat, trade becomes physical.
  A strong idea; the priority is for later. The mod still sets no prices: trading stays between players. Research: an item with
  contents (as baskets, `IHeldBag`) in boats, display blocks and other mods.
- **Losing the sheet with a lost invoice** (hardcore, on top of `ConsumeSheets`).
- **Items lying on the ground** as a container: nothing to click on, each item entity has its own access check; little use.
- **Books in schematics: research, not a planned change** (03.11.2026, while building the screenshot studio). A book's
  entries are item stacks in a tree array (`entries: [ {stack, count} ]`, also the album's `members`); when a schematic is
  pasted (WorldEdit, worldgen structures, perhaps other mods' blueprints), the game remaps item ids only in nested trees,
  not in arrays (vsapi `Collectible.OnLoadCollectibleMappings`). So a book carried in a schematic into a world with
  another mod set may point at other items. Checked only with the same mods (it works). To find out: does it happen in
  game with a different mod set; which ways of moving blocks between worlds players use; whether `ItemLedger` and
  `ItemInvoice` should remap their own entries (`OnStore`/`OnLoadCollectibleMappings`) or store them otherwise.

## When pigs fly

- **Album copies** (album + parchment) or **a shared album** synced by the server; the want list covers co-op. A shared album
  would also break "the data lives in the item".
- **Better Crates.** A crate holds one kind of item, which is too little for collections. Revisit after partial posting, template
  entries and recipes from the handbook (Next): taking a bill of materials from such crates may then be in big demand. A possible
  way: a list of storage classes in the config (default `BEBetterCrate`), so server admins add storage without a release.
- **Our items in a vanilla crate.** Shift+RMB on a container is ours, and the crate takes items with that gesture. Giving it back to
  the game for crates would make our gestures inconsistent.
- **An animation of taking items** from a container, to make the move felt (`DESIGN.md`, Main ideas). Lowest of all.

## Spin-off ideas

- **Bookcrafting** (a separate mod): books made from what they are crafted of, wooden boards of the planks' wood and clasps of the
  nails' metal (all 14 vanilla metals; our recipe takes only iron, meteoric iron and steel), with their own model and textures by
  attributes cached on the client (as vanilla `ItemShield`). Collectors love looks, and books that players use earn them; a separate
  mod gives them to players who write books too, and keeps model risks (other mods' rendering) out of this mod, which can pick the
  model up. Would also let the cover colour become an attribute (`docs/architecture.md`, Assets).

## Done

### 0.1.1: fixes

[The 0.1.1 plan](docs/plans/2026-10-31-release-0.1.1.md): the aged covers' handbook pages, English names of the invoices, small fixes.
Released 01.11.2026.
