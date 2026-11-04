# Collections design: the shape before the core

Started 04.11.2026. `ROADMAP.md`, 0.2, item 2: before the core learns a second collection, every collection planned for 0.2
(ore crystals, seashells, butterflies) is designed on paper, and the config is researched as a feature of its own. From these
designs comes **the shape of a collection** (what it is made of, how an album is told its dimensions and groups, where its
definition and its settings live) and the list of changes to the core. The core is then built once, in its own plan (item 3),
with good confidence that the collections after it will not need it changed radically. No change to the mod in this plan;
measurements may be written as tests or probes.

## How to work on this plan

As in the previous plans: one step per short session with the project skill `/step <id>` (`.claude/skills/step/`). Each step
says **Branch**, **After**, **Model**, **Author** (a decision or in-game work only the author can do; "none" means the session
goes ahead on its own) and **Done when**. A session that stops in the middle writes one line under "Where we stopped".

Questions "to the players" are answered by the author, who plays on the server where these things are collected; players'
feedback in the ModDB comments after 0.2 corrects the answers (Decisions). Each research step ends with its questions for the
author, written so they can be answered without the rest of the session.

The research goes into this plan's Decisions (facts with their sources: vsapi, vssurvivalmod, asset files and lines, "checked in
game <date>"); what surprised us about the game goes into `docs/modding.md`. Other mods: integration targets may be read on a
copy unzipped into a scratch folder; competitors (memory `competitors.md`, e.g. Infinitum Collections) only by a subagent that
returns a summary and never quotes code; a mod not on the list that overlaps: ask the author first.

## Steps

- [x] **C1. Ore crystals.** What one piece is and how the album would show progress, for `crystalizedore-{grade}-{ore}-{rock}`
      (`itemtypes/resource/crystalizedore-graded.json`): the allowed combinations in vanilla, how a player gets each (which
      ores and rocks occur together in worldgen, grade by what), whether some combinations never occur in survival. Len's
      Gemology (a compatibility target) only as far as it adds or changes crystals. Questions for the
      author: one piece is the ore, ore and grade, or every combination, or the player chooses when making the album;
      what makes a group complete.
      Branch: `docs/collections-design`. After: the plan's own commit. Model: opus. Author: answers the questions.
      Done when: Decisions has the facts with sources and the author's answers; the open points are named.
- [ ] **C2. Seashells.** The same for `seashell-{type}-{color}` (block `BlockSeashell`, 7 × 6 = 42): every source (worldgen
      patches, trader, loot, schematics); whether a picked-up shell is a stack that lies in a chest, and with what code
      (check in game if the sources do not say); lang names. Questions for the author: one piece is the type or type and
      colour; groups.
      Branch: `docs/collections-design`. After: the plan's own commit. Model: opus. Author: answers; the in-game check if needed.
      Done when: as C1.
- [ ] **C3. Butterflies.** The same for `butterfly-dead-{type}` (169 types; Decisions, "Butterflies: dead only"): which types
      a player can catch in survival (the entity's spawn conditions by climate), the forms of a type (male, female, colour,
      season) and whether a species can be told from the code alone; how the display mods keep a butterfly (Butterfly
      Displays, Butterfly Cases - Reborn, Catch and Release: Butterflies; integration targets) and whether any of them adds
      live butterflies as items. Grouping by species is not decided here (Decisions, "Butterflies last"): only the facts it
      will need.
      Branch: `docs/collections-design`. After: the plan's own commit. Model: opus. Author: answers.
      Done when: as C1.
- [ ] **C4. Beyond 0.2, a glance.** Not designed, only checked against the shape: fish, fruit trees, berry bushes, tapestries
      of other mods (`BlockTapestry` blocks with their own `sizes`), custom collections (the collection in the item), live
      butterflies of other mods. For each: what one piece would be and what the shape would need from it. The point is to see
      the shape does not exclude them, not to plan them.
      Branch: `docs/collections-design`. After: C1–C3. Model: opus. Author: none.
      Done when: a short table in Decisions: collection, one piece, what it needs from the shape.
- [ ] **C5. Configs of mods.** How Vintage Story mods are configured in practice, so that we adopt what is common rather than
      invent: the game's own (`api.LoadModConfig` / `StoreModConfig`, JSON in `ModConfig`, no sync to clients), config
      libraries players and admins already use (in-game editing, server-to-client sync; which, how widespread, a hard or
      soft dependency), and data as assets (definitions in JSON assets that other mods and admins change with JSON
      patches, as the game's own content). Then our question: how the settings of a collection scale to ten collections and
      to collections of other mods' items, without a breaking change each time; what of today's file (`TapestryExclude`,
      `TapestryAdd`) must keep working (golden config, plan B). The server's config sent to clients (`ROADMAP.md`, Later)
      is in scope as a requirement, not built.
      Branch: `docs/collections-design`. After: the plan's own commit. Model: opus. Author: chooses among the options.
      Done when: Decisions has the options with their costs and the author's choice; `docs/architecture.md` gets the decision
      when it is made in C7.
- [ ] **C6. The cost of reading a book.** Measured, not guessed: a tooltip, the crafting checks and the window's hash every
      500 ms (`ROADMAP.md`, 0.2) on an album of 88, 169 and ~400 members, on the server and on the client side where the test
      server allows. A probe or a test that stays if it is cheap to keep.
      Branch: `docs/collections-design`. After: the plan's own commit. Model: sonnet. Author: none.
      Done when: the numbers and how they were taken are in Decisions, with a verdict: fine as is, or what the core must change.
- [ ] **C7. The shape and the core's plan.** From C1–C6 and what the album view needs (`2026-11-04-album-view.md`, A2): the
      shape of a collection (members and their keys, dimensions, groups, order, names, the definition's source, its settings;
      hierarchical and Cartesian dimensions, the levels of ore crystals as one album or three collections, members filled by
      any of several codes: Decisions, C1) and the changes to the core: the one collection resolver used by
      the store, the items and the network (today `Collections.Get` in three places), the registry as definitions, the
      config, what C6 asks. Each change with the collection that needs it. What changes stored data, recipes, item codes,
      config or lang is named for the upgrade table (`docs/testing/upgrade-0.1-to-0.2.md`). Then `ROADMAP.md`, 0.2, item 3
      becomes a link to the new plan of the core, and item 4 gets what each collection needs.
      Branch: `docs/collections-design`. After: C1–C6. Model: opus. Author: approves the shape and the core's plan.
      Done when: the shape is in `docs/architecture.md` (Part 1, with the alternatives); the core's plan is written; the
      branch goes to `main` by a pull request.

## Decisions

- **Design all collections first, then build the core once** (the author, 04.11.2026). A core shaped by butterflies alone
  (one dimension, the species as the item's variant) would be rebuilt for ore crystals (three dimensions). Research is cheap;
  answers take time.
- **Butterflies: dead only** (the author, 04.11.2026). In vanilla the collection is `butterfly-dead-*`: what a net catches
  (`entities/animal/invertebrate/butterfly.json`, `netCaughtItemCode`), what goes on shelves and into display cases. Live
  butterflies (`creature-butterfly-*`), if a mod makes them collectable, are another collection; the two are never mixed.
- **Butterflies last** (the author, 04.11.2026): 169 types need a way of grouping by species that we do not have yet. Among
  the collections of 0.2 they come last, and their grouping is decided in the pass over all collections after the
  stamp-album view (`ROADMAP.md`, 0.2), with the ideas the other collections gave by then.
- **The order of the collections by what they give the core:** ore crystals first (three dimensions test the shape hardest),
  seashells, butterflies. The promise of butterflies on the ModDB page is kept by the 0.2 release, not by their order.
- **Questions to the players are answered by the author;** the players' feedback in the ModDB comments after the release
  corrects them.
- **The config is a feature of its own** (the author, 04.11.2026): researched in C5, not decided per collection.
- **Step letter C** (collections); P, S, T and B are taken.

### Ore crystals (C1)

Facts, game 1.22.7, read 04.11.2026 (assets under `VintageStory-Dev/assets/survival/`, code in `game-src`):

- **The item** `crystalizedore-{grade}-{ore}-{rock}` (`itemtypes/resource/crystalizedore-graded.json`): 4 grades (poor, medium,
  rich, bountiful), 18 ores, 17 rocks; `allowedVariants` lists 407. They are exactly the variants of the ore block
  `ore-{grade}-{type}-{rock}` (`blocktypes/stone/ore-graded.json`): one crystal per ore block, none without.
- **How a player gets one: only by mining.** The ore block drops its crystal at `avg: 0.01` (`ore-graded.json`, `dropsByType`),
  times the player's `oreDropRate` stat (`BlockOre.cs:38#OnBlockBroken`); a blown-up ore block never drops one
  (`BlockOre.cs:98#OnBlockExploded`). No trader, loot table or panning gives crystals. Four story and ruin schematics hold 9
  crystals placed by hand (`tobiascave`, `village`, `monastery-of-fishing`, `m-outlaw-hoarders`), all of combinations that also
  generate. A crystal crushed with a hammer becomes one nugget (`recipes/grid/nuggets.json`): a collected piece can be used up.
- **Grade:** one per deposit, a random index (plus a depth bonus where the deposit says `RandomPlusDepthBonus`) clamped to the
  grades its rock allows (`DiscGenerator.cs:213#GenDeposit`, lines 241, 242, 328). So a rock that allows fewer grades gets its top
  one more often: cassiterite in andesite is poor 1/4, medium 3/4; in granite 1/4 each.
- **Never generated in vanilla: 54 of 407.** Uranium (39) and stibnite (9) have no deposit at all (`worldgen/deposits/`) and no
  nuggets from anywhere; hematite in sandstone (3: sandstone is not a host rock of hematite, `deposits/metalore/iron.json`);
  magnetite rich and bountiful in slate (2); ilmenite rich in peridotite (1). Left: 353. Found by comparing every deposit's
  `placeblock` with the item's list (a script in the session's scratch, not kept; the five gaps checked by eye in the files).
- **Counts by what one piece is** (all / generated): the ore 18 / 16; ore and grade 70 / 61 (lost: all of uranium and stibnite,
  and magnetite bountiful); ore and rock 136 / 118; every combination 407 / 353. Generated by grade: poor 118, medium 118, rich 74,
  bountiful 43.
- **Name:** "{Grade} crystallized chunk of {ore}" (`ItemOre.cs:84#GetHeldItemName`, lang `poor-crystallizedore-chunk`). The rock is
  not in the name, only in the tooltip ("Parent Material", `ItemOre.cs:19#GetHeldItemInfo`) and in the icon (the rock's texture
  under the ore's). Crystals of two rocks have the same name. The handbook has one page per ore (`handbook.groupBy`).
- **Stack:** up to 64; ground storage, shelves, display cases. Each combination is its own code: crystals of two rocks do not stack.
- **Rocks:** kimberlite is the deepest stratum (`rockstrata.json`, `BottomUp`); marble exists only as deposits inside slate and
  phyllite (`deposits/rock/marble.json`).
- **Len's Gemology 1.0.15** (a compatibility target; a copy unzipped from its ModDB file, 04.11.2026) makes vanilla crystals by
  grid recipes (`recipes/grid/item/crystalizedores_graded_recipes.json`): any crystal + hammer + chisel → 5 shards; 4 shards +
  4 stones of a rock + 2 nuggets → `game:crystalizedore-{grade}-{ore}-{rock}`, for nearly every combination the item allows,
  uranium and stibnite too (their nuggets still come from nowhere in vanilla). Its own blocks (crystals growing in 4 stages per
  ore, a decorative glass per ore) are other items; it does not change worldgen. With it, a crystal of any rock is craftable once
  the player has the nuggets and the stone: "collected" no longer means "found", and the album cannot tell the two apart (same code).
- **Terra Prety 7.11.2-pre.1** (the only release tagged 1.22.7; checked 04.11.2026 in its assets and decompiled dll): patches
  landforms, oceans, rivers, upheaval, trees and surface block layers; nothing of deposits, ore blocks, rock strata or geologic
  provinces. It may change how often ores are met (deposits follow the surface), not which ore, rock and grade occur: the 353
  combinations hold.

The author's answers (04.11.2026):

- **One piece starts from the ore** (the `ore` variant as it is: silver in quartz and silver in galena are two ores, so are
  limonite, hematite and magnetite, and native copper and malachite). Three levels, from the smallest:
  1. **The richest of each ore that occurs**, in any rock: 16 pieces. Bountiful for 14 ores; galena_nativesilver is medium at
     most; magnetite is rich (its bountiful exists only in slate, where magnetite deposits give poor and medium). One piece is
     then filled by any of several codes (bountiful native copper from any rock it occurs in; 13 rocks for quartz gold and
     silver): a member is not one item code, unlike a tapestry piece.
  2. **The richest each rock gets, for every ore and rock:** 118 pieces (cassiterite bountiful in granite, medium in andesite).
     By grade: medium 44, rich 31, bountiful 43.
  3. **Every combination that occurs:** 353.
  Whether these are three collections chosen when the album is made or one album showing all three: to discuss after the
  dimensions below are decided.
- **Dimensions, not groups:** an album may be read along any of its dimensions (ore, rock, grade), not only one fixed grouping
  as the pictures of the tapestries. Nothing about groups is fixed by C1; it is a question for the shape (C7) and the view (A2).
  Tapestries have dimensions too (the author): the picture, and the picture's size (1×2, 2×1, 2×2…). There they are
  **hierarchical** (a piece belongs to a picture, the size belongs to the picture; a picture is complete when all its pieces
  are), while the crystals' are **Cartesian** (ore × rock × grade, independent, only some points allowed). The shape takes both,
  perhaps mixed. Whether the game knows a picture's width and height is A1's question.
- **Combinations that never occur are excluded** (the 54 above).
- **What players do with crystals** (the author's server): some collect, duplicates certainly go to nuggets.

Open points of C1:
- Whether malachite ever generates in marble (`deposits/metalore/malachite.json` allows it; marble is placed by deposits of its
  own, and their order was not checked). An in-game look or the deposit order in `vsessentialsmod` (`GenDeposits`).
- The names: pieces that differ by rock only share a name, so a list of missing pieces (a want list, the window) must show the
  rock (A2 of the album view plan).
- Combinations that never generate: excluded by a default list in the config (as `TapestryExclude` today), or computed from the
  loaded deposits at startup (a mod adding uranium deposits would then bring them in): C5 and C7.

## Where we stopped

- 04.11.2026: C1 done (final): the facts, the author's answers and the open points in Decisions, "Ore crystals (C1)". Next: C2
  (opus, After: the plan's own commit).
