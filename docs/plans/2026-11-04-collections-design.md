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
- [x] **C2. Seashells.** The same for `seashell-{type}-{color}` (block `BlockSeashell`, 7 × 6 = 42): every source (worldgen
      patches, trader, loot, schematics); whether a picked-up shell is a stack that lies in a chest, and with what code
      (check in game if the sources do not say); lang names. Questions for the author: one piece is the type or type and
      colour; groups.
      Branch: `docs/collections-design`. After: the plan's own commit. Model: opus. Author: answers; the in-game check if needed.
      Done when: as C1.
- [x] **C3. Butterflies.** The same for `butterfly-dead-{type}` (169 types; Decisions, "Butterflies: dead only"): which types
      a player can catch in survival (the entity's spawn conditions by climate), the forms of a type (male, female, colour,
      season) and whether a species can be told from the code alone; how the display mods keep a butterfly (Butterfly
      Displays, Butterfly Cases - Reborn, Catch and Release: Butterflies; integration targets) and whether any of them adds
      live butterflies as items. Grouping by species is not decided here (Decisions, "Butterflies last"): only the facts it
      will need.
      Branch: `docs/collections-design`. After: the plan's own commit. Model: opus. Author: answers.
      Done when: as C1.
- [x] **C4. Beyond 0.2, a glance.** Not designed, only checked against the shape: fish, fruit trees, berry bushes, tapestries
      of other mods (`BlockTapestry` blocks with their own `sizes`), custom collections (the collection in the item), live
      butterflies of other mods. For each: what one piece would be and what the shape would need from it. The point is to see
      the shape does not exclude them, not to plan them.
      Branch: `docs/collections-design`. After: C1–C3. Model: opus. Author: none.
      Done when: a short table in Decisions: collection, one piece, what it needs from the shape.
- [x] **C5. Configs of mods.** How Vintage Story mods are configured in practice, so that we adopt what is common rather than
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
- [x] **C6. The cost of reading a book.** Measured, not guessed: a tooltip, the crafting checks and the window's hash every
      500 ms (`ROADMAP.md`, 0.2) on an album of 88, 169 and ~400 members, on the server and on the client side where the test
      server allows. A probe or a test that stays if it is cheap to keep.
      Branch: `docs/collections-design`. After: the plan's own commit. Model: sonnet. Author: none.
      Done when: the numbers and how they were taken are in Decisions, with a verdict: fine as is, or what the core must change.
- [x] **C7. The shape and the core's plan.** From C1–C6 and what the album view needs (`2026-11-04-album-view.md`, Decisions,
      "The view on paper (A2)", its last part "What the view needs from the shape": dimensions, layouts, groups, members and
      the stacks to show, flat textures, levels, the collection's name, `Disabled` on the client, and two changes of stored
      data, the chosen level and the code that filled a member; the player's side of levels and of a disabled collection is
      decided there): the
      shape of a collection (members and their keys, dimensions, groups, order, names, the definition's source, its settings;
      hierarchical and Cartesian dimensions, the levels of ore crystals as one album or three collections, members filled by
      any of several codes: Decisions, C1) and the changes to the core: the one collection resolver used by
      the store, the items and the network (today `Collections.Get` in three places), the registry as definitions, the
      config, what C6 asks. Each change with the collection that needs it. What changes stored data, recipes, item codes,
      config or lang is named for the upgrade table (`docs/testing/upgrade-0.1-to-0.2.md`). Then `ROADMAP.md`, 0.2, item 3
      becomes a link to the new plan of the core, and item 4 gets what each collection needs.
      Branch: `docs/collections-design`. After: C1–C6, A2 of the album view (merged into `main`). Model: opus. Author:
      approves the shape and the core's plan.
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

### Seashells (C2)

Facts, game 1.22.7, read 04.11.2026 (assets under `VintageStory-Dev/assets/survival/`, code in `game-src`):

- **The block** `seashell-{type}-{color}` (`blocktypes/aquatic/seashell.json`, class `BlockSeashell`): 7 types (scallop, sundial,
  turritella, clam, conch, seastar, volute) × 6 colours (latte, plain, seafoam, darkpurple, cinnamon, turquoise) = 42, no
  `allowedVariants`. A block "kept for backwards compatibility" (the file's comment): `Unplaceable`, `GroundStorable` (4 to a
  spot), `RightClickPickup`, `shelvable`, `displaycaseable`. The handbook has one page per type (`handbook.groupBy`).
- **In the hand and in a chest:** picked up by a right click with an empty hand (or a hand holding the same stack), or broken;
  either way the stack is the block itself, `game:seashell-{type}-{color}` (`BehaviorRightClickPickup.cs:48`, `Block.cs:1340#OnPickBlock`),
  up to 64 (`Collectible.cs:63`, not overridden). A block stack lies in a chest as any other; tapestries are block stacks too.
  Not checked in game: the sources say it.
- **Under water: the patch's list, uniform** (`BlockPatch.cs:253`, `BlockShell.cs:18#TryPlaceBlockForWorldGenUnderwater` places
  the block it is given). `worldgen/blockpatches/aquatic.json:241-288`, depth 2..16, rain ≥ 0.3, chance 0.2 per chunk:
  - tropics (11..40 °C), sea water: all 42;
  - tropics, fresh water: clam and turritella, any colour (12);
  - temperate (-20..11 °C), sea water: clam, scallop, seastar, turritella, volute in plain, latte, cinnamon (15);
  - temperate, fresh water: clam and turritella in plain, latte, cinnamon (6).
- **On land: any of 42, whatever the patch lists.** Four `NearWater` patches (`aquatic.json:289-328`, ≥ -5 °C, dry and wet
  climates, chance 0.1..2) name only clam and turritella in plain and latte, but `BlockShell.cs:45#TryPlaceBlockForWorldGen`
  ignores the code it was given and draws a new one: the type uniform of 7, the colour latte, plain, darkpurple or cinnamon
  (92%, each 23%) or seafoam or turquoise (8%, each 4%). Placed only on sand or gravel (`HasBeachyGround`), around a point of
  fresh water (`BlockPatchConfig.cs:46`, Gauss offset σ 5, `BlockPatch.cs:73`). So every combination occurs in a world
  without tropics, sundial and conch included; one of a rare colour is ≈ 0.6% of loose shells, a common one ≈ 3.3%.
  Likely not what the patch's author meant (its comments); a game update may narrow it.
- **Never generated: none.** All 42 occur in survival. Rarity differs: tropical sea and rare colours.
- **Other sources:** none. No trader sells or buys shells (`villager-liga.json` sells two shell bracelets and necklaces, items
  of their own); no loot table or panning gives them. Seven schematics hold 8 placed shells, all of the rarer kind
  (`resoarchive`, `tobiascave` 3, `treasurehunter`, `cathedral-of-tyrons-basement`, `monastery-of-fishing`, `trader/treehouse/hut1`,
  `underground/lakes/small9`): darkpurple conch and volute, seafoam sundial and scallop, turquoise turritella.
- **Used up by:** the quern (2 lime, `grindingProps`), a seashell headband and necklace (`recipes/grid/clothes/face.json:112`,
  `neck.json:244`, any shell). A collected piece can be used up, as a crystal.
- **Names** (`game/lang/en.json:7265-7306`): "{Type} ({Colour})", one per combination, all distinct; the description is
  "Sea shell". In `uk.json` scallop darkpurple is named as seafoam ("водянистого", line 6794): two codes share a name
  in Ukrainian, a vanilla translation bug. Unlike crystals, the name tells the piece.
- **Mods that give shells** (sources, not competitors; not on the author's server; the latest files from ModDB read on a
  scratch copy 04.11.2026): both give the vanilla codes, so their shells fill the album as they are, and neither knows of
  rarity.
  - **She Pans Seashells 2.0.0** (content mod, game 1.22.0+): one patch (`patches/shellspanning.json`) adds all 42
    `game:seashell-*` to the panning drops of sand, gravel and wavy sand, each at 0.001 per pan (≈ 4% for any shell).
  - **Seashell Ore 1.0.0** (code mod, game 1.19.8, "To be reworked", last release 27.06.2024): a deposit of its own block
    `ore-seashell-{rock}` in 7 sedimentary rocks near the surface; broken, it drops one `game:seashell-{type}-{color}`, the type
    and colour drawn from all 42 alike (decompiled `BlockSeashellOre.GetDrops`); a rare `seashell-conch-plain` on the deposit's
    surface. Whether it still loads on 1.22 was not checked.

The author's answers (04.11.2026):

- **Two levels:** 1. **the type**, 7 pieces, each filled by a shell of that type in any of its 6 colours (a member filled by any
  of several codes, as level 1 of the crystals); 2. **type and colour**, 42 pieces.
- **Dimensions:** type and colour (Cartesian, all 42 points allowed). No "tropical" dimension: every shell generates anywhere
  (on land).
- **Rarity is a property of a piece, not a group:** the question is "is this shell rare", shown on its cell (a mark, a frame),
  not "which shells are rare" as a section of the album. The shape needs per-member attributes beside its dimensions (C7, A2).
- **What players do with shells** (the author's server): everything: collect, grind to lime, make jewellery.

Open points of C2:
- What "rare" is: the game's own split is the colour (seafoam and turquoise, `BlockShell.cs:13`, a constant in code, not in
  assets); the tropical sea and the schematics make some rarer still. A default in the definition, changeable by config? C5, C7.
- That land gives any of 42 in practice: the author has not noticed (04.11.2026). An in-game look at a temperate lake beach
  (sundial, conch or darkpurple among the shells) when convenient; the code is clear, so nothing waits on it.

### Butterflies (C3)

Facts, game 1.22.7, read 04.11.2026 (assets under `VintageStory-Dev/assets/survival/`, code in `game-src`, the engine's
`ServerSystemEntitySpawner` decompiled):

- **The item** `butterfly-dead-{type}` (`itemtypes/creature/creature-butterfly-dead.json`, class `ItemDeadButterfly`): 169 types,
  no `allowedVariants`; stack 64, `shelvable`, `displaycaseable`; one handbook page for all (`handbook.groupBy`). No recipe, trader,
  loot table or schematic gives or uses one: a collected butterfly is never used up, unlike crystals and shells.
- **How a player gets one: only the bug net** (`recipes/grid/butterflynet.json`, `bugnet-copper`). A hit on a butterfly kills it
  and gives the item named by the entity's `netCaughtItemCode`, `butterfly-dead-{type}` (`entities/animal/invertebrate/butterfly.json:10`,
  `ItemBugnet.cs:49-51`).
- **The entity has 170 types, the item 169: `reedbutterfly` has no item** (nor a name in lang; it has a texture). Its spawn
  conditions have a climate but no `chanceByType` or `maxQuantityByType` entry, so the defaults apply (`Chance` 1, `MaxQuantity` 20,
  `SpawnConditions.cs:265,273`; a `*ByType` with no matching key is dropped, `RegistryObjectType.cs:306#solveByType`): it spawns
  more readily than any other type in its climate. Caught, it dies first, then `new ItemStack(null)` throws (`ItemBugnet.cs:49-51`,
  `ItemStack.cs:225`): the player gets nothing. Not checked in game.
- **Spawning: at runtime only** (no worldgen spawn), `group: "passive"`, on the surface, inside a `flower-*` or `tallgrass-*` block,
  full daylight (`butterfly.json:275-286`). Per type a climate box, `spawnconditions.climateByType`:
  - temperature and rain are the yearly worldgen values, taken at a fixed height of 1.09 × sea level, not at the spawn point
    (decompiled `GetSuitableClimateTemperatureRainfall`); forest at the spawn point; `minY`/`maxY` at the spawn point, 0..1 below
    sea level, 1..2 from sea level to the top of the world;
  - also, the current temperature at the spawn point (season, time of day) must be ≥ 0 °C, or the spawn is refused
    (`EntityButterfly.cs:36`) and a loaded butterfly removed (`EntityButterfly.cs:73`). The seasonal swing grows with latitude,
    up to 65 °C near the poles (`Temperature.cs:75`): butterflies of a −20..−10 °C climate fly on warm summer days only.
- **Rarity is in the assets:** `chanceByType` in six steps, and `maxQuantityByType` follows it (1 to 12): 0.03 (55 types), 0.02 (44),
  0.01 (38), 0.003 (19), 0.0003 (7), 0.00003 (6: apollo female and male, two *Ornithoptera priamus* and Queen Victoria's and
  Wallace's golden birdwing). The rarest is a thousand times rarer than the commonest. All butterflies together are capped at 20
  (`maxQuantityByGroup`, ×0.4 per extra player).
- **Which types a player can catch in survival:** all 169 in principle; by climate box (types share 24 boxes):
  - most boxes overlap the climate of a flower or tall grass patch (`worldgen/blockpatches/flower.json`, `grass.json`, the
    `tallgrass` layer of `worldgen/blocklayers.json`);
  - **cold desert** (−5..5 °C, rain ≤ 0.1, forest ≤ 0.2; 24 types, plus `reedbutterfly`): no flower grows there and the tall grass
    layer needs rain ≥ 0.2; only the tall grass patches of `grass.json:92,103` (rain ≥ 0.12) fit. A patch's rain is taken at its own
    height and rises near sea level (`Climate.cs:38#GetRainFall`), while the butterfly's is taken at 1.09 × sea level, so these
    types can spawn on low ground a few blocks above sea level, or high up at the wetter edge of the box: rare by place, whatever
    their chance;
  - **hot desert** (`africanmapwing`, 35..45 °C, rain ≤ 0.1): worldgen temperature tops at 40 °C (`Climate.cs:62`), so only the
    hottest deserts; tall grass patch `grass.json` 20..40 °C, rain 0.1..0.15, and the orange mallow fit;
  - **the top of the world** (`africancloudedyellowmale`, `minY` 1.9: the top tenth of the height above sea level, hot and semi-arid):
    only on the highest peaks; `acmonblue*` (1.6..1.9) and eleven others (1.5..1.8) are mountain butterflies;
  - **players can make a place:** a planted flower is a `flower-*` block, so a flower on soil in the right climate (and height) is a
    spawn point. Not checked in game; whether the surface map counts a planted flower was not read.
  - The analysis compares boxes, not a world; nothing was checked in game.
- **Forms of a type** (the code and the English name): sex (59 types end in `female`, 52 in `male`, 58 have neither), female colour
  morphs (blood-red glider pale/red, Aega morpho blue/brown, dawn clouded white/yellow, southern dogface white/yellow, orange-barred
  sulphur *thalestris* red/yellow, large orange sulphur white), seasonal forms (chocolate albatross and tropical white dry/wet season,
  map butterfly spring/summer, eastern comma summer form) and named forms (lemon emigrant *alcmeone*/*hilaria*). **Seasonal forms are
  not seasonal in game:** the spring and summer map butterfly have the same spawn conditions; nothing in the spawn depends on season.
- **The species cannot be told from the code:** suffixes are irregular (`bloodredgliderpalefemale` vs `bloodredglidermale`; `comma`,
  `commaabdilutus`, `easterncommasummerform` and `falsecomma` are three species). **The Latin name is in lang:**
  `itemdesc-creature-butterfly-{type}` (`game/lang/en.json` from line 12063; shown in the tooltip, `ItemDeadButterfly.cs:21`) holds
  the binomial, the same in `uk.json`: 117 distinct names, 110 species by genus and species (the 7 extra are subspecies and
  aberrations: *ab. dilutus*, *ab. schmidtii*, *Miokensis*, *euphorion*, *thalestris*, *maxima*, *cornelia*, *belides*), 75 genera.
  By the 117 names: 72 have one type, 37 two, 8 three. A source for a grouping, read from the assets, not at runtime from lang:
  a translation may change it.
- **Names:** "Dead {name} ({form})" (`item-deadbutterfly`, `ItemDeadButterfly.cs:14`), all 169 distinct in `en` and `uk`, the forms
  in brackets ("Map Butterfly (male, spring form)"). Unlike crystals, the name tells the piece.
- **Where collected butterflies lie: displays more than chests.** Vanilla shelves and display cases, and all three display mods
  (integration targets; the latest files from ModDB, or the author's `ModsByServer`, on a scratch copy 04.11.2026) keep the vanilla
  `game:butterfly-dead-*` item in a `BlockEntityDisplay` (a `BlockEntityContainer`) inventory, one butterfly per slot. Today the books
  read none of these (`docs/domain.md`, "not supported yet"):
  - **Butterfly Displays 1.2.1** (modid `butteflydisplay`, name "Butterfly Plaques", on the author's server): plaques of 4 slots,
    any item with `displayplaqueable`, which it patches onto `butterfly-dead`;
  - **Butterfly Cases - Reborn 1.0.4**: dome, wall and slanted cases of 1 or 4 slots, any `displaycaseable` item;
  - **Catch and Release: Butterflies 2.1.2** (with Catch and Release LIB 1.1.5): **the net then gives a live butterfly**, the vanilla
    item `game:creature-butterfly-{type}` (patch of `netCaughtItemCode`); it perishes into `butterfly-dead-{type}` after 48 hours
    (its config `LiveButterflyPerishHours`), or a one-ingredient grid recipe makes it dead at once. Cages and terrariums keep live
    items (`validItems: game:creature-butterfly-*`) in a `BlockEntityDisplay` and show a caged entity. Whether perishing stops in a
    cage was not read. With this mod a collector holds live butterflies first: "dead only" (Decisions) means the album sees them once
    they die or are crafted.
  - Not read: Butterfly display frame, Dusiulka's Butterfly Pin Board, the optimization patch of Catch and Release (ModDB search
    04.11.2026).

The author's answers (04.11.2026):

- **Every type that has an item is in the album,** the hard-to-reach ones too (cold desert, hot desert, the top of the world).
  `reedbutterfly` has no item, so it cannot be a member.
- **Rarity is a property of a cell, not a dimension;** perhaps a key to sort by. Marked in several tiers (e.g. rare, epic,
  legendary), decided with the rarity feature (`ROADMAP.md`, Maybe), not here. As for seashells (C2): the shape needs per-member
  attributes beside its dimensions (C7).
- **Players on the author's server collect butterflies:** one collector keeps them both in chests and on wall plaques
  (Butterfly Displays). So reading display blocks (`ROADMAP.md`, Exhibits) matters for this collection; for C7 to weigh.
- **Catch and Release is not on the author's server** (not in `ModsByServer`, 04.11.2026). An album of live butterflies with it:
  `ROADMAP.md`, Maybe.

Open points of C3:
- Grouping by species (Decisions, "Butterflies last"): the Latin names give 110 species or 117 names; whether the grouping is
  built from them (read from the assets at build time) or written by hand.
- `reedbutterfly`: report to the game's developers? A fix would add a 170th item, and the album would need it (C5, C7: members
  from the loaded items, not a fixed list).
- In game, when convenient: catch a cold-desert type near sea level; whether a planted flower becomes a spawn point.

### Beyond 0.2, a glance (C4)

Not designed: only what one piece would be and what the shape would need from it. Game 1.22.7, read 04.11.2026 (assets under
`VintageStory-Dev/assets/survival/`, code in `game-src/vssurvivalmod`).

| Collection | One piece | What it needs from the shape |
|---|---|---|
| **Fish** | A species: `{habitat}-{type}-{species}`, 56 allowed (`itemtypes/resource/taxidermy-fish.json`, `allowedVariants`). A catch is `fishraw-{habitat}-{type}-{species}-{age}`, adult or juvenile by the spot's abundance (`EntityBobber.cs:338`), and rots (fresh 36 h, `fishraw.json:661`); the lasting form is the trophy `taxidermy-fish-…-adult` (barrel with borax or zinc sulfate, `recipes/barrel/taxidermy-fish.json`), 56 codes, stack 4..64. `creature-fish-*` (live, `ItemCreature`) has no source in survival assets. | Members by code, as crystals. Habitat and type are dimensions (Cartesian, only some points allowed). Which form counts (the trophy only, or a raw fish too: one member filled by several codes, C1) is the collection's choice. A piece that rots in a chest: the album recorded it, the item is gone (as crystals crushed, C1). |
| **Fruit trees** | A tree type: the cutting, block `fruittree-cutting` with attribute `type` (12 types, `fruittreebranch.json:6-20,273-284`; drops set it, `BlockFruitTreeBranch.cs:197#GetDrops`; a dead tree's cutting drops as firewood). The fruit (`fruit-{fruit}`) rots and is shared with other plants. | Members by code **and attribute**, as tapestry pieces (`tapestry-north` + `type`): today's `ItemKey` holds it. The list comes from the block's `worldgen` attribute or the creative stacks, not from variants. Climate per type is in the asset (a rarity source, as butterflies). A planted tree is "a cutting on the wall" (`ROADMAP.md`, Exhibits; the author). |
| **Berry bushes** | **A bush type with a good trait** (the author, 04.11.2026): 10 types × 4 = 40 pieces. The cutting `fruitingbushcutting-{type}-free` (10 types, all in `worldgen/blockpatches/berrybush.json`) carries the bush's traits in the attribute `traits`, comma-joined in the order of the pairs (`BEBehaviorFruitingBush.cs:606#SetHarvested`). Four pairs, bad / good: shybearer / heavybearer, thinskinnedfruit / thickskinnedfruit, weakrooted / strongrooted, weakclusteredberries / strongclusteredberries (`FruitingBush.cs:33`). Only a wild bush draws them: each pair 15%, then bad 60% or good 40% (`BEBehaviorFruitingBush.cs:314#genTraits`); a given good trait ≈ 6% of wild bushes (with no bad trait beside it ≈ 4.5%), two ≈ 0.4%, all four ≈ 1 in 77 000. A bush grown from a cutting inherits its traits (`BEBehaviorFruitingBushCutting.cs:32`); no crossing in vanilla. The cutting perishes into sticks after 360 h (`fruitingbushcutting.json:84`); the berry `fruit-{type}` rots. Legacy `bigberrybush`/`smallberrybush` are not in creative or worldgen. | **A member as a rule on an attribute:** "blueberry, strongrooted" is filled by any blueberry cutting whose `traits` holds `strongrooted` and no bad trait (other good ones may be there; the author, 04.11.2026); today's `ItemKey` tells the cuttings apart, the match from key to member is new. Dimensions: type × good trait (Cartesian). A second level of the same collection: type × the full set of traits, reachable with genetics mods (the author). The best specimen per type is another album, one that improves (`ROADMAP.md`, Maybe). A planted bush is "a cutting on the wall" (`ROADMAP.md`, Exhibits): whether cuttings perish in a chest does not matter now. |
| **Tapestries of other mods** | A piece, as vanilla: a `BlockTapestry` block of its own code (`craftablecompanion:tapestry-*`, `astriaporta:astriaportatapestry-*`; `ROADMAP.md`, Later) with its own `sizes`. | The definition read from **every block of a class**, not from one code; members of several domains. One collection or one per mod (a group per mod, or a dimension "mod"): the shape takes both. |
| **Custom collections** | Whatever the player lists (`ROADMAP.md`, Next, 3): any item key. | The definition's source is **the book item**, not the registry: the resolver takes a book (stack), not only an id. No dimensions, or a flat group the player names; order as written. Settings in the item, not in the config. |
| **Live butterflies** (Catch and Release) | `game:creature-butterfly-{type}`, the same 169 types (C3), given by that mod's net. | A collection of its own (Decisions, "Butterflies: dead only") over a second family of codes with the **same dimensions and groups** as the dead one: the grouping is defined once and applied to both. Its members perish into members of the other collection (48 h, C3). Members exist only with the mod: the definition is built from the loaded items. |

What the shape must not exclude, from this table: members keyed by code and attributes (fruit trees, as tapestries); a definition
read from a class of blocks across domains (tapestries of other mods); a definition held in the book (custom); a grouping
shared by two collections (butterflies dead and live); members that perish (fish, cuttings, live butterflies). **New beyond
C1–C3: a member filled by a rule on an attribute** (berry bushes: a trait in `traits`), next to members filled by several codes
(C1, C2). The rest needs no more than C1–C3 asked for: dimensions Cartesian or hierarchical, attributes per member.

### Configs of mods (C5)

Game 1.22.7 (`game-src`), ConfigLib 1.13.2 (an integration target, read decompiled, 04.11.2026), ModDB cache of 02.11.2026
(the latest cached release of each of 9238 mods).

How mods are configured:
- **The game's own** (`api.LoadModConfig` / `StoreModConfig`, JSON in `ModConfig`): each side keeps its own copy, never synced
  (`vsapi ICoreAPI.cs:139-173`: "synchronize it via network packet"). Ours today.
- **World config** (`worldconfig.json` at the mod's root, `Mod.cs:30`): stored in the world, set at world creation or by
  `/worldconfig`, seen by clients (`capi.World.Config`, `GuiDialogTrader.cs:46`). Scalars only (`WorldConfiguration.cs:15`:
  bool, numbers, string, dropdown), no lists. Rare: 19 of 9238 mods.
- **ConfigLib** (Maltiez): 712 000 downloads, the next config library 97 000 (Auto Config Lib, built on ConfigLib); on the
  author's server with `vsimgui`. 346 mods ship a `configlib-patches.json` (of about 4300 with a DLL). A soft dependency by
  design: without it the defaults in the assets apply. ConfigKit (5 000) reads the same files and refuses to run beside ConfigLib.
  - It edits **the mod's own JSON file in place** when its asset says `"file": "<name>.json"` (`ConfigLibModSystem.cs:399-404`),
    else its own `ModConfig/<domain>.yaml`. A setting's code is a path into the file, `/`-separated (`JsonObjectPath.cs:18`); it
    reads by path, a missing key gives the default (`Config.cs:998`), and writes only the paths it knows, the rest kept
    (`Config.cs:973`). In JSON mode no `version` check resets values (YAML only, `Config.cs:918`).
  - Server settings reach clients through a recipe registry (synced on join, `ConfigRegistry.cs:35,54`) into ConfigLib's own
    objects; on a multiplayer client it writes only `clientSide` settings to the file (`Config.cs:256-263`), so **a mod reading
    its file on the client still sees the client's values**. Only `controlserver` changes server settings (`ConfigLibModSystem.cs:556`).
  - It pushes `configlib:<domain>:config-saved` and `:setting-changed` on the game's event bus (`ConfigLibModSystem.cs:448,501`):
    a mod can listen by name, with no reference to ConfigLib's DLL.
  - Types: bool, integer, float, string, colour, other (`ConfigSettingType.cs`); how its window edits a list (other) not checked.
- **Data as assets**, the game's own way: `config/*.json` (`farming.json`, `traits.json`, `tradelists/`, `handbook/`) read by
  `Assets.Get` / `GetMany`, the latter across all domains; other mods and admins change them by JSON patches (4318 of 9238 mods
  have `patches/`). The `config` category is universal (`AssetCategory.cs:16`) and clients download the server's mods
  (`ModsByServer`), so both sides read the same definitions; a patch with `"side": "server"` does not reach the client
  (`JsonPatchLoader.cs:254`). `blocktypes` and `itemtypes` load on the server only (`AssetCategory.cs:11-12`) and reach the
  client through the registry.

Options for the settings of collections, with their costs:
- A. Flat keys per collection (`CrystalExclude`, `SeashellRare`…): keys grow as collections × settings; no room for other mods'
  collections.
- B. A dictionary by collection id in `curiocatalogue.json`, definitions in code: scales, but another mod cannot bring a collection.
- C. Definitions as assets (`config/` of any domain), changed by JSON patches: as the game does, the same on both sides with no
  packet, other mods bring collections without our release; but every change by an admin needs a patch mod.
- D. C, plus a dictionary of overrides by collection id in `curiocatalogue.json`: one file for the admin; the packet on join
  (`ROADMAP.md`, Later) carries this file only.

Decided (the author, 04.11.2026):
- **D.** Collection definitions are assets in `config/` (a folder of ours, read from every domain); their format is C7's. Defaults
  of the open points of C1–C3 go into the definition: combinations that never occur (crystals), what is rare (seashells),
  members from the loaded items (butterflies). The admin overrides in `ModConfig/curiocatalogue.json`, under `Collections`, by id:
  ```json
  "Collections": { "tapestry": { "Exclude": ["aid-*"], "Add": [] }, "seashells": { "Disabled": true } }
  ```
  An open set of fields: an older version ignores a field it does not know. **In 0.2: `Exclude`, `Add`, `Disabled`.** What a
  disabled collection does to its album recipe and to albums already made is C7's (the player sees it).
- **`TapestryExclude` and `TapestryAdd` migrate** into `Collections.tapestry` in 0.2: read once, moved, removed from the file;
  a line in `CHANGELOG.md` and the READMEs' config section; a case in the upgrade table (`docs/testing/upgrade-0.1-to-0.2.md`).
  The golden config test then checks this documented outcome for these two keys (B, "What does not survive goes into the changelog").
- **ConfigLib: not in 0.2, in Next** (`ROADMAP.md`, After 0.2). 0.2 must not stand in its way: the settings stay in one JSON
  file `ModConfig/curiocatalogue.json`, each value at a stable path (`Collections/<id>/<field>`); no `version` key; the parts
  built from the config can be rebuilt on a running game, or at least nothing in their structure forbids it.

### The cost of reading a book (C6)

Final. Measured 04.11.2026 by `CurioCatalogue.ServerTests/ReadingCostTests.cs` (stays: a loose budget of 100 ms catches a read that
turns quadratic). The collection grows by the config's `TapestryAdd` (88 real pieces + made-up names), so the path is the real one
(registry, store, core). Median of 100-200 runs on the server's main thread, **Debug build**, this laptop; milliseconds per call.
An album of all members missing unless marked.

| Members | Album's data | Read (`ReadLedger`) | Read, half collected | Tooltip text (`GetHeldItemInfo`) | `Fingerprint` | `Clone` of the stack | Grid change with the album in it |
|---|---|---|---|---|---|---|---|
| 88 | 8 KB | 0.17 | 0.11 | 0.49 | 0.09 | 0.06 | 0.91 (empty ledger 0.05) |
| 169 | 15 KB | 0.26 | 0.17 | 0.64 | 0.15 | 0.05 | 0.73 |
| 400 | 37 KB | 0.69 | 0.45 | 1.16 | 0.17 | 0.11 | 1.99 (empty ledger 0.06) |

- Everything grows about linearly with the members, and 400 is under 2 ms for any single operation. The window's hash every
  500 ms is 0.2 ms at worst (0.04% of a core); a read when the data changed 0.7 ms; crafting checks (a grid change with the album in
  it: several recipes ask `MatchesForCrafting`, each a full read) 2 ms at 400, once per click on the grid.
- **Verdict: fine as is for the sizes planned.** The core needs no change for reading cost. The only thing worth keeping in
  mind for the core: `RefreshIfStale` builds a key set per read (inside the numbers above).
- **Not measured (the test server has no client):** the window's recompose (`GridDialog.Refresh` → `Compose`: 400 slots, textures;
  A1 of the album view plan) and the tooltip's drawing.
- **A finding, checked in game 04.11.2026: the tooltip recomposes at frame rate for a moment.** The game's tooltip calls
  `GuiElementItemstackInfo.SetSourceSlot` on every frame the stack is hovered (vsapi `ItemstackComponentBase.cs:67-81`, `RenderItemstackTooltip`),
  which recomposes (`AsyncRecompose`, calling `GetHeldItemInfo`) unless `ItemStack.Equals` says the stack equals its copy
  (`GuiElementItemstackInfo.cs:252-258`). `ArrayAttribute.Equals` compares the elements with `a[i].Equals(b[i])`
  (vsapi `Other/ArrayAttribute.cs:26`) and `TreeAttribute` has no `Equals(object)`, so a tree array (our `entries`, `members`)
  never equals even its own clone. The server test shows it: `full.Equals(world, full.Clone(), IgnoredStackAttributes)` is false at
  88, 169 and 400. Result (a probe line in `GetHeldItemInfo`, dev client, the author hovering an album and a ledger in the inventory and on the ground): the text is built **every frame (~60 per second)** in runs of 72-73 calls (~1.2 s) each time the tooltip appears, then no more while the cursor stays; items on the ground are asked every 500 ms (the block info). So the rebuild is real, but short (at most ~75 builds per hover, ~1 ms each at 400 members, asynchronous): **not a problem**; no change in the core. The reason for the stop after ~1.2 s is not known (not looked for). The probe is removed.
- Not measured, by reasoning: the inventory slot names (`GetHeldItemName`) read only the name, cheaply (`LedgerStore.ReadName`).

### The shape and the core's plan (C7)

Final. Decided with the author 07.11.2026: ten proposals (R1–R10) accepted as made. The shape, with the alternatives, is in
`docs/decisions.md`, "Collections"; the core's plan is `2026-11-07-collections-core.md` (K1–K6). In short:
- R1 a definition is data (`config/curiocatalogue/collections/<id>.json`, every domain); members come from a source in code
  (`variants`, `tapestry`; later the book, a rule on an attribute);
- R2 a member: a point in the dimensions with a stable id, its filler (one key or several), stacks to show, attributes;
- R3 levels as projections (`keep`, and `any` / `max` for each dimension dropped): the three levels of crystals (C1), the two
  of seashells (C2);
- R4 dimensions with ordered values, lang names and icons; layouts per level (group by, table / shape / order), the default
  giving the order of taking new;
- R5 attributes by rules in the definition (rarity); what 0.2 shows of them is item 6's;
- R6 `Collections.<id>.Exclude` / `Add` on the source's own name of a member (a code, a piece name); the 54 crystals that never
  generate as the definition's default `exclude`; the config id is the stored `tapestries` (C5's example said `tapestry`);
- R7 the album stores what it collected (member id and the stack that filled it) and its level; format 2, migrated from 1;
- R8 a want list's line names a member; format 1 read as exact entries;
- R9 one resolver answering `Found | Disabled | Unknown`;
- R10 the server's `Collections` sent to clients on join; the recipe of a disabled collection removed at load (to check in K1).

Left open, for later plans: deposits as the source of "never generates" (C1); grouping of butterflies (item 6); reading display
blocks (Exhibits); malachite in marble (C1, in game).

## Where we stopped

- 04.11.2026: C1 done (final): the facts, the author's answers and the open points in Decisions, "Ore crystals (C1)". Next: C2
  (opus, After: the plan's own commit).
- 04.11.2026: C2 done (final): the facts, the author's answers and the open points in Decisions, "Seashells (C2)"; the land
  placement surprise in `docs/modding.md`. Next: C3 (opus, After: the plan's own commit).
- 04.11.2026: C3 done (final): the facts, the author's answers and the open points in Decisions, "Butterflies (C3)"; the
  `*ByType` default surprise in `docs/modding.md`; two lines in `ROADMAP.md` (rarity, Catch and Release) and one to Exhibits.
  Next: C4 (opus, After: C1–C3) or C5 (opus) or C6 (sonnet).
- 04.11.2026: C4 done (final): the table in Decisions, "Beyond 0.2, a glance (C4)", with the author's answer on berry bushes;
  `ROADMAP.md`: plants in Exhibits, "An album that improves" in Maybe. Next: C5 (opus) or C6 (sonnet); C7 after both.
- 04.11.2026: C5 done (final): the research, the options and the author's choice in Decisions, "Configs of mods (C5)" (D;
  `Exclude`, `Add`, `Disabled`; the two tapestry keys migrate; ConfigLib in Next); the `"side": "server"` trap in
  `docs/modding.md`; ConfigLib in `ROADMAP.md`, After 0.2. `docs/architecture.md` gets the decision in C7. Next: C6 (sonnet);
  C7 (opus) after it.
- 04.11.2026: C6 done (final): the numbers and the verdict (fine as is) in Decisions, "The cost of reading a book (C6)"; the test
  `ReadingCostTests` stays. The tooltip check in game is done and harmless (see there). Next: C7
  (opus, After: C1–C6).
- 04.11.2026: C7 waits for A2 of the album view (its `After:` now says so); A1 and A2 were updated by C1–C6 (album view plan,
  Decisions). Next here: C7 (opus, After: A2 merged into `main`).
- 07.11.2026: C7 done (final): the shape in `docs/architecture.md`, Part 1, "Collections"; the core's plan
  `2026-11-07-collections-core.md`; `ROADMAP.md`, 0.2, items 3 and 4 updated. The plan is done; the branch goes to `main` by a
  pull request. Next: K1 of the core's plan (opus, After: this branch merged).
