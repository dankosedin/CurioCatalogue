# The core of collections

Started 07.11.2026. `ROADMAP.md`, 0.2, item 3: the core built once for every collection of 0.2, to the shape decided in C7 of
`2026-11-04-collections-design.md` and written in `docs/architecture.md`, Part 1, "Collections" (read it first: this plan does
not repeat it). The collections themselves (ore crystals, seashells, butterflies) are item 4, each a plan of its own on this
core; the album window is A3–A5 of `2026-11-04-album-view.md`, after this plan.

## How to work on this plan

As in the previous plans: one step per short session with the project skill `/step <id>` (`.claude/skills/step/`). Each step
says **Branch**, **After**, **Model**, **Author** (a decision or in-game work only the author can do; "none" means the session
goes ahead on its own) and **Done when**. A session that stops in the middle writes one line under "Where we stopped".

The agent does not run the game: what only the game client can show is the author's, with exact steps written by the agent.
Each step updates `docs/testing/functional.md`, `CHANGELOG.md` and the upgrade table (`docs/testing/upgrade-0.1-to-0.2.md`,
"Changes in 0.2.0 that need a case") in its own commit, where they apply; the lines it must add are named in the step. The golden
stacks of 0.1.1 stay green through every step (`UpgradeTests`).

## Steps

- [x] **K1. What the game does for the shape.** Answer from the game's source (`game-src`, decompiled engine) and, where it
      does not say, a server test: whether grid recipes removed on the server at load (`api.World.GridRecipes`) are not sent to
      clients, and when the removal must happen; whether `Assets.GetMany("config/curiocatalogue/collections/")` returns the
      assets of every domain on both sides, with patches applied; the order of a variant group as the registry keeps it (for
      `max`); the game's lang keys usable for dimension values (ores, rocks; the grade's `ore-grade-*` says "Quality: …", so
      ours); how a packet on join is sent before the player opens anything.
      Branch: `feat/collections-core`. After: the plan's own commit. Model: opus. Author: none.
      Done when: the answers with sources in Decisions; what surprised us in `docs/modding.md`; a change to the shape, if
      any, agreed with the author and written in `docs/architecture.md`.
- [x] **K2. Definitions in the core.** Pure types in `CurioCatalogue.Core`: a definition (dimensions with ordered values, names
      and icons; levels with `keep` and `any` / `max`; layouts; attribute rules; exclusions), the source points it is applied
      to (point, key, sample: given by the adapters), and what it builds per level: members with ids, fillers and stacks to show,
      groups and order per layout, progress. The check "a key fills at most one member in a level". Unit tests on made-up points
      for crystals (three levels, the counts 16 / 118 / 353 on the real list of points as test data), seashells (7 / 42) and
      tapestries (picture shapes). Nothing uses it yet.
      Branch: `feat/collections-core`. After: K1. Model: opus. Author: none.
      Done when: the tests pass and are mutation-checked (`max` read as `any`; a duplicate key let through).
- [ ] **K3. The registry from assets, the config.** The sources `variants` and `tapestry` in the adapters; the registry reads the
      definitions from every domain and applies `Collections.<id>` from the config (`Exclude`, `Add`, `Disabled`); the
      tapestries become the first definition asset (`tapestries.json`), built the same as today (88 pieces, 31 pictures).
      `TapestryExclude` and `TapestryAdd` move into `Collections.tapestries` on load: read once, moved, removed from the file.
      Server tests: the tapestry collection equal to today's; a definition from another domain; a patched definition.
      Branch: `feat/collections-core`. After: K2. Model: sonnet. Author: none.
      Done when: the tests pass; `UpgradeTests` (golden config) checks the move; upgrade table: the config keys (U2); both
      READMEs' config section and `CHANGELOG.md`.
- [ ] **K4. The album, format 2.** The album as a core type of its own: level (or none), collected members with the stack that
      filled each, sheets, name; missing computed from the collection. `LedgerStore.Version` 2; format 1 migrated on read
      (snapshot and missing entries → collected member ids), written as 2 on the next server write. Taking new by members (a
      member of several codes filled by any of them, in the default layout's order); the level chosen while empty (a request
      from the window, re-checked on the server), fixed by the first piece, freed by "Start over"; no level → taking new and
      want lists refuse (A2). `Refresh` / `RefreshIfStale` and the snapshot go. The tooltip and today's window work on it until
      A3 (the window shows the missing members as now).
      Branch: `feat/collections-core`. After: K3. Model: opus. Author: none.
      Done when: the server tests pass with the 0.1.1 golden albums read through the migration (U3, U4, U10) and a test of the
      write-back; mutation-checked (the migration loses a collected piece); upgrade table: the album format (U10, U12).
- [ ] **K5. Want lists by members.** A want list's line names (collection, level, member id); taking by it goes the album's way.
      Want lists of format 1 read as exact entries (golden `want-list`, U6). The tooltip names a member of several codes with
      its freedom ("any rock").
      Branch: `feat/collections-core`. After: K4. Model: opus. Author: none.
      Done when: the server tests pass, mutation-checked; upgrade table: the want list format (U6).
- [ ] **K6. One resolver, `Disabled`, the packet, the recipes.** The resolver (`Found | Disabled | Unknown`) replaces the three
      `Collections.Get`; refusals "disabled on this server" (taking new, want list) beside "no longer in the game"; the
      `Collections` section sent to each client on join, the client's registry rebuilt; the recipe of a disabled collection removed
      as K1 found (or makes nothing). The album recipe stays the tapestries' in this plan; each collection of item 4 adds its own.
      Server tests for the resolver, the refusals and the recipe; the packet and the client's view are an in-game check on the
      dedicated server (`VintageStory-ServerData`): a collection disabled and an `Exclude` on the server only, seen by the client.
      Branch: `feat/collections-core`. After: K5. Model: sonnet. Author: the in-game check.
      Done when: tests pass, mutation-checked; the case in `functional.md` passed; upgrade table: the lang keys changed
      (`album-name-tapestries` and the "whole pictures" of `ledger-info-album` become a collection's name and its group's
      word), `CHANGELOG.md`; `docs/domain.md`, Albums and collections, says what changed (levels, members of several codes,
      disabled collections).
- [ ] **K7. Code review of the core, before the merge.** The five roles of `docs/review.md` on the branch, scope the diff from
      where it left `main`. Weight on what breaks silently and late: the migration of format 1 (albums and want lists of
      0.1.1 must read the same, and nothing collected may be lost), all or nothing in taking new by members, the server re-checking
      the level request, the client and the server disagreeing on a collection (the packet late or missing), a definition patched
      wrongly by another mod (one bad asset must not take the other collections down). Questions from the future for the
      architect: the collections of item 4, the album window (A3–A5), custom collections and the rule on an attribute (berry
      bushes).
      Branch: `feat/collections-core`. After: K6. Model: opus. Author: goes through the findings.
      Done when: the findings are fixed or rejected with reasons (Decisions, K7); what the round taught in `docs/review.md`; all
      test projects pass; the branch goes to `main` by a pull request.

## Decisions

- **The shape is the one in `docs/architecture.md`, Part 1, "Collections"** (the author, 07.11.2026, C7 of the collections design
  plan: R1–R10 accepted as proposed).
- **One branch for the plan, one pull request at the end:** the steps change one structure, and a half-built core in `main` would
  hold back the other plans. If the branch grows too large to review, the author may split the pull request after K3.
- **Code review before the merge, as the plan's last step (K7)** (the author, 07.11.2026): the change is fairly isolated but
  its effect is huge (stored data, every collection after it); finding at the end of the release that something in it is wrong
  would hurt. R1 of the release plan then reads the core again only as the ground of the album window.
- **What changes stored data, recipes, item codes, config or lang** (for the upgrade table), by step: K3 the config keys; K4 the
  album's format; K5 the want list's format; K6 lang keys and, with a collection disabled, the recipe. The album recipes of new
  collections come with item 4.
- **What the game does for the shape (K1, 07.11.2026, final).** From the game's source and the decompiled `VintagestoryLib`
  1.22.7; no server test was needed. The details and references are in `docs/modding.md` (Recipes, Config and logging, Network,
  Vanilla content).
  - *Removing a grid recipe.* The server serialises its recipe registries into the server-assets packet once, after the
    `WorldReady` phase, so a recipe taken out of `api.World.GridRecipes` in `AssetsFinalize` or `StartServerSide` never reaches a
    client. The list alone is not enough on the server: resolving put the recipe into `FastSearchRecipesByIngredient`, which the
    crafting grid searches, skipping only `Enabled: false`; and `Enabled` is not sent. So K6 removes it from the list and sets
    `Enabled = false`, finding the recipes of our album file by `Name` (the file's asset location, kept by every expanded recipe).
    Our config is read in `Start`, before that.
  - *`Assets.GetMany("config/curiocatalogue/collections/")`* returns that path in every domain, on both sides (`config` is a
    universal category), with patches applied when read after `AssetsLoaded` at order 0.05 (the patch loader rewrites the cached
    asset). A client has only the universal mods the server lists for it, so a server-side mod's definition, or a
    `"side": "server"` patch, is on the server only. **The author:** the client keeps reading definitions from its own assets; a
    mod with a collection must be universal and required on the client, said in the README's part for modders (G1 of
    `2026-11-04-mod-description.md` got the line). `docs/architecture.md` says so, with the rejected option.
  - *The order of a variant group* is not kept at run time: an item has only its own values, ids follow the world's history,
    `itemtypes` are not read on the client. **The author:** the order is a list in the definition; values not in it come after,
    by code, and rank lowest for `max`, with a warning (`docs/architecture.md`, Dimensions).
  - *Lang keys:* `game:ore-{ore}` and `game:rock-{rock}` exist in `en` and `uk` for every value; the grade only as
    `game:ore-grade-*` ("Quality: Poor"), so grade names are ours. Seashells have only full names
    (`block-seashell-{type}-{color}`): type and colour names are ours too (for the seashell plan).
  - *The packet on join:* send it in `PlayerJoin`. The client's handlers exist by then (`StartClientSide` runs before it asks to
    join), early channel messages are queued until the channel list comes, and packets run in order: the message arrives with
    items and blocks registered, maybe before the grid recipes, and before the client's `LevelFinalize`. K6 stores it in the
    handler and rebuilds the registry at `LevelFinalize`. Reasoned from the code: K6's in-game check on the dedicated server
    watches it.
- **Definitions in the core (K2, 07.11.2026, final; proposed by the AI assistant in the step, within the shape).** The code is
  `CurioCatalogue.Core/CollectionDefinition.cs` and `DefinedCollection.cs`, tests `DefinedCollectionTests`.
  - *Building is a value:* `DefinedCollection.Build(definition, points)` gives `Built` (with warnings for the log) or `Invalid`
    (the errors). Any error, a key filling two members of a level among them, makes the whole collection invalid; the other
    collections are untouched (K3 logs and skips it). Warnings: a value missing from a dimension's list, a point given twice by
    name (the first is kept).
  - *A member's id* is its kept dimensions' values in the order of the definition's dimensions, joined by `-`
    (`poor-nativecopper-andesite`, `scallop-latte`, the piece name for tapestries, whose level keeps only `piece`). Albums
    store it (K4), so a definition must not reorder its dimensions once released; ids that collide are an error.
  - *A level names every dimension:* kept, or dropped as `any` / `max`; no default. Several `max` dimensions apply in the
    definition's order.
  - *A dimension with no list of values* keeps the order in which the source gives its points, with no warning (tapestries:
    the order of `sizes`, as today); with a list, as `docs/architecture.md` says.
  - *Hierarchical dimensions need no flag:* a member has a value in every dimension on which all its points agree, so a
    piece has its picture and size; a layout may group or arrange only by dimensions every member has a value in.
  - *Attributes:* those the source gives and all the member's points share (a piece's `x`, `y`), then the rules; a rule applies
    only to members with a value in each of its `where` dimensions (rare colours mark nothing at the seashells' type level).
  - *Shape* reads integer `x`, `y` and orders a picture's pieces row by row; today's order inside a picture is the pieces'
    names (x, then y). Only the order of taking new inside one picture changes (K4).
  - *A level with no layout* gets one group in order. Stacks to show and fillers follow the dimensions' order.
  - *Names:* `DefinedCollection` beside the MVP's `Collection` until K4 removes it; `NamePattern.Matcher` is the old
    `Collection.Matcher`, which now calls it.
  - *Test data:* the 407 allowed codes of `game:crystalizedore` (1.22.7) in `CurioCatalogue.Core.Tests/Data`; with the 54 of C1
    excluded, 16 / 118 / 353, and level 2 by grade medium 44, rich 31, bountiful 43 as C1 counted. Bountiful native copper
    occurs in one rock only; quartz gold and silver in 13 (the tests' example of a member of several codes).
- **Step letter K** (core); A, B, C, F, G, P, R, S and T are taken.

## Where we stopped

- 07.11.2026: K1 done (final): the answers in Decisions, "What the game does for the shape (K1)"; the surprises in
  `docs/modding.md`; two changes of the shape agreed and written in `docs/architecture.md` (the order of values a list in the
  definition; definitions read by the client from its own assets, a collection mod universal). Next: K2 (opus, After: K1).
- 07.11.2026: K2 done (final): the types and `DefinedCollection.Build` in the core, unit tests mutation-checked (`max` read as
  `any`: 3 tests fail; a duplicate key let through: 1 fails); all five test projects pass; nothing uses the new types yet. The
  step's choices in Decisions, "Definitions in the core (K2)". Next: K3 (sonnet, After: K2).
