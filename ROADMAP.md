# Roadmap

What may come after 0.1.0, and roughly when. Why the mod exists and what it will not do: `DESIGN.md`.

**Horizons:**
- **Next:** what we work on, in this order. Several releases, not one.
- **Later:** wanted; order not decided.
- **Maybe:** ideas without promises, often because the design space or the research is too large to judge yet.
- **When pigs fly:** ideas we would rather not build; only on strong demand.

**Update** when an idea comes up (put it under a horizon, with a line on why) or a decision moves it. When an idea gets its own plan
in `docs/plans/`, replace its line with a link. Priorities follow playtests and players' feedback.

## Next

1. **Stamp-album view.** The album window shows the whole collection: collected pieces in their slots, missing ones as dim empty
   cells, grouped by picture. Today it shows only what is missing.
2. **Butterflies, the second collection.** Collected by a player on the author's server. A second collection tests how general the
   core is. Different from tapestries: the species is the item's variant (`butterfly-dead-<type>`), not an attribute; 169 types; two
   forms, live (`creature-butterfly-*`) and dead (`butterfly-dead-*`, goes on shelves and into display cases); males and females are
   separate types. Open: which form counts, and how to group (by species?). Ask the player who collects them.
   Before it (review, 28.10.2026): one collection resolver used by the store, the items and the network (today
   `Collections.Get` is called in three places), and the registry as a list of collection definitions, not tapestries only.
   Measure the cost of reading a book first: tooltips and crafting checks read the whole book, the window hashes its whole
   data every 500 ms; fine for 88 tapestries, maybe not for 169 butterflies.
3. **Invoices:**
   - **Names from the book.** A want list from the album "Danko's tapestries" is called "Want list for: Danko's tapestries"; the same
     for invoices from a named book.
   - **Partial posting for every invoice,** as the want list already does: what is there and fits moves, the invoice stays shorter.
     Open: by default or with another gesture (today "missing, nothing moves" is a useful signal for an outgoing invoice); a partly
     posted incoming invoice records only what moved.
4. **Recipes into a bill of materials** (the killer feature of the bill of materials):
   - **from the handbook:** a button on a recipe's page puts its ingredients into the bill of materials; pages of other mods' items
     need a Harmony patch of the handbook (`docs/modding.md`, Handbook);
   - **into raw materials:** "5 barrels" becomes boards and hoops; needs a choice when an item has several recipes;
   - **template entries** ("any boards"): vanilla ingredients are often wildcards, so expanding recipes needs them. The deepest
     change to the core (exact `ItemKey` matching, which stack a transfer takes), so it is done early, with this feature.
5. **Custom collections.** The player writes what a full set is (search in the book's window, the handbook) and turns the book into
   an album of that collection: progress, taking only new items, want lists. Covers everything the mod has no built-in collection
   for. New: the collection lives in the item, not in `CollectionRegistry`. Brings **search in the book's window** with it.
6. **Chests on boats** (an entity's inventory). The author loves boats. Costlier than it looks (review, 28.10.2026): "a container"
   is a block position everywhere (the network request, `InvoiceMaker` / `InvoicePoster`, `ContainerAccess`, hints as a
   `BlockBehavior`, the invoice's saved position). First a `ContainerTarget` (block | entity) behind `ContainerReader` and
   `ContainerAccess`; the access rule for an entity's inventory needs research.

7. **Ore crystals, the third collection** (29.10.2026). Players on the author's server collect them. The item is
   `crystalizedore-{grade}-{ore}-{rock}` (assumed to be what players mean; check with them): 4 grades (poor … bountiful), 18 ores,
   17 rocks, about 400 allowed combinations in vanilla (`itemtypes/resource/crystalizedore-graded.json`). Unlike tapestries and
   butterflies, a piece has three dimensions. Open: what counts as one piece and how the album starts: by ore only (18), by ore and
   grade (~70), or every combination (~400); or the player chooses when making the album. Needs the album to be told its
   dimensions, which fits the resolver and the registry as definitions (item 2) and may come with custom collections (item 5).
   Order against items 2 to 5 not decided.

8. **Seashells, the fourth collection** (asked on the ModDB page, 30.10.2026). Vanilla block `seashell-{type}-{color}`: 7 types,
   6 colors, 42 combinations, two dimensions like ore crystals. Open: what counts as one piece (type or type and color), and
   whether a picked-up shell is an item stack in a chest.

## Later

**Albums and collections**
- **Exhibits:** record a collection where it is displayed, without moving it: tapestries on walls, butterflies on shelves and in
  display cases, other wall-hung things, and mods that add display blocks. A lot of work: many block kinds. Pieces displayed by
  others count too: the collection is the items, the album only a way to look at it.
- **More collections,** from players' feedback.
- **Can the tapestry collection be completed?** (review, 28.10.2026) Some pieces in the game's `sizes` have index `-1` and are not
  in `tapestryGroups` (lore tapestries: `aid-*`, `glider`, `symptoms` and others). If survival never gives them, "the collection
  is complete" is out of reach, while README promises 88 pieces. Research in game and in the worldgen schematics, together with
  butterflies (Next, 2) and decorating a complete album.
- **Decorating a complete album:** golden corners or a seal on the cover for a full collection, a decorated page for a complete
  picture, maybe steps (25 / 50 / 100 %). On the book itself it depends on our own book model (see Spin-off ideas); in the window it does not.

**Books**
- **Editing a bill of materials in its window:** add, remove, change counts. An album is never edited (its collection decides).
- **The ledger's last operations, with undo,** instead of editing it by hand. Open: undo forgets that an invoice was posted; items
  that moved stay where they are.

- **Books and invoices from the creative inventory** (review, 28.10.2026). They have no data: a book has 0 sheets, an album
  follows no collection and says "the collection is complete", a blank invoice posts as "0 lines" and gives the book a free sheet.
  Defaults with sheets would break how the handbook shows the recipes, so 0.1.0 leaves it as it is. Open: hide them from the
  creative inventory, or defaults that keep the handbook working; a blank invoice refused in any case.

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
- **Tapestry pieces told apart in lists** (seen 28.10.2026): a want list's tooltip shows "1 × Nightfall (tapestry)" three times,
  since the pieces of one picture share a name; the album window tells them apart only by the game's "Section" line. Show the
  piece's place in the picture (e.g. "Nightfall, 2 of 3") wherever we list pieces.

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
  hand, else the inventory, else the ground" twice); one `Outcome` packet and one helper for handing out items.
- **Exhaustive `switch` over record hierarchies** (28.10.2026). Outcomes are closed record hierarchies (`TransferPlan`:
  `Ready | Missing | NoRoom`; `CreateInvoiceResult`), but unlike Scala's sealed traits C# does not warn when a `switch` misses a
  case. A Roslyn analyzer (e.g. `ExhaustiveMatching.Analyzer`) makes it a compile error. Cheap; check it with CI and the IDE.
- **Books as a sum type** (28.10.2026): `LedgerBook | MaterialsBook | Album` instead of one `Ledger` with a `Mode` and an `AlbumInfo`
  that only an album has. An album then cannot be asked for an invoice at all, and the `InvalidOperationException`s in `Ledger`
  go. A deep change of the core and of how books are stored: do it together with template entries (Next, 4), which change the
  data model anyway (README warns that the model may change). Review, 28.10.2026: then also the variant (`book-album-*`) as the
  only sign of an album (today the `album` attribute tree says it too, `docs/architecture.md` rejected two sources), and a typed
  sample instead of `Entry.Sample` as `object` with casts to `ItemStack` all over the adapters.
- **Tests for the item classes** (review, 28.10.2026): gestures, crafting hooks, tooltips of `ItemLedger` / `ItemInvoice` have
  none. Pull their decisions out into testable code first (as the invoice's fate in the 0.1.0 fixes).
- **`HandbookIndexFix` touches shared index entries** (review, 28.10.2026): it replaces the sample stack of every ingredient entry
  that one of our recipes uses, so another mod's clutter-based recipe may show a plain sample in the handbook. A known risk; look
  at it when someone reports it or when the game changes the index.
- **A page of commands for testing** (the author, 28.10.2026), e.g. `docs/testing.md`: vanilla commands with their exact syntax
  (`/giveblock tapestry-north 1 s[] {"type":"ambush1"}`, from the engine's `CmdGive`), our `/curio` commands from `dev-commands`,
  and what `docs/development.md` now scatters (`/op`, whitelist, `/land claim …`, padlocks).

## Maybe

- **Rarity in the album:** a frame by rarity in the stamp-album view, as real stamp and coin catalogues show it. Large design space:
  computed from the game (loot table weights, butterfly spawn by climate; needs research) or set in the collection's config.
- **Parcels:** our own container, in several sizes. Posting an invoice packs the items into it instead of the inventory, and the
  invoice goes with the parcel; at the destination a book posts it and unpacks. Put on a cart or into a boat, trade becomes physical.
  A strong idea; the priority is for later. The mod still sets no prices: trading stays between players. Research: an item with
  contents (as baskets, `IHeldBag`) in boats, display blocks and other mods.
- **Losing the sheet with a lost invoice** (hardcore, on top of `ConsumeSheets`).
- **Items lying on the ground** as a container: nothing to click on, each item entity has its own access check; little use.

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
