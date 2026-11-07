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
- [x] **K3. The registry from assets, the config.** The sources `variants` and `tapestry` in the adapters; the registry reads the
      definitions from every domain and applies `Collections.<id>` from the config (`Exclude`, `Add`, `Disabled`); the
      tapestries become the first definition asset (`tapestries.json`), built the same as today (88 pieces, 31 pictures).
      `TapestryExclude` and `TapestryAdd` move into `Collections.tapestries` on load: read once, moved, removed from the file.
      Server tests: the tapestry collection equal to today's; a definition from another domain; a patched definition.
      Branch: `feat/collections-core`. After: K2. Model: sonnet. Author: none.
      Done when: the tests pass; `UpgradeTests` (golden config) checks the move; upgrade table: the config keys (U2); both
      READMEs' config section and `CHANGELOG.md`.
- [x] **K4. The album, format 2.** The album as a core type of its own: level (or none), collected members with the stack that
      filled each, sheets, name; missing computed from the collection. `LedgerStore.Version` 2; format 1 migrated on read
      (snapshot and missing entries → collected member ids), written as 2 on the next server write. Taking new by members (a
      member of several codes filled by any of them, in the default layout's order); the level chosen while empty (a request
      from the window, re-checked on the server), fixed by the first piece, freed by "Start over"; no level → taking new and
      want lists refuse (A2). `Refresh` / `RefreshIfStale` and the snapshot go. The tooltip and today's window work on it until
      A3 (the window shows the missing members as now).
      Branch: `feat/collections-core`. After: K3. Model: opus. Author: none.
      Done when: the server tests pass with the 0.1.1 golden albums read through the migration (U3, U4, U10) and a test of the
      write-back; mutation-checked (the migration loses a collected piece); upgrade table: the album format (U10, U12).
- [x] **K5. Want lists by members.** A want list's line names (collection, level, member id); taking by it goes the album's way.
      Want lists of format 1 read as exact entries (golden `want-list`, U6). The tooltip names a member of several codes with
      its freedom ("any rock").
      Branch: `feat/collections-core`. After: K4. Model: opus. Author: none.
      Done when: the server tests pass, mutation-checked; upgrade table: the want list format (U6).
- [x] **K6. One resolver, `Disabled`, the packet, the recipes.** The resolver (`Found | Disabled | Unknown`) replaces the three
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
      The round ran and the author went through the findings (07.11.2026, Decisions, "The code review (K7)"); the fixes are
      K7a–K7c, then the lessons, the tests and the pull request.
  - [ ] **K7a. Registry, definitions, the album recipe.** R1, R2, R4, T3, the empty level id, H2, C2, P1–P3 of the Decisions;
        the Teammate's and QA's registry tests. Model: opus. Done when: tests pass, the new ones fail on the old code;
        `docs/domain.md` (Recipes, the definition's `albumRecipe`) and `docs/architecture.md` (the recipe rule) say it.
  - [ ] **K7b. Albums, want lists, the packet.** M1+M3, W1, H6, A9/C7, H14, T4; QA's tests for M1, Q3, Q4, Q8. Model: opus.
        Done when: tests pass, the new ones fail on the old code; upgrade table if H6 changes what is written; lang for W1.
  - [ ] **K7c. Docs and the leftovers.** Q6, Q7 (README in both languages; the text shown to the author at release), H16/A7,
        C6 in `docs/domain.md`; H8/H9 to `ROADMAP.md`, A5 to the seashell plan's ideas, A8 to item 4; what the round taught in
        `docs/review.md`. Model: sonnet.

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
- **The registry from assets, the config (K3, 07.11.2026, final; proposed by the AI assistant in the step, within the shape).** The
  JSON of a definition and what the registry does are in `docs/architecture.md`, Part 1, Collections ("The JSON of a definition").
  - *The id of a collection is its file's name* (`tapestries.json`); a second file with the same id is ignored with an error. The
    reader is `CollectionDefinitionReader`, the sources are `variants` and `tapestry`; `Add` works for `tapestry` only.
  - *The config:* `Collections` is a dictionary by id (default `tapestries` with empty lists); the legacy keys are `string[]?`,
    null by default and not written (`NullValueHandling.Ignore`), read by `MoveLegacy()` before `Correct()` in `LoadConfig`
    (appended after what `Collections.tapestries` already holds, a line in the log), then null, so the write-back drops them.
  - *Changed behaviour, small:* the config's `Exclude` also leaves out an `Add`ed piece it matches (0.1 kept it); an `Add` of a
    piece the block already has gives a warning "given twice" in the log (0.1 was silent). The order of taking new inside a
    picture is by place (row by row), as K2 decided; the album window and K4 do not care yet.
  - *`Disabled` for now only removes the collection from the registry:* consumers see "no collection". K6 makes it a refusal of
    its own, and sends it to clients.
  - *`IAssetManager` is not mockable in `CurioCatalogue.Tests`* (SkiaSharp): the registry takes `Func<IEnumerable<DefinitionAsset>>`
    (`docs/modding.md`, Config and logging). The read of the real assets is tested in `CurioCatalogue.CompatTests`
    with a test mod `cccollections` (a collection of the game's ingots by a definition of its own domain, and a patch of it);
    the server tests of the plan for "the tapestry collection equal to today's" are there too (`CollectionsFromAssetsTests`: 88 +
    the 2 pieces of `cctapestry`, 31 + 1 pictures), because only that project has a mod besides ours.
- **The album, format 2 (K4, 07.11.2026, final; proposed by the AI assistant in the step, within the shape).** The code is
  `CurioCatalogue.Core/Book.cs`, `Album.cs` and `LedgerStore` (the layout in its comment); tests `AlbumTests`, `LedgerStoreTests`,
  `UpgradeTests` (the golden albums through the migration, the write-back), `CollectionsFromAssetsTests` (the level request).
  - *Books as a closed pair:* `Book` (sheets, name, `ReturnSheet`, `AddSheets`) with `Ledger` (types and counts only) and `Album`;
    `LedgerMode.Album` stays as the kind of book (the item's variant). The store reads by kind (`ReadBook`, `ReadLedger`,
    `ReadAlbum`), `InvoiceFates` and `CreateInvoiceResult.Created` take a `Book`. The MVP's `Collection`, the snapshot and
    `Refresh` / `RefreshIfStale` are gone; `CollectionRegistry.ForSample` gives a `DefinedCollection`.
  - *What an album stores:* `album: { id, level, collected: [ { id, stack } ], unplaced: [ { stack } ] }`, `v` 2; no `entries`.
    The stack that filled a member is a copy of one item from the container. `LedgerStore.Version` 2 is written on every book
    and invoice (one number for all; 0.1.1 refuses them as newer, U12).
  - *Nothing collected is lost by the migration:* format 1's collected (the snapshot less the missing; without a snapshot, the
    collection's first level less the missing, as 0.1 read it) go on the first level's members, the only level 0.1 knew. Those
    that are no member now (the collection unknown or disabled, a piece excluded) are kept as `unplaced` and placed on every read
    once the collection has them again. Rejected: dropping them (a server that disables tapestries for a while, then any write
    of the album, a rename or a sheet back, would wipe it).
  - *The level:* `Album.LevelIn` gives the chosen level; a collection of one level has it always (also a stored id the
    collection no longer has); else none, and taking new and want lists refuse `nolevel`. Choosing (`ChooseLevelRequest` from the
    window, re-checked on the server): a level the collection lacks is ignored with a diagnostic line (stale or forged), a filled
    album refuses `levelfixed`. "Start over" frees it (`album-restarted-nolevel` when there is no level). Today's window has a
    level dropdown while the album is empty and its collection has several levels; A3 replaces it.
  - *Refusals about the album come first:* taking new says `nocollection` / `nolevel` before looking at the block (they are about
    the book, and true of any chest). Until K6, a collection off in the config is `nocollection` too.
  - *Taking new by members:* for each missing member in the default layout's order, the first of its items the container has; a
    key fills one member of a level, so the lines keep keys and `Transfer` is unchanged. A want list is a line per missing
    member by its first item (K5 makes it a member reference).
- **Want lists by members (K5, 07.11.2026, final; proposed by the AI assistant in the step, within the shape).** The code is
  `Entry.cs`, `Invoice.cs`, `LedgerStore` (the layout in its comment), `InvoicePoster.PostWantList`, `ItemInvoice.Freedom`; tests
  `AlbumTests` (want lists of format 2), `LedgerStoreTests`, `InvoicePosterTests`, `CollectionsFromAssetsTests` (the test mod's
  seashells `ccshells`: a level by type, any colour), `UpgradeTests` (U6, unchanged).
  - *Stored:* the collection and the level once per list (`wants: { id, level }`, `Invoice.From`), not on every line: a list comes
    from one album. Each line keeps the member's first item as its stack (what it shows) and adds `member`. No `wants`, no `member`:
    format 1, exact lines.
  - *Taking:* `Invoice.Wanted(level, available)` as the album's: the first of the member's items the container has; a line whose
    member the level no longer has, or whose collection or level the game lacks, wants exactly its own item (the list still works
    after a mod is removed). No item goes to two lines; lines are crossed off by their place, not by key. Posting never refuses
    for the collection; whether K6's `Disabled` should, is K6's.
  - *The level* of a list is found as an album's (`DefinedCollection.LevelOrOnly`: a collection of one level has it under any id).
  - *The tooltip's freedom:* `DefinedCollection.Free(level, member)`, the dimensions the level drops as `any` where the member has
    no value, only for a member of several items. Named by a new optional `anyKey` of a dimension in the definition (a whole
    phrase in lower case, "any colour", for each language's grammar); a dimension without it gives "any of N kinds"
    (`invoice-info-any-of`). The line is "1 × {the first item's name}, any colour" (`invoice-info-member-free`). Naming a member by
    its own values ("Scallop, any colour" rather than the first shell's full name) is left for the album window (A3–A5) and item 4.
- **The resolver, `Disabled`, the packet, the recipes (K6, 07.11.2026, final; proposed by the AI assistant in the step, within the shape).** The code is
  `Core/CollectionResolution.cs`, `Adapters/CollectionRegistry.Resolve`, `LedgerStore.Resolve`, `Adapters/AlbumRecipes.cs`, `ModNetwork`
  (`CollectionSettingsPacket`), `CurioCatalogueModSystem.UseServerSettings`; tests `CollectionRegistryTests`, `InvoicePosterTests`,
  `InvoiceMakerTests`, `AlbumRecipesTests`, `CollectionSettingsPacketTests`, `RefusalTests`.
  - *The resolver:* `CollectionResolution` is a closed pair of `Found(Value)`, `Disabled(Id)`, `Unknown(Id)`. The registry answers it:
    `Disabled` when the config says so (even with no definition: the admin's word), else `Found`, else `Unknown`. `LedgerStore` takes
    the resolver (the old constructor with a collection function stays for tests). `Defined` / `CollectionOf` still give the collection for
    reading; every refusal and every text that must tell the reasons apart goes through `Resolve`. Refusals: `disabled` (new) beside
    `nocollection`, in taking new, want lists, level choice and Start over; the tooltip and window say "switched off on this server".
  - *The packet:* `CollectionSettingsPacket` (id, `Exclude`, `Add`, `Disabled` of every collection of the server's config), sent in
    `PlayerJoin`; the client's handler stores it in the mod system and drops the registry, which is built again on next use, and once
    more at `LevelFinalize`. Until a packet comes the client uses its own file. Not in-game checked: G5, G6.
  - *The recipe:* `AlbumRecipes` maps a collection id to the names of its album recipes (`tapestries` → `ledger-album`); at `AssetsFinalize`
    on the server the recipes of disabled collections leave `GridRecipes` and get `Enabled = false` (found by our domain and `name`,
    not by the file's location: our recipes set `name`). Each collection of item 4 adds its line.
  - *Lang:* a collection's words moved into its definition: `nameKey` (the collection's name, "tapestries", shown as `album-name` "Album: {0}")
    and, new, a level's `progressKey` (the whole progress line with four numbers, for each language's grammar; none: the generic
    `ledger-info-progress`). Removed `album-name-tapestries`, `ledger-info-album`, `ledgerwindow-collected`. Added `ingameerror-disabled`,
    `ledger-info-disabled`. Found on the way: a positional record parameter named like an inherited property of the same type
    reuses it (a stack overflow in `CollectionResolution.Collection`), hence `Found(Value)`.
- **The code review (K7)** (the author, 07.11.2026). Five roles at b0c4a08, about 35 findings in seven groups; ids as the roles
  gave them (T Teammate, Q QA, A Architect, C C# expert, H Hostile). Fixed in K7a–K7c unless said otherwise.
  - *Registry.* R1 (all five roles): a definition that throws (a dimension named twice in the pattern, a bad regex, `sizes` of
    another shape, `null` in a config list) took every collection down, again on each use. The reader checks these and answers
    `Bad`; `null` in the config's lists is dropped on reading; each definition is built inside a guard that logs and skips it.
    R2: the first **good** definition of an id wins, ours (`curiocatalogue`) before others, then the asset order; the others are
    logged. R3 (ids of different mods collide): rejected, the bare file name was decided in K3. R4: each problem is logged once
    per session, not on every rebuild. T3: `drop: max` on a dimension with no `values` is an error, not a random order. An empty
    level id is refused (it read as "no level chosen"). H2: a warning for `Add` on a `variants` collection and for a config id
    no definition has. C2: the check "two members have the id" cannot fire (names are deduplicated before) and goes.
  - *Levels and migration.* M1 (an album of format 1 placed on the first level) cannot happen today (the tapestries have one
    level, `pieces`; `pictures` is a layout), and M3 (a level renamed by a patch leaves a filled album at "no level") can: one
    rule for both, an album whose level the collection does not have places what it collected on the level that takes most of
    it (a tie: the first); what fits nowhere stays unplaced, nothing is lost. M2 (format 1 with no snapshot and no collection
    reads empty): rejected, only development builds before 0.1.0 wrote it. M4 (Start over refused for a collection off or
    gone): by design (`docs/domain.md`, a switched-off collection), an admin's switch must not let a stray click wipe an album.
  - *Want lists.* W1: posting a want list of a switched-off collection is refused with "disabled on this server", like taking
    new; the list stays and works again when the collection is back. A collection the game lacks keeps the exact-item reading
    (the list must not become rubbish for good). `docs/domain.md` names making and posting a want list apart.
  - *The album recipe (overrides K6: "each collection of item 4 adds its own" in C#).* P1: a definition names its album recipe,
    `albumRecipe` with the domain (`curiocatalogue:ledger-album` for the tapestries); the registry maps recipes to collections
    from every good definition, switched-off ones included (R2's order, a recipe claimed twice is logged); `ItemLedger` asks the
    registry instead of a constant; `AlbumRecipes` reads the definitions itself at `AssetsFinalize` (no full build there: it reads
    the world's blocks) and takes out recipes of any domain. So a mod adds a collection with its album by JSON alone. P2 (a
    sample of two collections) goes with it: the collection comes from the recipe; the sample must still be its member, as now.
    P3: a server test on a clone of the loaded `GridRecipes` (the shared server must keep the recipe for `CraftingTests`).
  - *The client.* A9/C7: `ExtraIgnoredAttributes` goes in the settings packet and the client's keys are rebuilt on it, so the
    client's view counts items as the server does.
  - *Stored data.* H6: `v=2` is written on ledgers and bills of materials too, so 0.1.1 refuses them. If format 2 changed only albums
    and want lists (K7b checks first), ledgers and bills keep `v=1`, so a player who goes back to 0.1.1 keeps them. H14: the move of the old tapestry keys is logged only when something moved.
  - *Tests.* QA's tests for Q3 (the join packet), Q4 (taking new in the layout's order), Q8 (an album of 0.1.1 written while its
    collection is off). T4, the flake of K3/K4/K7 (the author's find): test chests float at spawn height + 6 and the player is
    above ground, so a dropped list falls onto a chest lid or the ground, out of the ±4 cube `OnGroundNear` searches; it searches
    the column (±4 across, ±64 up and down).
  - *Docs.* Q6, Q7 (README: the config is synced since K6), H16/A7 (`docs/architecture.md` behind K6), C6 (a collected stack keeps
    its ignored attributes: kept, one line in `docs/domain.md`).
  - *Left as they are:* H10 (`LedgerMode.Album`), H11 ("Collect" names four things), H12 (the test constructor), H13 (`FilledBy.Key`
    computed again): the names are a known debt, not worth a rename now. *Later:* H8/H9 (the resolve ladder in four places, two
    "first filler" algorithms) to `ROADMAP.md`, for when new collections come; A5 (the filler chosen before room is known) to the
    seashells; A8 (`Member.Fills`/`Shown`) to item 4.
- **Step letter K** (core); A, B, C, F, G, P, R, S and T are taken.

## Where we stopped

- 07.11.2026: K1 done (final): the answers in Decisions, "What the game does for the shape (K1)"; the surprises in
  `docs/modding.md`; two changes of the shape agreed and written in `docs/architecture.md` (the order of values a list in the
  definition; definitions read by the client from its own assets, a collection mod universal). Next: K2 (opus, After: K1).
- 07.11.2026: K2 done (final): the types and `DefinedCollection.Build` in the core, unit tests mutation-checked (`max` read as
  `any`: 3 tests fail; a duplicate key let through: 1 fails); all five test projects pass; nothing uses the new types yet. The
  step's choices in Decisions, "Definitions in the core (K2)". Next: K3 (sonnet, After: K2).
- 07.11.2026: K3 done (final): reader, registry and config move in place, tests mutation-checked (the config's `Exclude` left out of
  the definition: 2 fail; the legacy keys not cleared: 2 fail). All five test projects green; one flake seen once in an earlier run: `BookFlowTests.
  Containers_the_mod_does_not_read_are_refused(chest-east, perPlayerLoot: True)` once timed out waiting for a join (30 s, the run took
  6 min) and passed alone and in the full run after. Not committed yet: the author's "комічь". Next: K4 (opus, After: K3).
- 07.11.2026: K4 done (final): the album of format 2 in the core and the store, the migration of format 1, taking new by members,
  the level request; tests mutation-checked (the migration loses a collected piece: 4 core, 4 adapter and 9 golden tests fail;
  placing drops an unplaced piece: 2 fail). All five test projects green; a flake of the K3 kind (a 30 s timeout), this time
  `BookEdgeTests.L1_an_intake_list_lands_on_the_ground_when_the_inventory_is_full`, once, passed alone and in the next full run. The in-game check of the old window on an album is
  the author's (the step's report). Next: K5 (opus, After: K4).
- 07.11.2026: K5 done (final): want lists name their album's level and members, filled the album's way; format 1 read as exact
  lines; the tooltip names what a member of several items leaves free. Tests mutation-checked (a member line read as exact: 2 core
  and 1 adapter tests fail; the store dropping `member`: 2 fail). The step's choices in Decisions, "Want lists by members (K5)". No
  in-game check: vanilla has no collection of members of several items before item 4 (W5 in `functional.md`). Next: K6 (sonnet,
  After: K5).
- 07.11.2026: K6 done (final): code, tests, docs; G5 and G6 of `functional.md` passed by the author on the dedicated server (a
  collection disabled; an `Exclude` on the server only: the client showed 82 pieces in 28 pictures from the server's packet while its own
  file said nothing). After G5 the hints over a container are gone for an album whose collection is off or gone (`LedgerHintsBehavior`).
  Open for the author: the want-list button is hidden for a switched-off album (option A, kept); the album stays in the handbook and
  creative inventory, only its recipe goes. The step's choices in Decisions, "The resolver (K6)". Next: K7
  (opus, After: K6).
- 07.11.2026: K7 review round done: five roles, findings merged; the author went through all seven groups (Decisions, "The code
  review (K7)"). Nothing in the code changed yet. Next: K7a (opus), then K7b (opus), K7c (sonnet), then the pull request.
