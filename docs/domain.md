# Domain: terms and mechanics

What the mod's things are called and how they behave, as built in 0.1.0. Why the mod exists and for whom: `DESIGN.md`.
How it is built: `docs/architecture.md`.

**Read** before touching books, invoices, albums or collections, and whenever a term is unclear. **Update** when a term or a
mechanic appears or changes (behaviour changes need the author's agreement first, see `DESIGN.md`).

## Glossary

Player-facing names in English and Ukrainian (`lang/en.json`, `lang/uk.json`); names in code where they differ.

| Term | Ukrainian | In code | Meaning |
|---|---|---|---|
| **book** | книга | item type `book`, class `ItemLedger` | any of the three books below; one item type `book-<kind>-<cover>` |
| **ledger** | облікова книга | `book-ledger-*`, `LedgerMode.Types` | a register of item *types* (a set) |
| **bill of materials** | список матеріалів | `book-materials-*`, `LedgerMode.Counts` | a register of types *with counts* (a multiset) |
| **album** | альбом | `book-album-*`, `LedgerMode.Album` | a types book filled with a collection; its entries are what is still **missing** |
| **entry** | запис | `Entry` | one item type in a book (with a count in a bill of materials); stored as a sample stack |
| **invoice** | накладна | item type `invoice`, class `ItemInvoice` | a standalone document listing real stacks with counts; not bound to a book |
| **incoming invoice** | прибуткова накладна | `invoice-incoming` | what a container has that the book lacks |
| **outgoing invoice** | видаткова накладна | `invoice-outgoing` | what a container has of the book's entries |
| **want list** | список бажаного | `invoice-wantlist` | what an album misses, made to hand to another player; technically an invoice |
| **sheet** | аркуш | | vanilla parchment (`paper-parchment`) held by a book; one per invoice |
| **cover** | обкладинка | variant `<type>-<colour>` | the book's colour, to tell books apart; `normal-*` or `aged-*` |
| **collection** | колекція | `Collection` | a set of items worth collecting; tapestries are the first and only one so far |
| **member**, **piece** | шматок | | one item of a collection; for tapestries, one piece of a picture |
| **snapshot** | знімок | | the collection's members at the time an album was made or started over |
| **posting** | проведення | `InvoicePoster.Post` | carrying out an invoice on a container: the items move into the player's inventory |
| **cancelling** | анулювання | | dropping an invoice without moving anything |
| **taking new** | забрати нове для колекції | `InvoicePoster.Collect` | the album's gesture: take one of each missing member straight from a container |
| **origin** | | | where and when an invoice was made (day and coordinates; for information only) |

Notation below: **B** is the book's set or multiset, **C** the container's contents.

Old names (before 26.10.2026), still found in the archive: the mod was "Warehouse Ledger", the album "catalog",
the want list "collection invoice".

## Books

- Data lives in the item's attributes (under `curiocatalogue`), so a book can be handed over, stored, dropped, and survives
  everything a vanilla item does. The mode is the item's variant (its code), not an attribute.
- Holds up to **32** sheets (`MaxSheets`).
- Can be renamed; the name shows as the item's name. Goes on shelves like a vanilla book. Can be held in the **off-hand**.
- Entries are sample stacks with counts; an item's identity is recomputed on every read, so changing the ignored attributes in the
  config applies to existing books (entries that become equal merge). An entry of an item from a removed mod is kept; it just matches nothing.
- **Soft entry limit** (`SoftEntryLimit`, 500): posting an incoming invoice is never refused for it (entries are added, with a
  warning), but a book at or above the limit makes no incoming invoices ("the book is full"); outgoing ones still work, to unload it.
- Items of this mod (books, invoices) are never counted in containers.

## Making an invoice

Gesture: **book in the main hand + click on a container**, or **book in the off-hand + empty main hand + the same click**. The
second keeps the book in the off-hand all the time: the main hand is either empty (to make invoices) or holds an invoice (to post it).

| Invoice | Gesture | Ledger (types) | Bill of materials (counts) |
|---|---|---|---|
| incoming | **Shift + RMB** | C \ B: types in the container that the book lacks | surplus: C − B per type |
| outgoing | **Shift + Ctrl + RMB** | C ∩ B: types both have | min(C, B) per type |

- Plain RMB and Ctrl + RMB open the container as usual.
- Only the container's top level counts: a vessel or a backpack *inside* a chest is one item.
- One sheet is used. No sheets, nothing to list, or a full book (incoming): nothing happens, the player gets a red message.

## Posting an invoice

**Invoice in the main hand + Shift + RMB on any container**, not only the one it came from.

All or nothing, on the server:
1. check access to the container;
2. check the container has everything on the invoice, in full;
3. check the player's inventory has room for all of it;
4. only then move the items **into the player's inventory**, update the book, handle the sheet;
5. otherwise change nothing and say what stopped it ("Missing: …", "Not enough room in your inventory. Slots still needed: 2").

- **Where from:** smallest partial stacks first, then in slot order, so whole stacks stay in the chest.
- **What moves:** the container's real stacks (with their wear, temperature…), not copies of the invoice's samples.
- **Where to:** hotbar (not the off-hand) and backpack, respecting bag restrictions; partial stacks fill first. The invoice's own
  slot counts as free only when the invoice disappears. Stacks the game would not put together (a worn and a fresh ingot are one
  item for the book) need a slot each.
- If the game still moves less than planned (a bug), everything goes back as it was and the player is asked to report it.
- Any invoice can be posted with any book mode: a ledger takes the types, a bill of materials the counts.

The book in the off-hand:

| Off-hand | Book | Invoice |
|---|---|---|
| ledger or bill of materials | incoming adds entries; outgoing removes them (counts go down, not below zero; unknown entries ignored). Never fails the transaction. | sheet returns to the book, invoice disappears |
| the same, already 32 sheets | updated as above | becomes parchment |
| album, or nothing | unchanged | becomes parchment in the same slot |

With `ConsumeSheets` (hardcore option) the invoice just disappears: no sheet back, no parchment.

Results go to chat ("Posted. Lines: 5, items: 12.", entries before and after, where the sheet went); refusals are red in-game messages.

## Cancelling

A button in the invoice's window (with confirmation). Nothing moves. The sheet of any invoice, the want list included, returns
to a book **or album** in the off-hand; without one the invoice becomes parchment.

## Sheets

A sheet is vanilla parchment; there is no separate blank item.
- a book spends a sheet on each invoice (and an album on each want list);
- an invoice posted or cancelled with a book in the off-hand returns its sheet; without one it becomes parchment
  (dropped on the ground if the inventory is full);
- parchment recharges a book up to 32 (also when invoices got lost).

## Albums and collections

- An album is made from an **empty** ledger or bill of materials and a collection member (recipe below). It belongs to the
  collection of that member, remembers the collection's snapshot, and shows progress: pieces collected X of Y and whole pictures A of B
  (tapestries: 88 pieces, 31 pictures). It has sheets but no "entries" line.
- **Taking new** (Shift + RMB, same two gestures as a book): C ∩ missing, one of each, moved straight into the inventory and crossed
  off. No invoice, no sheet. If not all fits: what fits, in the collection's order (picture by picture, piece by piece); exactly what
  moved is crossed off ("Taken for the collection: 12. Still to collect: 40." + "Did not fit: 5; make room.").
- Shift + Ctrl + RMB with an album does nothing (the click goes to the game). An album never makes ordinary invoices, and posting
  an invoice with an album in the off-hand does not touch it.
- **Start over** (the album's "Clear"): every member is missing again; sheets and name stay.
- **An album follows its collection:** if the collection changed (config, a mod adding tapestries), the album reads as up to date:
  collected stays collected, new members are missing, gone members disappear. The stored data catches up the next time the server writes the album.
- **Want list:** made with a button in the album window; costs one album sheet, appears in the hand. Whoever holds it posts it with
  Shift + RMB on a container: it takes what it lists (one of each) into their inventory, as much as fits, and crosses off what moved;
  it stays for other chests. When empty, it becomes parchment. The mod only moves items; giving or trading is up to the players.
- A tapestry already on a wall gets into an album by taking it down into a chest once.

**Tapestry collection:** generated from the tapestry block's `sizes` attribute (31 pictures = 88 pieces; mods that extend
`sizes` are included). The member is a piece (`tapestry-north` + `type`, e.g. `ambush1`). Config: `TapestryExclude` (patterns
with `*`; candidates that may not appear in survival: `aid-*`, `glider*`, `symptoms*`) and `TapestryAdd`.

## Covers

- **Normal** (13): brickred, cherryred, darkbeige, darkgray, darkgreen, darkolive, gray, olive, orange, orangebrown, purple,
  purpleorange, teal. **Aged** (11): the same without teal and purple.
- The cover comes from the vanilla book in the recipe (normal → normal, aged → aged); a restored large book is darkgreen.
- **Recolour:** book + dyed leather `leather-normal-<colour>` → normal cover:

  | Leather | plain | orange | black | red | blue | purple | pink | white | yellow | gray | green |
  |---|---|---|---|---|---|---|---|---|---|---|---|
  | Cover | orangebrown | orange | darkgray | cherryred | teal | purple | purpleorange | darkbeige | olive | gray | darkgreen |

- **Age:** book + jug of sulfuric acid (1 L, the jug stays) → aged cover of the same colour (not for teal and purple).
- Recolouring and aging keep everything (entries, name, sheets). The colour list must stay stable: a removed variant turns such
  books in the world into unknown items. Covers are collapsed in the handbook and creative inventory (only `normal-*` shown).

## Recipes

Grid recipes in `recipes/grid/ledger.json`; the handbook shows each group as one slideshow.

| Group | Recipe | Result |
|---|---|---|
| 1 | fallback: vanilla `book-normal-*` / `book-aged-*` in the middle, iron, meteoric iron or steel `metalnailsandstrips-*` above and below, ink and quill right (a tool, not used up) | ledger, 8 sheets, cover from the book |
| 1 | restoration: a found large book (clutter `bookshelves/large-book-closed` or `-open`; must be repaired with glue to drop at all) + jug of lime water (1 L, jug stays) + ink and quill | ledger, 32 sheets, darkgreen |
| 2 | an empty ledger or bill of materials alone | the other mode (name and sheets kept) |
| 3 | book + parchment | sheets up to 32 in one craft; spare parchment stays in the grid; not with a full book |
| 4 | empty ledger or bill of materials + a tapestry piece (stays) | tapestry album (sheets carried over) |
| 5 | book or album + dyed leather (used up) | recolour |
| 6 | book or album + jug of sulfuric acid (1 L, jug stays) | aged cover |

- Both making recipes give a ledger; a bill of materials comes from switching. Switching and making an album need an empty book.
- The fallback takes any vanilla normal or aged book, even one with written text or found lore (`textCodes`): that text is lost
  (the ingredient check cannot see it); the handbook warns about it.

## Access and multiplayer

- Making, posting and taking need the same access as opening the container: land claims, padlocks and reinforcement, other mods'
  access checks. Checked on the server at every action; refused without revealing anything about the contents.
- Reach is the normal click reach. Several players at once are safe: each transaction is atomic on the server.

## Containers

- **Supported:** chests, trunks (two cells), labeled chests, stationary baskets, storage vessels, crates, and backpacks or baskets
  placed on the ground. Chests of other mods that derive from the vanilla classes work too.
- **Shift + RMB with a book or invoice:** on a supported container, ours; on another block with an inventory (oven, quern, barrel,
  shelf…), "not supported yet"; anywhere else, including ground storage without bags, the click goes to the game (e.g. putting the book down).
- **Not supported:** per-player loot chests from world generation ("each player can take…"): their slots show the total, not the player's share.
- Later (see `ROADMAP.md`): items on the ground, cabinets, shelves, chests on boats.

## Windows

- **Open:** RMB into the air with the item in the main hand, or with an empty main hand and a book in the off-hand.
- Read-only slot grids; data comes from the item's attributes and the window refreshes if the item changes while open, and closes
  if it leaves the hand or the main hand switches to another hotbar slot. Actions that change state (rename, Clear, Start over,
  Cancel, Make a want list) go to the server and name the item as the window shows it (hand, hotbar slot, data); if the hand no
  longer holds exactly that item, the server refuses with a message ("no longer the one in the window").
  Irreversible ones ask for confirmation.
- **Book window:** name (editable, Save), mode, sheets (e.g. 27/32), entries and the limit; search by name in the player's language;
  sort by name, by mod, by count (bill of materials only); "Clear". Album: progress line, the grid under "Missing:", "Start over", "Make a want list".
- **Invoice window:** type, origin (day and coordinates), lines and items, the grid with counts, "Cancel".
- **Tooltips:** book: mode, sheets, entries (album: sheets and progress); invoice: type and the first few items.
- **Hints:** at the bottom of the screen only "RMB: open"; the Shift gestures are hinted over a container while a book is held.

## Config

`ModConfig/curiocatalogue.json`, written back after every load (new settings appear with defaults, comments are not kept).

| Setting | Default | Meaning |
|---|---|---|
| `MaxSheets` | 32 | sheets a book can hold |
| `SoftEntryLimit` | 500 | soft limit of entries, see Books |
| `ExtraIgnoredAttributes` | `["durability"]` | attributes ignored when comparing items, on top of the game's own |
| `ConsumeSheets` | false | hardcore: a posted invoice never returns its sheet |
| `TapestryExclude`, `TapestryAdd` | empty | tune the tapestry collection |
| `Diagnostics` | false | debug lines in the game log (`docs/development.md`) |

## Scenarios

- **Stock-taking:** a ledger in the off-hand; Shift + RMB on chests lists what is new, posting moves it to you and records it.
- **Project:** a bill of materials as a shopping list that shrinks itself: write "4 X, 5 barrels", walk past chests making and posting
  outgoing invoices with the book in the off-hand; what remains is what is still missing. Without the book in the off-hand the same
  list works as a picking template and does not change.
- **Collection:** loot goes into an inbox chest → the album takes only what is new (duplicates stay) → hang it on the walls. Friends
  get a want list.
