# Backward compatibility: golden stacks of 0.1.1

Started 04.11.2026. `ROADMAP.md`, 0.2, item 1: before anything in 0.2 changes the stored data (the collection registry, the
album's dimensions), the items players have now are frozen as **golden stacks** and read by a server test on every pull
request. Today only the format version `v` is tested (`RefusalTests`, `LedgerStoreTests`), on stacks our current code writes;
nothing checks what 0.1.1 wrote. No change to the mod itself.

The golden stacks are made by **the code of the release**: a capture project starts the test server (`CurioCatalogue.TestServer`)
with the mod of the v0.1.1 release zip instead of the current build, makes the items through the game's API (gestures, the
crafting grid, our network requests) and writes them out. It is run by hand, once per release, not in CI. The same run may
leave the **fixture world** for the manual upgrade pass (`docs/testing/upgrade-0.1-to-0.2.md`), kept outside the repo.

## How to work on this plan

As in the previous plans: one step per short session with the project skill `/step <id>` (`.claude/skills/step/`). Each step
says **Branch**, **After**, **Model**, **Author** (a decision or in-game work only the author can do; "none" means the session
goes ahead on its own) and **Done when**. A session that stops in the middle writes one line under "Where we stopped".

The agent does not run the game: what only the game client can show (the fixture world opened in game) is the author's, with
exact steps written by the agent.

## Steps

- [ ] **B1. The release's code in the test server.** Research and a spike, kept only if it works: answer the open questions of
      Decisions ("Capture", "Format", "Ids") in the engine (vsapi, decompiled `VintagestoryLib.dll` with `ilspycmd`) and by
      trying. The fixture loads the mod from the v0.1.1 zip (`Releases/curiocatalogue_0.1.1.zip`, the same file as the GitHub
      release `v0.1.1`); the capture project compiles against that zip's DLLs, not the current build (the tests bind to the
      loaded assembly by name, `TestServer.props`), so it may use only the types 0.1.1 has. What `ItemStack.ToBytes` writes and
      what a nested `ItemstackAttribute` writes; how a world keeps its ids across loads; whether
      `Collectible.OnLoadCollectibleMappings` skips `TreeArrayAttribute` (`ROADMAP.md`, Maybe, books in schematics); whether the
      run's world can be kept and opened in game as the fixture world.
      Branch: `test/golden-stacks`. After: the plan's own commit. Model: opus. Author: approves the format and the way of capture.
      Done when: a spike shows the 0.1.1 mod enabled in the test server with a book made by a gesture; Decisions has the
      answers with their sources; `docs/modding.md` has the engine findings that surprised us.
- [ ] **B2. The capture project.** `CurioCatalogue.GoldenCapture`, in the solution but skipped by `dotnet test curiocatalogue.sln`
      and by CI (run by name, by hand). It makes, by the game's API and 0.1.1's own requests:
      - a ledger, a bill of materials, an album with some tapestries (partly full), an album with every piece (full);
      - a want list made from the partly full album; an incoming and an outgoing invoice;
      - a book with a custom name; a book with a non-default cover colour, and one with an aged cover;
      - a book with spent sheets (fewer than the maximum);
      and writes each into `CurioCatalogue.ServerTests/Golden/v0.1.1/` by the format of B1: per stack a name, its code, its
      bytes, the code of every id it holds (nested ones and those inside arrays included), the attributes as readable JSON
      for people; plus the game's and the mod's versions; plus `ModConfig\curiocatalogue.json` as 0.1.1 wrote it on its first
      start, with no changes (Decisions, "The scenario"). The inputs of each item (what went in) are written next to it, as the expected values for B3.
      Branch: `test/golden-stacks`. After: B1. Model: opus. Author: none.
      Done when: a run writes the files; a second run writes the same files but the dates (or the differences are explained);
      `docs/development.md` says what the project is, how to run it, and why CI skips it.
- [ ] **B3. The golden tests.** The reader `GoldenStacks` in `CurioCatalogue.TestServer` (a file into stacks of the test world,
      ids replaced by code, Decisions "Ids") and `UpgradeTests` in `CurioCatalogue.ServerTests`: every golden stack read by the
      current code, the expected values from B2's inputs, never from what the current code reads (Decisions, "Expected"):
      - each book: its mode, sheets, name, cover (the item's code), the entries (codes and counts), not newer (`v` 1);
      - each album: the collection id, collected and missing counts, full / not full;
      - the want list and both invoices: kind, lines, counts;
      - one action each, so "read" also means "works": an invoice posted on a real chest, a want list posted, an album taking a
        new tapestry, a book recharged in the grid (`BookTestBase`, `CraftingTests`);
      - the config file 0.1.1 wrote, through the mod's loader (as `ConfigFileTests`): loads without a warning, the settings 0.2
        adds get their defaults, and the books read with it as above.
      Branch: `test/golden-stacks`. After: B2. Model: sonnet. Author: none.
      Done when: all four projects pass locally and in CI; mutations checked (a stored key renamed in `LedgerStore`, the album's
      `members` not read, a nested id not replaced by the reader, a config key renamed); `docs/testing/functional.md` (⚙ where
      a case is now automatic) and `docs/development.md` (what golden stacks are) updated.
- [ ] **B4. The fixture world** (if B1 says it can be kept). The capture run lays the fixture out (invoices in a chest, on a
      shelf, on the ground; a chest to post on) and keeps its world; the author opens it in game with the 0.1.1 zip once, to see
      that it is a world a player could have, and keeps a backup outside the game's data folder. Otherwise the author makes it
      by hand with the 0.1.1 zip, by the list in `upgrade-0.1-to-0.2.md`. Either way the fixture's list there loses the
      "non-default values" of the config (Decisions, "The scenario"); U2 checks the file 0.1.1 wrote.
      Branch: `test/golden-stacks`. After: B2. Model: sonnet (the steps). Author: the look in game.
      Done when: the fixture's contents are written in `upgrade-0.1-to-0.2.md`; the backup's place is in `CLAUDE.local.md`.
- [ ] **B5. The process for the next plans.** `docs/testing/upgrade-0.1-to-0.2.md`: the golden stacks named next to the fixture
      (what each covers), and the rule for the table "Changes in 0.2.0 that need a case"; `docs/development.md`: at each
      release, a new golden set made by the capture project with that release's zip (`v0.2.0/` next to `v0.1.1/`), old sets
      never re-captured or edited; a change of the stored format raises `LedgerStore.Version` and keeps every old set green.
      `ROADMAP.md`: items 2–4 say "fills the upgrade table".
      Branch: `test/golden-stacks`. After: B3, B4. Model: sonnet. Author: approves the texts.
      Done when: the docs are committed.
- [ ] **B6. Code review, and an experiment with the architect.** The five roles of `docs/review.md` on the branch, the
      "mostly tests" scope. The experiment (hypothesis: the architect finds more, or proves more, when it may build and
      write): two architects in parallel on the same scope and prompt, one read-only (`Plan`, as today), one in its own
      worktree that may build, run tests and write probes as diffs. Compared by the moderation: findings each found alone,
      findings that survived verification, findings with a proof (a test or a probe), the cost (time, tokens).
      Branch: `test/golden-stacks`. After: B5. Model: opus. Author: goes through the findings; decides on the role from the result.
      Done when: the findings are fixed or rejected with reasons (Decisions, B6); the comparison and the decision on the
      architect's rights are in `docs/review.md` (what a round taught); all four projects pass; the branch goes to `main` by a
      pull request.

## Decisions

Proposed 04.11.2026, to be confirmed by the author (B1 settles the open ones).

- **Real 0.1.1 items, not ones the current code writes.** The point is to catch what the current code no longer reads; stacks
  written by today's `LedgerStore` would change together with it.
- **Capture: the test server with the release's code** (the author, 04.11.2026), rather than a session in game with a dump
  tool. Reproducible: the same project makes the set of 0.2.0 from its zip. Not quite "made by a player's client": the client
  side of a gesture (the window, the hotkeys) does not run; what is stored is made on the server in both cases
  (`docs/critical.md`, server decides). Open (B1): how much of the capture code can be shared with `BookTestBase` while it
  compiles against 0.1.1.
- **Golden stacks in the repo, the world outside it.** A test world is 9–10 MB of SQLite, and the test server runs one fresh
  world per process; a world in git would be large, binary and need a fixture of its own. The golden files are small, readable
  in a pull request, and run in the server tests that already exist. The world serves the manual pass (things lying in the
  world, the dedicated server), which golden stacks do not cover.
- **Rejected: a schematic export** (WorldEdit) of a chest with the items: the engine remaps nested ids only in trees, not in
  arrays, where our entries are (`ROADMAP.md`, Maybe); the test would need its own remap anyway, with more moving parts.
- **Format** (open, B1): bytes as the game writes them, so the test starts from exactly what a world holds, plus readable JSON
  for people (never read by the test).
- **Ids** (open, B1): a stack's bytes hold numeric ids of the world it was made in. A world keeps its ids across loads and
  updates (to check), so a player's book always meets its own world's ids; the test emulates that by replacing each id by the
  code it had, everywhere in the stack. 0.2 adds items, so the ids of the test world will differ from the capture's.
- **Expected** values come from the inputs of the capture (what went into each item), written into the test; reading the same
  stacks with the current code to make the expected values would test nothing.
- **The scenario is the default one** (the author, 04.11.2026): a player dropped the 0.1.1 zip into `Mods`, played, and
  drops 0.2.0 in its place. No hand-edited config: the capture runs with 0.1.1's defaults. The one config that matters is the
  file 0.1.1 wrote on its first start (the mod writes the file back every time, `ConfigFileTests`), which 0.2.0 then reads
  instead of its own defaults: a default changed in 0.2.0 stays old in such a world. The golden config is that file, untouched.
- **What does not survive goes into the changelog.** If an old item or setting cannot keep working, the release says so in
  `CHANGELOG.md` (and in the READMEs when a player must do something); the golden test then checks the documented outcome.
- **Step letter B** (backward compatibility); P, S and T are taken.

## Where we stopped

