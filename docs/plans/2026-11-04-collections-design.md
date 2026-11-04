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

- [ ] **C1. Ore crystals.** What one piece is and how the album would show progress, for `crystalizedore-{grade}-{ore}-{rock}`
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
- [ ] **C7. The shape and the core's plan.** From C1–C6: the shape of a collection (members and their keys, dimensions, groups,
      order, names, the definition's source, its settings) and the changes to the core: the one collection resolver used by
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

## Where we stopped
