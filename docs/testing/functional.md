# Functional test plan

What to check in the real game before every release. Unit tests cover the logic; this plan covers what only the game shows:
windows, gestures, chat messages, the server, other mods. It accumulates: a case stays until the feature is gone.

**Read** before a release (`docs/releasing.md`). **Update** in the same PR as the change:
a new feature adds its cases, a fixed bug adds the case that would have caught it. Mark each case with the version that
added it ("Since"). The upgrade between two versions has its own plan per release: `upgrade-<from>-to-<to>.md`.

## How to run

- Debug build, creative world; switch to survival where a case says so. Config `"Diagnostics": true` (`docs/development.md`).
- Dev commands (`/curio ...`, branch `dev-commands`) help to fill chests and books; see `docs/development.md`.
- After a case that changes the config, put the config back.
- A case marked ⚙ also has a server test (`BookFlowTests`, `BookEdgeTests`, `ContainerTests`, `CraftingTests`, `LanguageTests`, `ConfigFileTests`, `RefusalTests`, `CreativeInventoryTests`, `TooltipTests`, `VanillaContentTests`, `UpgradeTests`, `UpgradeableStorageTests`, `ModdedTapestryTests`; `docs/tests.md`, Server tests and Compatibility tests) that checks the state of the world
  and the refusals' codes. Check it in game only for what a client shows: texts in the player's language, windows, hints.
- Logs after the run: no `[CurioCatalogue]` errors, no "Failed to parse grid recipe", no "cannot be resolved".
- Cases marked **S** also run on the dedicated server (`ServerLanguage "en"`, client in Ukrainian): chat messages and red
  refusals must come in the player's language, not the server's.

## Smoke test

For a patch release (0.x.y) that changes no stored data, network messages or access checks (`docs/releasing.md`). Its new cases were checked in game in the sessions of their steps. With the release zip, single player:

1. The zip in the data folder's `Mods`, no `--addModPath`; load a world. The log has no "Could not load file or assembly",
   no "Failed to parse grid recipe", no "cannot be resolved", no `[CurioCatalogue]` errors.
2. A2: the album takes from a chest.
3. L1 and I1: an intake list from a ledger, filled with the ledger in the off-hand.
4. L2: a pick list.
5. W1 and W2: a want list filled to the end.

## Results

The results of a run go into the release pull request's description, not into these pages: which pass (full or smoke), on
which build, who ran it, and each `❌` with its reason.

## Crafting and the handbook

| # | Since | Case | Expected |
|---|---|---|---|
| C1 ⚙ | 0.1.0 | Vanilla book in the grid's middle, nails above and below, ink and quill to its right | A ledger with 8 sheets and the cover of the book used |
| C2 ⚙ | 0.1.0 | A large book from ruins, jug of lime water, ink and quill | A ledger with 32 sheets |
| C3 ⚙ | 0.1.0 | An empty ledger and a tapestry piece in the grid | An album; the piece stays |
| C4 ⚙ | 0.1.0 | Craft a book out of an old book (one with entries) | Crafts; no stack or attribute error in the log |
| C5 | 0.1.0 | Open the handbook | A Curio Catalogue guide and a page for every book, in both languages |
| C6 ⚙ | 0.1.1 | Creative inventory, search "ledger" (and "bill", "album") | Every cover is listed, normal and aged: 24 per kind, 72 books in all. ⚙ `CreativeInventoryTests` checks the tabs on the server (counts and tabs); the game check stays for the search box and the drawing |
| C7 | 0.1.1 | H over an empty aged ledger, and over one with entries (three different aged covers) | The book's own handbook page opens, with "Created by" and "Ingredient for"; not the handbook's main page |
| C8 | 0.1.1 | In the guide, click "ages its cover" (English and Ukrainian) | The page of an aged ledger opens |
| C9 | 0.1.1 | "Ingredient for" on a page of an aged book, then on a normal one | The aged page shows the aged covers' recipes, the normal page the normal covers'; recolouring takes aged books too (`DESIGN.md` 2.5) |

## The album

| # | Since | Case | Expected |
|---|---|---|---|
| A1 | 0.1.0 | Right click into the air with the album | The window opens: missing pieces, counts of pieces and whole pictures, search |
| A2 ⚙ | 0.1.0 | Shift + right click on a chest with tapestries, album in hand | One of each missing piece into the inventory; duplicates stay; message says how many |
| A3 ⚙ | 0.1.0 | Same with the album in the off-hand, main hand empty | Same result |
| A4 ⚙ | 0.1.0 | Same on a chest with nothing new | A message that nothing is new; nothing moves |
| A5 ⚙ | 0.1.0 | "Make a want list" in the window | A want list for the missing pieces; the album's sheets −1 |
| A6 ⚙ | 0.1.0 | Rename with a name longer than 40 characters | The field accepts 40 and no more |
| A7 ⚙ | 0.1.0 | Clear, with and without confirmation | "No" and "Cancel" change nothing; "Yes" empties the book |
| A8 | 0.1.0 | Open and close the window ~10 times, some with confirmations | The log line "loaded dialogs: N" does not grow |
| A9 | 0.1.0 | Open the window, then press a hotbar key or scroll | The window closes |
| A10 ⚙ | 0.1.0 | Open the window of book 1, switch to book 2, press Clear → Yes | Red "the item in the hand is not the one in the window"; book 2 untouched (or the window closed first: also fine) |
| A11 ⚙ | 0.1.0 | A tapestry from another mod, in the game's tapestry list | The album counts it |
| A12 | 0.2.0 | Open an empty album of tapestries (en, then uk) | The title bar "Album: tapestries"; our row with "No name", the pencil, the bar at 0/88, the sheets; pages of pictures with every piece a dim silhouette; ◀ 1 / M ▶ turns the pages |
| A13 | 0.2.0 | The pencil: type a name, Enter; again, type, Escape; a name of 40 characters | The first shows the name in the window and on the item in the hotbar; Escape keeps the old name and the window open; 40 characters are cut with "…", the full name on hover |
| A14 | 0.2.0 | Window open, "Make a want list" (an open window takes the mouse, so a chest cannot be clicked with it open) | The sheets' count drops by one in place, nothing flickers; the window stays open |
| A15 | 0.2.0 | An album partly full, then full | The group's "n/m" in its heading, ✓ on a complete group's heading and tab; the bar full at the end |
| A16 | 0.2.0 | An album of a collection with levels (ore crystals, three levels: "The richest of each ore", "Each ore in each rock", "Every crystal"), empty | The level dropdown and "Choose a level to start collecting", no bar; after a level is chosen: tabs, the layout dropdown, the bar; hover the bar: the level's name above the progress line |
| A17 | 0.2.0 | Switch the layout with a page scrolled to a piece | The first piece of the page stays in view |
| A18 | 0.2.0 | Type in the search; Enter | The cells that do not match are dimmed; Enter turns to the next page with a match |
| A19 | 0.2.0 | A piece with a long name, in en and in uk | The name is cut with "…" in group headings and headings in words; the full name on hover; the cut does not change as pieces come |
| A20 | 0.2.0 | Window at 1024×768 | The page and every row fit on the screen |
| A21 | 0.2.0 | An album whose collection is switched off (config `Collections`) | One group of what was collected; no level, layout, want list or ↺; the text says the collection is switched off on this server |
| A22 | 0.2.0 | An album with tabs (ore crystals at the level "Every crystal", by ore and by rock; the level "Each ore in each rock" is one table of two pages with no tabs); turn pages with ◀ ▶, pick tabs; then a screen too short for the tabs (1024×768) | Each tab starts a page of its own and only its tab is lit, the window as tall as its tabs; "N / M" counts the pages of the tab and ◀ ▶ stay in it (no page buttons for a tab of one page); tabs taller than the screen allows are a dropdown in the row of controls, right after the level (while the album is empty), a complete tab with ✓ in its name (a box instead of ✓: the font lacks the glyph) | Column headings of ores are short ("Au in quartz"), cut with … when still too long, the full name on hover
| A23 ⚙ | 0.2.0 | An album at a level whose members take any of several items (seashells by type, once item 4 brings them), the backpack full except a stack of one colour of a shell with room; Shift + right click on a chest with that shell in two colours, the other one first in the member's order. The same with a want list of the album | The colour there is room for is taken (topping up the stack) and the member collected, or its line crossed off; not "no room". ⚙ `InvoicePosterTests` (V1 of the vanilla collections plan) |
| A24 | 0.2.0 | An album made from a crystal: an empty ledger (or bill of materials) and a crystal of any ore in the grid, in `en` and `uk`; the handbook; then a want list of the album at the level "The richest of each ore", its tooltip, the tooltip of a cell in the window and a slot of the want list window (gold in quartz: "any rock") | The recipe gives an album of ore crystals and the crystal stays; its handbook page is in the albums group; the album, level names, tabs and ore names read well in both languages (ores as headings, not the game's genitive); a want list line names the crystal by its ore and the rock when one rock holds it ("Native copper, basalt"), "any rock" otherwise; members' names in full: A29. ⚙ `CrystalsTests`, `CraftingTests` |
| A25 | 0.2.0 | An album made from a seashell: an empty ledger (or bill of materials) and a shell from the creative inventory or the beach in the grid, in `en` and `uk`; the handbook; choose the level "Every type", then "Every type in every colour"; shift + right click on a chest of shells of several colours; a want list of the album at the level "Every type", its tooltip | The recipe gives an album of seashells and the shell stays; its handbook page is in the albums group; level names, types and colours read well in both languages ("мушлі", no "черепашка" for the collection; the 7 types have different names); at "Every type" a shell of any colour fills its type (one per type, the second of a type stays in the chest); at the second level the window is one table, types as rows and colours as columns; a want list line ends with "any colour" ("будь-якого кольору"). Rarity (seafoam, turquoise) is data only: nothing marks a cell yet. ⚙ `SeashellsTests`, `CraftingTests` |
| A26 | 0.2.0 | An album made from a dead butterfly: an empty ledger (or bill of materials) and a butterfly from the creative inventory or the bug net in the grid, in `en` and `uk`; the handbook; open the album (no level to choose), turn the pages; shift + right click on a chest of butterflies with two of one kind; a want list of the album, its tooltip | The recipe gives an album of butterflies and the butterfly stays; its handbook page is in the albums group; the album starts at its only level (no choice), 169 cells over pages, each cell the game's own name of the butterfly ("Dead Monarch Butterfly"), the progress line "Butterflies N of 169" / "Метеликів N з 169"; one butterfly of a kind is taken, the second stays in the chest; a want list line names the butterfly. Grouped by family and species (A32). ⚙ `ButterfliesTests`, `CraftingTests` |
| A27 | 0.2.0 | The album window's width (L6 of the album polish plan): butterflies; crystals at "The richest of each ore" (album empty, so the level dropdown shows), at "Each ore in each rock", and at "Every crystal" by ore and by rock, turning tabs; seashells at both levels; tapestries; in `en` and `uk` | The window is one block: no empty strip on the right of the cells (butterflies fill the width, 13 cells a line); members in order spread evenly over their lines (the richest crystals 8 + 8, not 12 + 4); a page with no frames narrower than the window stands centred; a tab with one group has its dark frame out to the window's right edge and down to the controls, with the same margins as a frame round a group; the rows of controls end at the window's right edge. The dropdowns (ours since L2c) hold their longest name clear of the arrow. ⚙ `AlbumPagesTests` |
| A28 | 0.2.0 | Icons for words in the crystal album (L2b of the album polish plan): "Each ore in each rock"; "Every crystal" by ore and by rock; hover over each kind of heading; in `en` and `uk` | Ores head their rows and columns as the game's nuggets (gold in quartz as the gold nugget, both silvers as the silver nugget), grades as four yellow round marks, bountiful (large, full yellow) on top down to poor (small, dim) at the bottom, rocks as stones as before; no heading in words is cut with "…"; hovering a nugget or a stone shows the value's full name ("Native silver in galena"), a round mark its grade's name. ⚙ `VanillaContentTests` (every icon is in the game), `AlbumPagesTests` |
| A29 | 0.2.0 | Members named by their values, in `en`, then `uk`: an album of tapestries, of crystals at each of the three levels, of seashells at both levels and of butterflies; hover a missing and a collected cell of each; make a want list of each, look at its tooltip and open its window, hover a slot | The cell's tooltip: the stack's own name as the title, then the member's name, then "Missing" / "Collected": "Ambush, left third" / "Засідка, ліва третина" (a picture of one piece: its name alone); crystals "Native copper, any rock" (the richest), "Native copper, basalt" (each ore in each rock), "Native copper, bountiful, basalt" / "Самородна мідь, щедра, базальт" (every crystal); seashells "Scallop, any colour" / "Гребінець будь-якого кольору", "Scallop, seafoam" / "Гребінець, морська піна"; parts after the first in lower case. Butterflies: no name line, the title is the name. The want list's tooltip lines say the same names (butterflies: the game's name); a slot of its window says the name over the item's own text (butterflies: nothing). Group headings, tabs and table headings read as before. ⚙ `TooltipTests`, `CrystalsTests`, `SeashellsTests`, `ButterfliesTests` |
| A30 | 0.2.0 | The dropdowns and tabs with icons (L2c of the album polish plan), in `en` and `uk`: an empty crystal album (the level dropdown), open it, choose "Every crystal"; the layout dropdown, by ore then by rock; the side tabs; a screen too short for the tabs (1024×768: the tabs as a dropdown), open it, scroll it with the wheel, Up, Down, Home, End, Enter; Escape with a list open, then again; the wheel over the closed box after a click has opened and closed it; hover the page under an open list; an empty seashell album (both levels) | Each choice has its icon before its name, closed and open (crystals: a bountiful copper crystal, granite, a medium crystal; by ore a copper nugget, by rock granite; seashells: a plain and a turquoise scallop; "Choose a level" with no icon, its name in line with the others); the side tabs show their nugget or stone before the name, the ✓ still beside a complete tab; the open list stands under the box when it fits, else over the page, never off the screen, scrolled with a thin bar at its right when long; the keys and the wheel move through it, Enter takes the line lit; the wheel over the focused closed box takes the next choice; the first Escape closes the list and keeps the window, the second closes the window; no cell tooltip shows through the list; a click outside closes it. The level dropdown of an empty album whose level is chosen, and the layout dropdown always, show their icon alone, the name on hover. Where the row is wider than the page, a closed dropdown whose choices all have icons shows its icon alone, its name on hover. ⚙ `DropDownListTests` |
| A31 | 0.2.0 | The rarity corner of seashells (L3 of the album polish plan), in `en` and `uk`: a seashell album at "Every type in every colour" with some shells collected; hover a seafoam and a turquoise shell, collected and missing, then a plain one; switch to "Every type" | The seafoam and turquoise columns (14 cells) have a small golden triangle in the bottom right corner of each cell, collected or missing, over the shell; no other cell has one; the tooltip of a marked cell says "Rare colour" / "Рідкісний колір" in gold under "In the collection" or "Missing"; a plain shell's tooltip has no such line; at "Every type" no cell has a corner. ⚙ `SeashellsTests`, `DefinedCollectionTests`, `CollectionRegistryTests` |
| A32 | 0.2.0 | The butterflies grouped (L4b of the album polish plan), in `en`, then `uk`: an album of butterflies with a few collected (a monarch, a peacock, both Ornithoptera priamus males); open it, go through the tabs by family; hover a cell; switch the layout to by rarity and go through its tabs; a want list of the album | By family (the layout's icon a swallowtail): seven side tabs in this order, each with a sample butterfly as its icon: Swallowtails, Skippers, Whites and sulphurs, Metalmarks, Gossamer-winged butterflies, Brush-footed butterflies, Moths / Косатцеві, Товстоголовки, Біланові, Ріодиніди, Синявцеві, Сонцевики, Нічні метелики; in a tab a frame for each species, by Latin name (A to Z), headed by the Latin binomial alone, the same in both languages ("Ornithoptera priamus", four cells; "Polygonia c-album" with the comma and its aberration), not "family · species". By rarity (the icon a birdwing): six tabs, Legendary 6, Very rare 7, Rare 19, Scarce 38, Uncommon 44, Common 55 / Легендарні, Дуже рідкісні, Рідкісні, Нечасті, Незвичайні, Звичайні; each tab one frame named by its step. A cell's tooltip and the want list lines name the butterfly by the game's own name, as before (A29). ⚙ `ButterfliesTests`, `AlbumPagesTests` |

## Want lists

| # | Since | Case | Expected |
|---|---|---|---|
| W1 ⚙ | 0.1.0 | Want list in hand, Shift + right click on a friend's chest that has some of the pieces | What is there and fits moves; the list gets shorter |
| W2 ⚙ | 0.1.0 | Fill it until it is finished, `ConsumeSheets` false, album in off-hand | "Taken from the list: N. The list is done and is now parchment."; the list is parchment (a want list goes back into no book, `docs/domain.md`); the album's sheets unchanged |
| W3 ⚙ | 0.1.0 | Same with `ConsumeSheets` true | "…The list is done."; the list is gone, no parchment; the album's sheets unchanged |
| W4 ⚙ | 0.1.0 | Fill with no room for one piece | Partial fill; the list keeps the piece that did not fit |
| W5 ⚙ | 0.2.0 | An album at a level whose members take any of several items (crystals by ore at the level "The richest of each ore"; seashells by type); make a want list, look at its tooltip, fill it on a chest with a piece of another kind than the one shown (another colour, or another rock for a crystal of quartz gold), in `en` and `uk` | Each such line names the member and ends with what is free ("Scallop, any colour"); the piece of the other kind is taken and its line crossed off. `CollectionsFromAssetsTests`, `CrystalsTests` and `SeashellsTests` check it |
| W6 ⚙ | 0.2.0 | A want list made by an album, the server's `Collections.tapestries.Disabled: true` (restart); Shift + RMB with the list on a chest that has a piece of it; then `Disabled: false`, restart, again | No "Take from the list" hint over the chest (the client's hints); red "This want list's collection is switched off on this server"; the chest and the list unchanged. Switched on, the list fills as usual (`InvoicePosterTests`) |

## The ledger and the bill of materials

| # | Since | Case | Expected |
|---|---|---|---|
| L1 ⚙ | 0.1.0 | Ledger, Shift + right click on a chest | An intake list for the kinds the ledger lacks; the ledger's sheets −1 |
| L2 ⚙ | 0.1.0 | Ledger, Shift + Ctrl + right click on a chest | A pick list for the kinds the ledger lists |
| L3 ⚙ | 0.1.0 | Bill of materials: the same two gestures | Lists by count; the book shrinks as items are taken |
| L4 ⚙ | 0.1.0 | Window: search, sorting, rename, clear | Work as in A1, A6, A7 |
| L5 ⚙ | 0.1.0 | `SoftEntryLimit: 3`; a book with 2 entries, intake list with 1 new item | Filled, warning "limit 3"; the next one says the book is full |

## Lists

| # | Since | Case | Expected |
|---|---|---|---|
| I1 ⚙ | 0.1.0 | List in hand, Shift + right click on a chest that has everything | All items into the inventory; the book in the off-hand is updated and the sheet comes back |
| I2 ⚙ | 0.1.0 | Same, with something missing | "Missing"; nothing moves |
| I3 ⚙ | 0.1.0 | No book in the off-hand | The list becomes parchment |
| I4 ⚙ | 0.1.0 | Album in the off-hand, full (32 sheets) | Parchment; the message says the book already has 32 sheets |
| I5 ⚙ | 0.1.0 | Inventory with one free slot, list for 3 plain + 3 damaged iron ingots (`/giveitem ingot-iron 3 {durability:5}`) | Refused "not enough room (1 slot short)"; nothing moves |
| I6 ⚙ | 0.1.0 | List filled on a furnace, quern, barrel | Red "This container is not supported yet" (also from the off-hand) |
| I7 ⚙ | 0.1.0 | Per-player loot chest in ruins | Refused |
| I8 ⚙ | 0.1.0 | Shift + Ctrl + right click with an album in hand | The mod does nothing; the game handles the click |
| I9 ⚙ | 0.1.0 | Plain right click and Ctrl + right click on a chest | Open it as usual |
| I10 ⚙ | 0.1.0 | Put a list on the ground, on a shelf | Behaves as an ordinary item |
| I11 ⚙ | 0.1.1 | A blank intake list, a blank pick list and a blank want list from the creative inventory (`/giveitem` or the tab), a ledger in the off-hand; Shift + right click on a chest | Red "Nothing to fill: …" (Ukrainian too); the list stays in the hand, the book keeps its sheets, nothing moves. Then Cancel in its window, with the ledger in the off-hand and again without one: "The list is cancelled."; the list is gone, the book keeps its sheets, no parchment |
| I12 ⚙ | 0.1.1 | Make a want list from an album; cancel it in its window three ways: with an album in the off-hand, with a ledger in the off-hand, with nothing in the off-hand. Then the same with an intake list, in `en` and `uk` | Want list: "The want list is cancelled, its sheet is back in the album." / "…in the book." / "…and is now parchment." (uk: "Список бажаного скасовано, …"); intake list keeps "The list is cancelled…" (uk: "Накладну анульовано…"). The sheet and the parchment go as before |

## Access (S)

| # | Since | Case | Expected |
|---|---|---|---|
| X1 ⚙ | 0.1.0 | A chest in another player's land claim, no `use` rights | All gestures refused, as opening by hand |
| X2 ⚙ | 0.1.0 | Same after `grant <name> use` | Works |
| X3 ⚙ | 0.1.0 | A padlocked or reinforced chest | Refused |
| X4 ⚙ | 0.1.0 | A chest out of reach (request from a window or the off-hand) | Refused |
| X5 ⚙ | 0.1.0 | The book changes in the hand during a request | Red refusal; nothing changes |

## Containers

| # | Since | Case | Expected |
|---|---|---|---|
| K1 ⚙ | 0.1.0 | Chest, trunk, labeled chest, basket, storage vessel, crate, a backpack and a basket on the ground | Each supported |
| K2 ⚙ | 0.1.0 | Upgradeable Storage chest | Works |
| K3 | 0.1.0 | Carry On: carry a chest, put it down | No conflict with our gestures |
| K4 | 0.1.0 | Better Crates | Not supported, as in the README's list of limitations |

## Config

| # | Since | Case | Expected |
|---|---|---|---|
| G1 ⚙ | 0.1.0 | `"MaxSheets": 0`, restart | Server log warning "MaxSheets 0 is below 1, using 1"; the file now says 1. Put back 32 |
| G2 ⚙ | 0.1.0 | `ExtraIgnoredAttributes`: `[]` and `["durability"]` | Damaged and plain items count as different, or the same |
| G3 ⚙ | 0.1.0 | `Collections.tapestries.Exclude` / `Add` (before 0.2: `TapestryExclude` / `TapestryAdd`, which move there on the first start and leave the file) | The collection changes accordingly |
| G4 ⚙ | 0.1.0 | `Diagnostics` true / false | Debug lines appear / do not |
| G5 | 0.2.0 | Dedicated server (`VintageStory-ServerData`), a client joined: set `Collections.tapestries.Disabled: true` and restart the server only. Then, on the client: hold an album made before, open its window, Shift + RMB on a chest; look for the album recipe in the handbook and the grid | The album says "switched off on this server" (tooltip and window), the chest click and "Make a want list" give the red "switched off on this server" (not "no longer in the game"); the album is untouched; no album recipe in the handbook or grid; no "Take new items for the collection" hint over a chest while the album is in hand (the client's hints; not unit-testable). Set `Disabled: false` and restart: the album works again, its collected pieces still counted |
| G6 | 0.2.0 | Same server, `Collections.tapestries.Exclude: ["aid-*"]` on the server only (the client's own file unchanged), restart the server, rejoin | The client's album counts the same pieces as the server's (no `aid-*` among the missing); the client's log has "the server's collection settings: tapestries" (with `Diagnostics` on). Put the config back |
| G7 | 0.2.0 | Dedicated server (`VintageStory-ServerData`): `ExtraIgnoredAttributes: []` on the server only (the client's own file keeps `["durability"]`), restart the server, rejoin. A chest with two copper pickaxes, one new and one worn; Shift + RMB on it with an empty bill of materials, fill the intake list with the bill in the off-hand, then open the bill's window | The bill lists the two pickaxes as two lines (the server's setting: wear counts), not one line of 2 as the client's own file would. Put the config back (`RefusalTests` checks that the packet carries the setting) |

## Languages (S)

| # | Since | Case | Expected |
|---|---|---|---|
| T1 | 0.1.0 | Switch the client between English and Ukrainian | All texts of the mod follow, no raw keys |
| T2 ⚙ | 0.1.0 | Dedicated server in English, Ukrainian client: A2, A10, I6 | Chat and refusals in Ukrainian; item names in the player's language |

## Not reproducible in game (unit and server tests only)

- `newerversion`: a book or invoice from a newer mod version. ⚙ `RefusalTests` (every gesture and window request, the stacks written by the test).
- `movefailed`: the game accepts only part of the items (the post rolls back). Unit test only: no container of the server accepts part of a stack.
- `nocollection`: "Start over" for an album whose collection no longer exists. ⚙ `RefusalTests`; taking new and a want list from
  such an album, ⚙ `InvoicePosterTests`, `InvoiceMakerTests`.
- `nolevel`, `levelfixed`: an album of a collection with several levels (none in the game before the collections of 0.2's item 4)
  takes nothing and makes no want list until a level is chosen in its window; the first piece fixes the level, "Start over" frees
  it. ⚙ `CollectionsFromAssetsTests` (a test mod's collection of two levels), `AlbumTests`. In game with the first collection of levels.
- A broken collection definition (bad JSON, a pattern naming a dimension twice, `max` on a dimension with no `values`, a block
  whose `sizes` is of another shape, a null in the config's lists) is logged once and skipped; the other collections work, and a
  good copy of the same id from another mod is used. Another mod's album recipe, named by its definition, makes its album and is
  taken out when its collection is switched off. ⚙ `CollectionRegistryTests`, `AlbumRecipesTests`, `ConfigTests`, `RecipeTests` (0.2.0, K7).
