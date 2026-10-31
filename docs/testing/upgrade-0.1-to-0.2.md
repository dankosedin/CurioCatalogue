# Upgrade test: 0.1.0 → 0.2.0

Does a world made with 0.1.0 survive the update to 0.2.0? One such plan per release, made by copying the previous one; the
old ones stay as a record. The functional plan (`functional.md`) checks the new version itself; this one checks only the move.

**Read** before the release. **Update** while 0.2.0 is made: whenever a change touches stored data, a recipe or item code, a
config key or a lang key, add its case here in the same PR (the changelog's section for 0.2.0 is the checklist).

## Fixture: the 0.1.0 world

Made once with the **0.1.0 release zip** (not a dev build), then kept untouched; every pass starts from a fresh copy of it.

Keep it as a backup outside the game's data folder (copy it in for a pass, never open the original). It holds:

- one ledger, one bill of materials and one album, each with entries; the album with some tapestries collected, a partly full
  one and a full one (32 sheets);
- a want list made from the album, and invoices of each kind (incoming, outgoing) lying in a chest, on a shelf and in the inventory;
- a book with a custom name, and one with a non-default cover colour;
- a book with `ConsumeSheets` spent (fewer sheets than the maximum);
- a chest with tapestries and mixed items to post invoices on;
- `ModConfig\curiocatalogue.json` of 0.1.0, with a few non-default values (`MaxSheets`, `ExtraIgnoredAttributes`, `TapestryAdd`).

If the fixture is lost, make it again with the 0.1.0 zip from the GitHub release `v0.1.0` and write down here what was in it.

## Run

1. Copy the fixture world; put the 0.2.0 release zip (not a dev build) in `Mods` instead of 0.1.0. Keep the old config.
2. Start the game, load the world. Check `server-main.log` and `client-main.log` first (see the table).
3. Go through the cases; then run a short pass of the functional plan on the same world (A1, A2, W1, L1, I1).
4. Repeat 2–3 on the dedicated server with the same world (S): the world on the server, the zip on both sides.

## Cases

| # | Case | Expected | Result |
|---|---|---|---|
| U1 | Load the world | Loads; no `[CurioCatalogue]` errors or warnings about unknown items, no "Failed to parse grid recipe", no "cannot be resolved" | — |
| U2 | Config of 0.1.0 | Read as it is; the new keys of 0.2.0 appear with defaults; the old values are kept | — |
| U3 | Look at every book from the fixture (tooltip, window) | Same counts, entries, name and cover colour as before | — |
| U4 | Open the album window | Collected pieces and the missing ones as before; the collection counts match | — |
| U5 | Post an invoice from the fixture (each kind) | Works as in 0.1.0; the book in the off-hand updates and gets the sheet back | — |
| U6 | The want list from the fixture | Posts on the chest; finishes normally | — |
| U7 | Books and invoices lying in the world (chest, shelf, ground, inventory) | Drawn correctly, not as error items; nothing disappeared | — |
| U8 | Craft a book out of an old book (a book once could not be crafted from an old one) | Crafts; the result is a valid book | — |
| U9 | Old recipes | All three still craft: first ledger, restoring a large book, album | — |
| U10 | The format version `v` of the old books | Read as version 1 and still work; if 0.2.0 changed the format, they are converted without losing entries | — |
| U11 | Save, quit, load again | Everything from U3 is still there | — |
| U12 | The other direction: the 0.2.0 world on 0.1.0 | Not supported; check only that 0.1.0 says "newer version" for a changed book and does not wipe it, if 0.2.0 changed the format | — |

## Changes in 0.2.0 that need a case

Fill in while 0.2.0 is made. Take each line from the changelog or the PR and say which case above covers it, or add one.

| Change | Touches stored data, recipes, item codes, config or lang? | Case |
|---|---|---|
| | | |

## Outcome

Fill in at the release: date, the 0.2.0 build (zip or commit), who ran it, and a line for each `❌`.
