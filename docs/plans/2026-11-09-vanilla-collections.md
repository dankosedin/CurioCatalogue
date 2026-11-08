# The collections of 0.2: ore crystals, seashells, butterflies

Started 09.11.2026. `ROADMAP.md`, 0.2, item 4: the three vanilla collections on the core of item 3
(`2026-11-07-collections-core.md`), each a definition asset, its album recipe and its lang. What each collection is was decided
in C1–C3 of `2026-11-04-collections-design.md` (Decisions: "Ore crystals", "Seashells", "Butterflies"); the shape is in
`docs/decisions.md`, "Collections"; how the album window shows them is A2 of `2026-11-04-album-view.md`. Read those first:
this plan does not repeat them. The test mod's `ccores` and `ccshells` (`CurioCatalogue.CompatTests/testmods/cccollections`)
are near drafts of two of the definitions.

## How to work on this plan

As in the previous plans: one step per short session with the project skill `/step <id>` (`.claude/skills/step/`). Each step
says **Branch**, **After**, **Model**, **Author** (a decision or in-game work only the author can do; "none" means the session
goes ahead on its own) and **Done when**. A session that stops in the middle writes one line under "Where we stopped".

The agent does not run the game: what only the game client can show is the author's, with exact steps written by the agent.
Each collection step updates `docs/testing/functional.md`, `CHANGELOG.md` and the upgrade table
(`docs/testing/upgrade-0.1-to-0.2.md`, "Changes in 0.2.0 that need a case": new recipes and lang keys) in its own pull request,
as the rules ask; V5 brings the READMEs and `docs/domain.md` up for all three at once. The golden stacks of 0.1.1 stay green
through every step (`UpgradeTests`).

## Steps

- [ ] **V1. One resolve, one filler.** The two tails the core's code review left for this item (`ROADMAP.md`, Technical, "The
      collections' code, one resolve and one filler"; K7 H8/H9 and A5 in the core plan's Decisions): the ladder that answers "which
      collection: found, switched off or gone" written once instead of in four places (store, items, network, hints), with one
      refusal type where the three enums say the same; `Album.Wanted` and `Invoice.Wanted` choosing the filler of a member by one
      algorithm; and that algorithm choosing a filler for which there is room (A5: today the first filler is picked before room
      is known, so a member of several items may take one when another would fit). No change the player sees but A5's.
      Branch: `feat/crystals`. After: the plan's own commit. Model: opus. Author: none.
      Done when: all test projects pass; a test for A5 fails on the old code; the `ROADMAP.md` line is removed.
- [ ] **V2. Ore crystals.** The definition `crystals.json` by C1 and A2: source `variants` on `game:crystalizedore-{grade}-{ore}-{rock}`;
      three levels (the richest of each ore, 16; ore × rock at its richest grade, 118; every combination, 353), the layouts of A2
      (level 2 ores as rows and rocks as columns; level 3 by ore and by rock, a table grade × rock or grade × ore); the 54
      combinations that never generate as the definition's `exclude`; the rocks' stones as icons; the grade names our own lang keys
      (`game:ore-grade-*` says "Quality: …", K1), en and uk; `anyKey` for rock and grade. The rock shown wherever a missing piece is
      listed (pieces of two rocks share a name, C1): at least the want list's lines. The album recipe (decided: as the tapestries'),
      its `albumRecipe`. Server tests: the counts 16 / 118 / 353 on the real registry, the album crafted from a crystal
      (`CraftingTests`), taking new and a want list on a level of several codes.
      Branch: `feat/crystals`. After: V1. Model: opus. Author: the in-game check (en, then uk): an album made from a crystal, the
      level chosen, tabs and both layouts at level 3, taking new from a chest, a want list naming the rock.
      Done when: tests pass, mutation-checked (an `exclude` dropped; `max` read as `any` on the real data); A16, A22, W5 of
      `functional.md` run on crystals instead of the test mod and passed; upgrade table: the recipe and the lang keys; `CHANGELOG.md`;
      the branch goes to `main` by a pull request.
- [ ] **V3. Seashells.** The definition `seashells.json` by C2: source `variants` on `game:seashell-{type}-{color}`; two levels
      (type, any colour, 7; type × colour, 42), level 2 a table type × colour; type and colour names our own lang keys (the game has
      only full names, K1; ours also avoid the vanilla uk bug of two codes sharing a name, C2), "мушля" in uk; rarity (seafoam,
      turquoise) as an attribute rule in the definition, data only (showing it is item 6's). The album recipe and `albumRecipe`.
      Server tests as V2 (counts, crafting, taking new at the type level with a shell of another colour).
      Branch: `feat/seashells`. After: V2 merged. Model: sonnet. Author: the in-game check (en, uk).
      Done when: tests pass, mutation-checked; cases in `functional.md` passed; upgrade table; `CHANGELOG.md`; pull request.
- [ ] **V4. Butterflies.** The definition `butterflies.json` by C3: source `variants` on `game:butterfly-dead-{type}`, members from
      the loaded items (169; `reedbutterfly` has no item); one level, no grouping (decided in item 6), pages by count; the game's
      own names. The album recipe and `albumRecipe`. Server tests: the count, crafting, taking new.
      Branch: `feat/butterflies`. After: V3 merged. Model: sonnet. Author: the in-game check (en, uk).
      Done when: tests pass, mutation-checked; cases in `functional.md` passed; upgrade table; `CHANGELOG.md`; pull request.
- [ ] **V5. The docs for players.** Both READMEs (the collections there are, their levels, the album recipes; that butterflies on
      shelves, display cases and display mods are not read yet, `ROADMAP.md`, Exhibits), together in en and uk; `docs/domain.md`
      (Albums and collections; the recipe table's album row for every collection).
      Branch: `docs/vanilla-collections`. After: V4 merged. Model: sonnet. Author: reads the texts (the READMEs are the ModDB
      page's source; shown at release as player-facing texts).
      Done when: the texts agreed; pull request.

## Decisions

- **One plan for the three collections** (the author, 09.11.2026): they share V1 and the same path (definition, lang, recipe,
  tests), each little more than data on the core.
- **A branch and a pull request per collection** (the author, 09.11.2026): V1 with V2, then V3, then V4. Each collection reaches
  `main` whole, so item 6 and G5 of the description plan can start on what is there. Rejected: one branch for all, as the core
  had (that was one structure; these are three independent pieces).
- **No code review of its own** (the author, 09.11.2026): R1 of the release plan reads it, as it reads the album window. The core
  changes only in V1; the rest is data.
- **The album recipe as the tapestries'** (the author, 09.11.2026): an empty ledger or bill of materials and a member of the
  collection as a tool that stays (a crystal, a shell, a dead butterfly); each recipe with its own `name`, named by its
  definition's `albumRecipe`.
- **Item 4 makes a collection collectable and readable; item 6 makes it look right** (the author, 09.11.2026). Here: the rock in
  the lists, names of grades, types and colours, the seashells' rarity as data. In item 6: grouping, order, the rarity mark on
  the cell, a member named by its values ("Native copper, bountiful") in the window, the grouping of butterflies.
- **`Member.Fills` and `Member.Shown` stay one** (the author, 09.11.2026; K7 A8): none of the three needs them apart (what fills a
  member is what it shows). The line stays in `ROADMAP.md` for berry bushes.
- **Malachite in marble is not excluded** (the author, 09.11.2026) until seen otherwise; an open point below.
- **The test mod's `ccores` and `ccshells` stay** (the author, 09.11.2026): `CompatTests` need a collection from another domain.
  The in-game checks move to the real collections.
- **Step letter V** (vanilla collections); A, B, C, F, G, K, P, R, S and T are taken.

Open points:
- Whether malachite ever generates in marble (C1): an in-game look, or the deposit order in `vsessentialsmod` (`GenDeposits`),
  when convenient. If it never does, its combinations join `exclude`.

## Where we stopped
