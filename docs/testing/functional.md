# Functional test plan

What to check in the real game before every release. Unit tests cover the logic; this plan covers what only the game shows:
windows, gestures, chat messages, the server, other mods. It accumulates: a case stays until the feature is gone.

**Read** before a release (`docs/development.md`, Branches, PRs and releases). **Update** in the same PR as the change:
a new feature adds its cases, a fixed bug adds the case that would have caught it. Mark each case with the version that
added it ("Since"). The upgrade between two versions has its own plan per release: `upgrade-<from>-to-<to>.md`.

## How to run

- Debug build, creative world; switch to survival where a case says so. Config `"Diagnostics": true` (`docs/development.md`).
- Dev commands (`/curio ...`, branch `dev-commands`) help to fill chests and books; see `docs/development.md`.
- After a case that changes the config, put the config back.
- Logs after the run: no `[CurioCatalogue]` errors, no "Failed to parse grid recipe", no "cannot be resolved".
- Cases marked **S** also run on the dedicated server (`ServerLanguage "en"`, client in Ukrainian): chat messages and red
  refusals must come in the player's language, not the server's.

## Results

The "Last run" column holds the version and the outcome of the latest full pass, e.g. `0.2.0 ✅`; `—` means never run.
Fill it in the PR of the release; older runs are in git history. A `❌` gets a line under its table with the reason.

## Crafting and the handbook

| # | Since | Case | Expected | Last run |
|---|---|---|---|---|
| C1 | 0.1.0 | Vanilla book in the grid's middle, nails above and below, ink and quill to its right | A ledger with 8 sheets and the cover of the book used | — |
| C2 | 0.1.0 | A large book from ruins, jug of lime water, ink and quill | A ledger with 32 sheets | — |
| C3 | 0.1.0 | An empty ledger and a tapestry piece in the grid | An album; the piece stays | — |
| C4 | 0.1.0 | Craft a book out of an old book (one with entries) | Crafts; no stack or attribute error in the log | — |
| C5 | 0.1.0 | Open the handbook | A Curio Catalogue guide and a page for every book, in both languages | — |

## The album

| # | Since | Case | Expected | Last run |
|---|---|---|---|---|
| A1 | 0.1.0 | Right click into the air with the album | The window opens: missing pieces, counts of pieces and whole pictures, search | — |
| A2 | 0.1.0 | Shift + right click on a chest with tapestries, album in hand | One of each missing piece into the inventory; duplicates stay; message says how many | — |
| A3 | 0.1.0 | Same with the album in the off-hand, main hand empty | Same result | — |
| A4 | 0.1.0 | Same on a chest with nothing new | A message that nothing is new; nothing moves | — |
| A5 | 0.1.0 | "Make a want list" in the window | A want list for the missing pieces; the album's sheets −1 | — |
| A6 | 0.1.0 | Rename with a name longer than 40 characters | The field accepts 40 and no more | — |
| A7 | 0.1.0 | Clear, with and without confirmation | "No" and "Cancel" change nothing; "Yes" empties the book | — |
| A8 | 0.1.0 | Open and close the window ~10 times, some with confirmations | The log line "loaded dialogs: N" does not grow | — |
| A9 | 0.1.0 | Open the window, then press a hotbar key or scroll | The window closes | — |
| A10 | 0.1.0 | Open the window of book 1, switch to book 2, press Clear → Yes | Red "the item in the hand is not the one in the window"; book 2 untouched (or the window closed first: also fine) | — |
| A11 | 0.1.0 | A tapestry from another mod, in the game's tapestry list | The album counts it | — |

## Want lists

| # | Since | Case | Expected | Last run |
|---|---|---|---|---|
| W1 | 0.1.0 | Want list in hand, Shift + right click on a friend's chest that has some of the pieces | What is there and fits moves; the list gets shorter | — |
| W2 | 0.1.0 | Post it until it is finished, `ConsumeSheets` false, album in off-hand | The list is gone, "Taken from the list: N. List done."; no parchment; the album's sheets +1 | — |
| W3 | 0.1.0 | Same with `ConsumeSheets` true | The list is gone; the album's sheets unchanged | — |
| W4 | 0.1.0 | Post with no room for one piece | Partial post; the list keeps the piece that did not fit | — |

## The ledger and the bill of materials

| # | Since | Case | Expected | Last run |
|---|---|---|---|---|
| L1 | 0.1.0 | Ledger, Shift + right click on a chest | An incoming invoice for the kinds the ledger lacks; the ledger's sheets −1 | — |
| L2 | 0.1.0 | Ledger, Shift + Ctrl + right click on a chest | An outgoing invoice for the kinds the ledger lists | — |
| L3 | 0.1.0 | Bill of materials: the same two gestures | Invoices by count; the book shrinks as items are taken | — |
| L4 | 0.1.0 | Window: search, sorting, rename, clear | Work as in A1, A6, A7 | — |
| L5 | 0.1.0 | `SoftEntryLimit: 3`; a book with 2 entries, incoming invoice with 1 new item | Posted, warning "limit 3"; the next one says the book is full | — |

## Invoices

| # | Since | Case | Expected | Last run |
|---|---|---|---|---|
| I1 | 0.1.0 | Invoice in hand, Shift + right click on a chest that has everything | All items into the inventory; the book in the off-hand is updated and the sheet comes back | — |
| I2 | 0.1.0 | Same, with something missing | "Missing"; nothing moves | — |
| I3 | 0.1.0 | No book in the off-hand | The invoice becomes parchment | — |
| I4 | 0.1.0 | Album in the off-hand, full (32 sheets) | Parchment; the message says the book already has 32 sheets | — |
| I5 | 0.1.0 | Inventory with one free slot, invoice for 3 plain + 3 damaged iron ingots (`/giveitem ingot-iron 3 {durability:5}`) | Refused "not enough room (1 slot short)"; nothing moves | — |
| I6 | 0.1.0 | Invoice posted on a furnace, quern, barrel | Red "This container is not supported yet" (also from the off-hand) | — |
| I7 | 0.1.0 | Per-player loot chest in ruins | Refused | — |
| I8 | 0.1.0 | Shift + Ctrl + right click with an album in hand | The mod does nothing; the game handles the click | — |
| I9 | 0.1.0 | Plain right click and Ctrl + right click on a chest | Open it as usual | — |
| I10 | 0.1.0 | Put an invoice on the ground, on a shelf | Behaves as an ordinary item | — |

## Access (S)

| # | Since | Case | Expected | Last run |
|---|---|---|---|---|
| X1 | 0.1.0 | A chest in another player's land claim, no `use` rights | All gestures refused, as opening by hand | — |
| X2 | 0.1.0 | Same after `grant <name> use` | Works | — |
| X3 | 0.1.0 | A padlocked or reinforced chest | Refused | — |
| X4 | 0.1.0 | A chest out of reach (request from a window or the off-hand) | Refused | — |
| X5 | 0.1.0 | The book changes in the hand during a request | Red refusal; nothing changes | — |

## Containers

| # | Since | Case | Expected | Last run |
|---|---|---|---|---|
| K1 | 0.1.0 | Chest, trunk, labeled chest, basket, storage vessel, crate, a backpack and a basket on the ground | Each supported | — |
| K2 | 0.1.0 | Upgradeable Storage chest | Works | — |
| K3 | 0.1.0 | Carry On: carry a chest, put it down | No conflict with our gestures | — |
| K4 | 0.1.0 | Better Crates | Not supported, as in the README's list of limitations | — |

## Config

| # | Since | Case | Expected | Last run |
|---|---|---|---|---|
| G1 | 0.1.0 | `"MaxSheets": 0`, restart | Server log warning "MaxSheets 0 is below 1, using 1"; the file now says 1. Put back 32 | — |
| G2 | 0.1.0 | `ExtraIgnoredAttributes`: `[]` and `["durability"]` | Damaged and plain items count as different, or the same | — |
| G3 | 0.1.0 | `TapestryExclude` / `TapestryAdd` | The collection changes accordingly | — |
| G4 | 0.1.0 | `Diagnostics` true / false | Debug lines appear / do not | — |

## Languages (S)

| # | Since | Case | Expected | Last run |
|---|---|---|---|---|
| T1 | 0.1.0 | Switch the client between English and Ukrainian | All texts of the mod follow, no raw keys | — |
| T2 | 0.1.0 | Dedicated server in English, Ukrainian client: A2, A10, I6 | Chat and refusals in Ukrainian; item names in the player's language | — |

## Not reproducible in game (unit tests only)

- `newerversion`: a book from a newer mod version.
- `movefailed`: the game accepts only part of the items (the post rolls back).
- `nocollection`: "Start over" for an album whose collection no longer exists.
