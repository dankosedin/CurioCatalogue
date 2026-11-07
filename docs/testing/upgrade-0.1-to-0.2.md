# Upgrade test: 0.1.1 → 0.2.0

Does a world made with 0.1.1 survive the update to 0.2.0? One such plan per release, made by copying the previous one; the
old ones stay as a record. The functional plan (`functional.md`) checks the new version itself; this one checks only the move.

**Read** before the release. **Update** while 0.2.0 is made: whenever a change touches stored data, a recipe or item code, a
config key or a lang key, add its case here in the same PR (the changelog's section for 0.2.0 is the checklist).

## Fixture: the 0.1.1 world

Made by `CurioCatalogue.GoldenCapture` with the **0.1.1 release zip** (not a dev build), then kept untouched; every pass starts from a fresh copy of it.
0.1.1, the latest release players have, not 0.1.0: the stored data is the same in both (`upgrade-0.1.0-to-0.1.1.md`), and
0.1.1 adds the item codes and texts players have now (the author, 04.11.2026).

The stored data of the same items is also checked automatically, on every pull request: golden stacks saved by 0.1.1 and read
by the server tests (`ROADMAP.md`, 0.2, item 1). This pass covers what they do not: the world, things lying in it, recipes in
the game, the dedicated server.

Keep it as a backup outside the game's data folder (copy it in for a pass, never open the original). It holds:

- the books (all in one chest, as the capture player's inventory cannot be kept): a ledger that learnt a chest, a bill of materials, the same
  book recoloured with black leather, a ledger aged with acid, a ledger of 32 sheets with 3 spent, one named `Комора № 1`, an album with
  3 tapestries collected, an album with every piece (88);
- the invoices of each kind (an intake list, a pick list, a want list of the partly full album), each in a chest, on a shelf and on the
  ground (ground items despawn after a while: look soon after loading);
- a chest with copper, tin and iron ingots, sticks and planks to post the invoices on;
- `ModConfig\curiocatalogue.json` as 0.1.1 wrote it on its first start, with no changes (the defaults: `MaxSheets` 32, `ConsumeSheets` false);
  the same file is in the golden set, so U2 checks that one.

The places of the blocks are in `fixture-layout.txt` next to the world (blocks float 6 above the spawn: fly there in creative).

The golden stacks of 0.1.1 (`CurioCatalogue.ServerTests/Golden/v0.1.1/`) are the same items as the fixture's, one per line, and what each covers:

| Golden stack | The fixture's item | Covers |
|---|---|---|
| `ledger` | the ledger that learnt a chest | entries by type; the intake action (posting an intake list gives 9 sheets) |
| `intake-list`, `pick-list` | the invoices of each kind | an invoice's kind and lines; posting on a chest |
| `materials` | the bill of materials | entries by count |
| `recoloured` | the book recoloured with black leather | the cover (the item's code) |
| `aged` | the ledger aged with acid | the aged cover |
| `spent` | the ledger of 32 sheets with 3 spent | sheets below the maximum; recharging in the grid |
| `named` | `Комора № 1` | a custom name, Cyrillic |
| `album-partial`, `want-list` | the album with 3 tapestries and its want list | the collection id, collected and missing counts; `members` (a collection that grew); a want list posted on a chest |
| `album-full` | the album with every piece (88) | full album |
| `curiocatalogue.json` | `ModConfig\curiocatalogue.json` | the config file 0.1.1 wrote, through the mod's loader |

The golden stacks cover the data of the items; the fixture covers the world around them. A case in the table below goes to the golden
set when it is about the data of an item, and stays a manual case when it is about the world (drawing, lying in a chest, the server).

If the fixture is lost, make it again with the 0.1.1 zip from the GitHub release `v0.1.1` and write down here what was in it.

## Run

1. Copy the fixture world; put the 0.2.0 release zip (not a dev build) in `Mods` instead of 0.1.1. Keep the old config.
2. Start the game, load the world. Check `server-main.log` and `client-main.log` first (see the table).
3. Go through the cases; then run a short pass of the functional plan on the same world (A1, A2, W1, L1, I1).
4. Repeat 2–3 on the dedicated server with the same world (S): the world on the server, the zip on both sides.

## Cases

| # | Case | Expected |
|---|---|---|
| U1 | Load the world | Loads; no `[CurioCatalogue]` errors or warnings about unknown items, no "Failed to parse grid recipe", no "cannot be resolved" |
| U2 ⚙ | Config of 0.1.1 | Read as it is; the new keys of 0.2.0 appear with defaults; the old values are kept. `TapestryExclude` and `TapestryAdd` are the exception: they move into `Collections.tapestries` (`Exclude`, `Add`) and the file loses them (K3; with non-empty lists too: `ConfigFileTests`) |
| U3 ⚙ | Look at every book from the fixture (tooltip, window) | Same counts, entries, name and cover colour as before |
| U4 ⚙ | Open the album window | Collected pieces and the missing ones as before; the collection counts match |
| U5 ⚙ | Post an invoice from the fixture (each kind) | Works as in 0.1.1; the book in the off-hand updates and gets the sheet back |
| U6 ⚙ | The want list from the fixture | Posts on the chest; finishes normally |
| U7 | Books and invoices lying in the world (chest, shelf, ground, inventory) | Drawn correctly, not as error items; nothing disappeared |
| U8 | Craft a book out of an old book (a book once could not be crafted from an old one) | Crafts; the result is a valid book |
| U9 | Old recipes | All three still craft: first ledger, restoring a large book, album |
| U10 ⚙ | The format version `v` of the old books | Read as version 1 and still work; if 0.2.0 changed the format, they are converted without losing entries |
| U11 | Save, quit, load again | Everything from U3 is still there |
| U12 | The other direction: the 0.2.0 world on 0.1.1 | Not supported; check only that 0.1.1 says "newer version" for a changed book and does not wipe it, if 0.2.0 changed the format |

A case marked ⚙ is also checked on every pull request by `UpgradeTests` on the golden stacks of 0.1.1 (`docs/tests.md`, Golden stacks): the data read and one action each, and the config file 0.1.1 wrote. For U3 and U4 that is the data the tooltip and the window show, not the tooltip or the window themselves. The pass by hand covers the world around them.

## Changes in 0.2.0 that need a case

Fill in while 0.2.0 is made. Take each line from the changelog or the PR and say which case above covers it, or add one.

| Change | Touches stored data, recipes, item codes, config or lang? | Case |
|---|---|---|
| Config: `TapestryExclude`, `TapestryAdd` move into `Collections.tapestries` (`Exclude`, `Add`; new `Disabled`), read once and removed from the file (K3) | config | U2 ⚙ (golden config; `ConfigFileTests` with non-empty lists), G3 |
| The album's stored format 2 (K4): what it collected by member id (with the item that filled it) and its level, no snapshot and no missing entries; `v` 2 (on albums and want lists only since K7b, below); an album of 0.1.1 is converted when read and written as 2 by the next server write, nothing collected lost (a piece the collection lacks at the time is kept and placed later) | stored data | U3 ⚙, U4 ⚙, U10 ⚙ (`UpgradeTests`: the golden albums read through the conversion, and written back), U12 (0.1.1 says "newer version" for an album the 0.2.0 server wrote) |
| New lang keys for albums with levels and a lost collection (`ledger-info-nolevel`, `album-restarted-nolevel`, `ledgerwindow-level-none`, `ingameerror-nolevel`, `ingameerror-levelfixed`); no key changed (K4) | lang | U1 (`AssetsTests`: both languages have them); T1 |
| A want list's stored format 2 (K5): the album's collection and level (`wants: { id, level }`) and the member each line names (`member`); a line of a member of several items is filled by any of them. A want list of 0.1.1 has neither and is read as exact lines, as before | stored data | U6 ⚙ (`UpgradeTests`: the golden want list filled from a real chest), U12 |
| New lang keys for a want list's line of a member of several items (`invoice-info-member-free`, `invoice-info-any-of`); no key changed (K5) | lang | U1 (`AssetsTests`); T1 |
| The resolver (K6): a collection switched off is told from one the game lacks. New lang keys `ingameerror-disabled`, `ledger-info-disabled`, `ledger-info-progress`, `album-name` (with the collection's name), `collection-tapestries`, `tapestries-progress`; removed `album-name-tapestries`, `ledger-info-album`, `ledgerwindow-collected` (their words moved into the definition's `nameKey` and `progressKey`; the album's shown name and its progress line read the same) | lang | U1 (`AssetsTests`); T1, G5 |
| The album recipe of a collection with `Disabled: true` is removed from the server's recipes at load (K6); with no collection disabled, no recipe changes | recipes | U9, G5 (`AlbumRecipesTests`, `RecipeTests`) |
| The server sends its `Collections` settings to each client on join (K6); no stored data | network | G5 |
| The format version by kind (K7b, H6): only albums and want lists of format 2 are written with `v` 2; ledgers, bills of materials, intake and pick lists (and want lists of 0.1.1) keep `v` 1, as their layout did not change | stored data | U12 (0.1.1 reads a ledger, a bill and an intake list the 0.2.0 server wrote; says "newer version" for an album and a want list), `LedgerStoreTests` |
| An album whose level the collection no longer has (format 1, or a level renamed by a patch) is placed on the level that takes most of what it collected (K7b, M1 and M3) | stored data | U10 ⚙ (the golden albums), `AlbumTests` |
| New lang key `ingameerror-wantlistdisabled` (K7b, W1): filling a want list of a collection switched off is refused; no key changed | lang | U1 (`AssetsTests`); W6 |
| The settings packet also carries `ExtraIgnoredAttributes` (K7b, A9/C7); the 0.1.1 config's empty `TapestryExclude` / `TapestryAdd` leave the file without a line in the log (H14) | network, config | G7; U2 ⚙ |

**The rule for the table:** a change that touches stored data, a recipe, an item code, a config key or a lang key gets its line in the same PR as
the change, and each line names a case. If the case is about an item's data, it is also made automatic: a stack of the golden set (the old sets are
never edited; a new item kind appears in the set of the release that has it) and a check in `UpgradeTests`, marked ⚙ above. A format change raises
`LedgerStore.Version` and keeps every old set green (`docs/tests.md`, Golden stacks). The table is read at the release: an empty
line is a change nobody covered.

## Outcome

The results go into the release pull request's description (`functional.md`, Results).
