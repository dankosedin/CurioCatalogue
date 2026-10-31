# Upgrade test: 0.1.0 → 0.1.1

Does a world made with 0.1.0 survive the update to 0.1.1? One such plan per release, made by copying the previous one
(`upgrade-0.1-to-0.2.md` holds the long form of the fixture and the run). This one checks only the move to 0.1.1.

**Read** before the release. **Update** while 0.1.1 is made: whenever a change touches stored data, a recipe or item code, a
config key or a lang key, add its case here in the same PR (the changelog's section for 0.1.1 is the checklist).

## Fixture and run

The same fixture as in `upgrade-0.1-to-0.2.md` (the 0.1.0 world, a fresh copy for every pass), the 0.1.1 release zip instead of
0.1.0, the old config kept. Pass it in single player and on the dedicated server (S).

## Cases

| # | Case | Expected | Result |
|---|---|---|---|
| U1 | Load the world | Loads; no `[CurioCatalogue]` errors, no "Failed to parse grid recipe", no "cannot be resolved" | — |
| U2 | Every book from the fixture (tooltip, window) | Same entries, name, sheets and cover as before | — |
| U3 | Creative inventory | Every cover of every book is listed (normal and aged); the books of the world are not unknown items | — |
| U4 | H over a book of the world with data, normal and aged | Its handbook page opens | — |
| U5 | The recipes of 0.1.0 (first ledger, restoring a large book, album, recolouring, aging) | Still craft | — |
| U6 | An intake list, a pick list and a want list made in 0.1.0, in inventory and in a chest (English) | Named Intake list / Pick list / Want list; they fill and cancel as before | — |
| U7 | Open the release zip; load the world | The zip holds `ExhaustiveMatching.dll` and `THIRD-PARTY-NOTICES.md` beside `curiocatalogue.dll`; the mod loads without "Could not load file or assembly" in the logs | — |

## Changes in 0.1.1 that need a case

| Change | Touches stored data, recipes, item codes, config or lang? | Case |
|---|---|---|
| Every cover in the creative inventory, `groupByByType` in `book.json` | Item JSON only: no codes, no stored data. The handbook pages and the recipe icon groups change | U3, U4 |
| Handbook links in `lang/*.json` | Lang values only, no keys | U4 |
| English item names and texts: Intake list, Pick list, Fill (S3) | Lang values only, no keys, no item codes; Ukrainian unchanged | U6 |
| `ExhaustiveMatching.dll` and `THIRD-PARTY-NOTICES.md` in the zip (S5) | The zip gains a library; no stored data | U7 |
| Clearer texts after the players' round (S8), en and uk | Lang values only, no keys | U6 |

## Outcome

Fill in at the release: date, the 0.1.1 build (zip or commit), who ran it, and a line for each `❌`.
