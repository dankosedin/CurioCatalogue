# Every vanilla collection, polished in the album

Started 09.11.2026. `ROADMAP.md`, 0.2, item 6: back to the tapestries, ore crystals, seashells and butterflies (last), each
one's look in the stamp-album view (grouping, order, names), and its album recipe if it needs a change. Built on the album
window (`2026-11-04-album-view.md`, A2 and A4 in its Decisions) and the collections of item 4
(`2026-11-09-vanilla-collections.md`); what each collection is was decided in C1–C3 of `2026-11-04-collections-design.md`.
The line between the two items (item 4's Decisions): item 4 makes a collection collectable and readable, this item makes it look
right. Screenshots are not here: R3 of `2026-11-04-release-0.2.md`, by the shot list of G5.

## How to work on this plan

As in the previous plans: one step per short session with the project skill `/step <id>` (`.claude/skills/step/`). Each step
says **Branch**, **After**, **Model**, **Author** (a decision or in-game work only the author can do; "none" means the session
goes ahead on its own) and **Done when**. A session that stops in the middle writes one line under "Where we stopped".

The agent does not run the game: what only the game client can show is the author's, with exact steps written by the agent.
The look steps (L2, L3, L4b, L11) are driven by the author's notes from the game: the plan names what to look at, not what is wrong.
Each step updates `docs/testing/functional.md`, `CHANGELOG.md` and the upgrade table (`docs/testing/upgrade-0.1-to-0.2.md`,
"Changes in 0.2.0 that need a case": lang keys, recipes) in its own pull request, where they apply; the READMEs where the player
sees a change.

## Steps

- [x] **L1. Members named by their values.** One rule for naming a member wherever members are listed (a want list's line, the
      title of a cell's tooltip, a chat message that names a piece), from its values in the dimensions it keeps and the freedom
      of those it drops: "Nightfall, 2 of 3" for a tapestry piece (`ROADMAP.md`, 0.2, item 6: pieces of one picture share a name),
      "Native copper, bountiful, andesite" for a crystal, "Scallop, any colour" for a seashell at the type level. Replaces the
      first filler's name plus the freedom (K5 of the core plan left this here). The phrasing in en and uk per collection, by the
      definition (each language's grammar, as `progressKey` and `anyKey`).
      Branch: `feat/member-names`. After: V3 merged. Model: opus. Author: the phrasing for each collection, the in-game check (en, uk).
      Done when: tests pass, mutation-checked; the cases in `functional.md` passed; upgrade table: the lang keys; pull request.
      Split in two sessions (the author, 10.11.2026), one branch and one pull request:
  - [x] **L1a. The core and the data.** `DefinedCollection.NameParts` in place of `Listed`, `LevelDef.MemberKey`, `inLists` gone;
        the tapestries' `section` from `TapestryPieces`; the four definitions, the section and member lang keys (en, uk); the
        reader and its tests; `docs/decisions.md` and `docs/domain.md`. Done when: core and adapter tests pass, mutation-checked.
  - [x] **L1b. Shown, and released.** One adapter helper for a value's name (`Lang.GetMatchingIfExists`) and a member's name
        (parts, lower case after the first, `memberKey`, freedom; no part: the first item's name); the want list sheet's line,
        the album cell's tooltip, the want list window's slot; the window's `ValueName` through the helper; the server tests
        (`CrystalsTests`, `SeashellsTests`, `CollectionsFromAssetsTests`); `functional.md`, the upgrade table, `CHANGELOG.md`; the
        in-game check (en, uk); the pull request. Done when: L1's "Done when". Model: opus. After: L1a.
- [x] **L2. Ore crystals: the look.** The author plays the crystal album (all three levels, both layouts) and writes what is
      wrong: the order of ores and rocks (the definition's value lists), the names of levels and layouts, table headings, the
      default layout, the tabs. The agent fixes by the notes. The album recipe kept or changed.
      Branch: `feat/crystals-look`. After: V2 merged. Model: opus. Author: plays, writes the notes, checks the fixes.
      Done when: the author's notes are fixed or rejected with reasons (Decisions); pull request.
      The notes (10.11.2026), each in Decisions: the table of `rocks` fullest first (tried, rejected in game); the row of
      `richest` and the frame of a tab's page (to L6); icons for words (L2b); the album recipe kept.
- [x] **L2b. Ore crystals: icons for words.** Ores as the game's nuggets (`nugget-{ore}`; gold and silver in quartz or galena by a
      value → icon table, the game has one `nativegold` and one `nativesilver`), the grade as one round mark of four sizes. How the
      definition carries an icon per value: decided at the start of the session (Decisions, "Icons for words"). The words stay in
      tooltips. Split 10.11.2026 (the author): the dropdown with icons is L2c.
      Branch: `feat/crystals-icons`. After: L9 merged. Model: opus. Author: the icons, then plays and checks.
      Done when: the decisions in Decisions; tests pass, mutation-checked; the author's in-game check; pull request.
- [x] **L2c. A dropdown with icons.** The level, layout and tab choices with icons: a dropdown of our own (closed: the icon and the
      name, or the icon alone when short of room; open: lines of icon and name, scrolled when long: 17 tabs of crystals, the
      butterflies' species after L4a), and the side tabs with an icon beside the name. The icons of levels and layouts (an `icon`
      on `levels` and `layouts` in the definition, the same forms as a value's) decided at the start, before code; a look at
      every collection as in "Icons for words". The element lands in `AlbumControls` (L9).
      Branch: `feat/icon-dropdown`. After: L2b merged. Model: opus. Author: the icons of levels and layouts, the in-game check.
      Done when: tests pass, mutation-checked; A27 and a case of its own in `functional.md` passed; pull request.
      Split in two sessions (the author, 10.11.2026), one branch and one pull request:
  - [x] **L2ca. The icons as data.** `LevelDef.Icon`, `LayoutDef.Icon`, read from `icon`; `Adapters/DefinitionIcons` finds an icon
        for the window (the page's headings moved to it) and for the server test that every icon is in the game; `crystals.json`,
        `seashells.json`; `docs/decisions.md`. Nothing the player sees. Done when: core, adapter and server tests pass, mutation-checked.
  - [x] **L2cb. The dropdown and the tabs.** Our own dropdown element in `AlbumControls` for the level, the tabs and the layout
        (an item drawn as a stack, so not vanilla's `AddDropDown`: it composes its VTML names into a static texture, vsapi
        GuiElementListMenu.cs:237, where an `<itemstack>` is not drawn); the side tabs with an icon; `functional.md` (A27 and a
        case of its own), `CHANGELOG.md`, `docs/domain.md` ("Album window"); the in-game check; the pull request.
        Done when: L2c's "Done when". Model: opus. After: L2ca.
- [x] **L3. Seashells: the look and rarity.** Whether 0.2 shows the rarity corner of A2 (a coloured triangle by the attribute the
      definition of V3 gives seafoam and turquoise; `ROADMAP.md`, Maybe, "Rarity in the album", for the full feature); if so, the page
      element draws it and the cell's tooltip names it; its colour and name from the definition, not code (other mods' authors
      have only the asset; `ROADMAP.md`, item 6, 10.11.2026). Then the author's notes from the game, as L2. The album recipe kept or changed.
      Branch: `feat/seashells-look`. After: V3 merged. Model: sonnet. Author: decides on the corner, plays, writes the notes.
      Done when: the decision in Decisions; the notes fixed or rejected; pull request.
- [x] **L4a. Butterflies: the grouping on paper.** By species, from the Latin names in the game's assets (110 species by genus and
      species, or 117 names with the subspecies and aberrations; C3), read at build time, or a list written by hand; how it lands
      in the definition (a dimension the `variants` source cannot give: a value per type from a table); its order and names; whether
      it is a layout with tabs or groups on pages by count. Also whether a second layout (by rarity, `chanceByType`, C3) is worth it.
      Branch: `docs/butterflies-grouping`. After: V4 merged. Model: opus. Author: decides.
      Done when: the grouping and how the definition carries it in Decisions; a change of the shape, if any, in `docs/decisions.md`.
- [x] **L4b. Butterflies: the grouping built.** By L4a; then the author's notes from the game, as L2. The album recipe kept or changed.
      Branch: `feat/butterflies-look`. After: L4a. Model: sonnet. Author: plays, writes the notes.
      Done when: tests pass (every one of the 169 in exactly one group), mutation-checked; the notes fixed or rejected; pull request.
      Split in two sessions (the author, 11.11.2026), one branch and one pull request:
  - [x] **L4ba. The derived dimension in the core.** `DimensionDef` `From` and `Map`, the reader, `CheckDefinition` (a source
        value in two entries refused), a value the `map` lacks (its own, after the listed, a warning), no `drop` needed when the
        source is kept, never a part of a member's name; core and reader tests, mutation-checked. Nothing the player sees.
        Model: opus. After: L4a merged. Done when: core and adapter tests pass, mutation-checked.
  - [x] **L4bb. The butterflies grouped.** The tables generated by a script from the game's assets into `butterflies.json`
        (`species`, `family`, `rarity`; layouts `byfamily`, `byrarity`); lang en / uk (species, families, steps, layouts); the icons;
        a server test against the game's assets (every one of the 169 in exactly one group, the Latin names, the steps of chance);
        a frame's heading inside a tab; `functional.md`, `CHANGELOG.md`, the upgrade table; the author's notes from the game; the
        pull request. Model: sonnet. After: L4ba. Done when: L4b's "Done when".
- [x] **L6. The album window's width and rows.** The window's width follows the bottom rows of controls, not the page of cells (seen with
      the butterflies, 09.11.2026): a column of cells more on the page and the controls spread a little, so the window is one clean block.
      Looks at every collection (tapestries, crystals, seashells, butterflies), at both layouts and the tabs of crystals; the author's
      notes from the game as L2. The earlier tab-heading cut-off ("Бісму…", V2) belongs here too. From L2 (the author, 10.11.2026):
      a free grid (crystals `richest`: 12 + 4 cells) spread evenly over the lines the window's width needs (8 + 8), and a page
      narrower than the window centred in it; a page of one group (a tab of `all`) has its dark frame stretched to the controls below
      and the window's right edge, with the margins; the tapestries' frames per picture looked at alike.
      Branch: `feat/album-layout`. After: V4 merged. Model: opus. Author: plays, writes the notes.
      Done when: the notes fixed or rejected with reasons (Decisions); pull request.
- [x] **L9. The album window's rows of controls, a class of their own.** `AlbumDialog` is about 600 lines with several jobs
      (principle 8 in `docs/architecture.md`): move the row of controls (`Controls`, `Arrange`, the pager, the dropdowns' widths)
      into a class of its own (`AlbumControls`, as `AlbumHeader` holds the album's own row), so that L2b's dropdown with icons
      lands there. A refactoring: no change the player sees. Proposed 10.11.2026 after L6, the author agreed.
      Branch: `refactor/album-controls`. After: L6 merged. Model: sonnet. Author: none.
      Done when: all test projects pass; the album window looks and works as before (A27 and A22 in `functional.md`, the agent
      writes the check, the author runs it); pull request.
- [x] **L5. The album recipes final.** One pass over the four album recipes as L2–L4b left them: the same pattern, each named by its
      definition, all in the handbook. F3 of `2026-11-04-fixes-0.2.md` waits for this.
      Branch: `feat/album-recipes` (none if nothing changes). After: L1–L4b. Model: sonnet. Author: yes or no.
      Done when: the author's yes; the recipe rows of `docs/domain.md` agree.
- [x] **L7. Want list window: a line cycles through its items** (`ROADMAP.md`, 0.2, item 6, asked 09.11.2026). A slot of a line any
      of several items fills (a shell of any colour, a crystal of any rock) shows them in turn, as the album's cells do (`Slideshow`,
      once a second, paused while hovered).
      Branch: `feat/wantlist-cycle`. After: V4 merged. Model: sonnet. Author: the in-game check.
      Done when: tests pass, mutation-checked; a case in `functional.md` passed; pull request.
- [x] **L8. The guide and the album's handbook text name four collections** (`ROADMAP.md`, 0.2, item 6, seen 09.11.2026): `guide-text`
      and `handbook-use-album-text` (en, uk) say there are four collections, not "the first is tapestries", and describe the album
      without "pictures"; `handbook-use-ledger-text` and `handbook-use-materials-text` (en, uk) make an album from any collection's
      member, not "with any tapestry piece" (found in L5, the author, 11.11.2026). Branch: `docs/album-texts`. After: V4 merged. Model: sonnet. Author: the text (reviewed with the lang at release).
      Done when: both languages; `AssetsTests` pass; the upgrade table's lang keys; pull request.
- [x] **L10. Textures of other elements drawn in place of the game's tooltip** (seen 11.11.2026 in W7; the release waits for it, the
      author). With `glDebugMode` on, the client crashes on `GL_INVALID_VALUE` in `LoadOrUpdateCairoTexture`: the item tooltip in the
      album (09.11.2026, A4 of the album view plan), the level dropdown with quick clicks (10.11.2026, L6), the item tooltip right
      after the album opened (11.11.2026, 11:47). With it off (the dev client since 11.11.2026), no crash but garbage: the tooltip's
      title drawn apart from its box, the box showing pieces of another window or the chat's text, and `OpenGL threw an error:
      InvalidOperation` in `client-main.log`, in a want list window of tapestries opened seconds after the album (11:52). What a
      player would see. The likely kind: a texture id deleted while another element still owns it, given by GL to a smaller
      texture, so that element updates or draws the wrong one. The one raw delete of ours: `AlbumPageElement.Dispose` deletes
      `slotTexture` and `cornerTexture` and does not zero them (a second `Dispose` deletes another's texture); no path to a second
      call found yet. Not from L7 (the hovered slot holds still; the tapestries' slots never change). Also to rule out: the game's
      own race in `GuiElementItemstackInfo.AsyncRecompose` (composed on a worker thread). The way: zero the ids; log every delete
      and dispose of our textures for a while; write the exact steps for the author (open the album, close it, open a want list,
      hover its slots; the album's dropdowns quickly); find the cause, fix it, test what can be tested.
      Branch: `fix/gui-textures`. After: none. Model: opus. Author: reproduces in game by the agent's steps.
      Done when: the cause found and fixed, or ruled ours-free with the evidence; the author's steps pass with no `OpenGL` error in
      the log; a case in `functional.md`; pull request.
- [x] **L11. Tapestries: the look.** The step of the tapestries as L2–L4b were of the others (none had been planned; L6 left them
      "unchanged; the author looks"). The author's idea (11.11.2026): a picture's dark frame stretches so that the page has no light
      patches: frames on one line of pictures as tall as the tallest, the line's frames widened to the page's width (as L6 stretches
      a group alone on its page), perhaps the last line down to the controls; and the picture centred in its frame, tried and kept
      only if it looks better. Then the author's notes from the game, as L2. The rule lives in `AlbumPages` (the box of a group
      apart from where its cells stand), so the butterflies' frames per species (`byfamily`) follow it too: looked at alike.
      Branch: `feat/album-frames`. After: none. Model: opus. Author: plays, writes the notes, decides on centring.
      Done when: the notes fixed or rejected with reasons (Decisions); `AlbumPagesTests` for the stretched frames, mutation-checked;
      a case in `functional.md` passed; pull request.
- [x] **L12. A tapestry collection named by its source, not its id** (`ROADMAP.md`, 0.2, item 6, the author, 10.11.2026: a
      collection's look by its asset alone, for other mods' authors). The game's names of tapestry pictures are looked up only for
      the collection with the id `tapestries` (`AlbumDialog.ValueName`), so another collection of the `tapestry` source shows raw
      picture codes. Tie it to the source kind (`CollectionSource.Tapestry`), wherever the picture's name is looked up
      (`MemberNames` too, if it has the same special case).
      Branch: `fix/tapestry-names`. After: none. Model: sonnet. Author: none.
      Done when: a test with a second collection of the `tapestry` source under another id names its pictures by the game,
      mutation-checked; no code compares a collection's id with `tapestries` for its look; pull request.
- [x] **L13. Want list window: a grid with no empty slots** (`ROADMAP.md`, 0.2, item 6, seen 10.11.2026 in L6). The window's grid
      is the game's, always 10 columns with the last row filled up by empty slots, while the album's cells spread evenly over
      their lines with none empty. First the author decides: the want list follows the album (lines spread evenly as `AlbumPages`
      does, no empty slots) or keeps the look of the game's chests (then the step closes with the reason in Decisions). Then built.
      Branch: `feat/wantlist-grid`. After: none. Model: sonnet. Author: the decision; the in-game check.
      Done when: the decision in Decisions; if built, a case in `functional.md` passed (W7 still passes: the lines cycle); pull request.
- [ ] **L14. A part for modders** (`ROADMAP.md`, 0.2, item 6, the author, 10.11.2026): how another mod adds a collection. The
      definition's JSON (today only in `docs/decisions.md`, Collections), that the definition and its patches must reach the client
      (not `"side": "server"`), icons (`icon`, `icons`, an SVG drawn in its own colours), the album recipe; `tapestries.json` and
      the test mod as examples. Where it lives follows G1 of `2026-11-04-mod-description.md` (README becomes the project's README:
      the part goes there, English only, unless G1 decides otherwise); `docs/decisions.md` keeps the why and points to it.
      Branch: `docs/modders`. After: G1, L12 (the part promises a look by the asset alone). Model: sonnet. Author: the text
      (public: reviewed before the pull request).
      Done when: the part in place, every field it names checked against `CollectionDefinitionReader`; the author's yes; pull request.

## Decisions

- **Tapestry pieces told apart by the same rule as every member** (the author, 09.11.2026): "the picture, the place in it" is a
  name from values like the others; one step (L1), not a step for tapestries and another for the rest.
- **L1 after the seashells, not at once** (the author, 09.11.2026): the rule is right only with crystals and seashells in view.
  Rejected: L1 for tapestries now, the rest later (the rule made twice).
- **The look steps are driven by the author's notes from the game** (the author, 09.11.2026), not guessed in advance.
- **L11–L14 added to this plan after it was done** (the author, 11.11.2026): the tapestries had no look step of their own, and
  three lines of `ROADMAP.md` 0.2 item 6 had no step (a tapestry collection by its source, the want list's grid, a part for
  modders). The same item of the roadmap, so this plan, not a new one.
- **No code review of its own** (the author, 09.11.2026): R1 of the release plan reads it, as item 4 and the album window.
- **A branch and a pull request per step** (the author, 09.11.2026).
- **Step letter L** (look); A, B, C, F, G, K, P, R, S, T and V are taken.
- **Members named by their values (L1, the author, 10.11.2026).** Where shown: a want list sheet's line (in place of the first
  item's name), the first line of an album cell's tooltip (above "Missing"; the stack's own name stays the title), the want list
  window's slot (in place of the freedom line). Not the album's search (a roadmap line if wanted). The phrasing (en / uk):
  - tapestries: the picture's name from the game, then the place by our own words, not the game's "Section: …" (its words for
    `4x1` say "top quarter", but the game lays `4x1` out in a row, vssurvivalmod BlockTapestry.cs `neighbours4x1`): "Nightfall,
    left third" / "Сутінки, ліва третина"; a `1x1` picture its name alone. Rejected: "Nightfall, 2 of 3" (less clear for `2x2`).
    Pictures the game names alike (`schematic-a`, `-b`, `-d`; `-c`, `-c-bloody`: "Schematic") stay alike (the author): the album
    tells them apart by place;
  - crystals: `richest` "Native copper, any rock" / "Самородна мідь, будь-яка порода" (the grade by `max` is not said; a rock all its
    items share is: "…, basalt"); `rocks` "Native copper, andesite"; `all` "Native copper, bountiful, andesite" / "Самородна мідь,
    щедра, андезит";
  - seashells: `types` "Scallop, any colour" / "Гребінець будь-якого кольору"; `colors` "Scallop, seafoam" / "Гребінець, морська піна";
  - butterflies: the game's name of the item (the type has no names in the definition).
- **The rule and how the definition carries it (L1, the author, 10.11.2026).** The parts: the member's value in each dimension the
  level keeps, in `keep` order, then each other dimension not dropped as `max` in which all its items share a value, in the
  definition's order; only dimensions with a `nameKey` and values that have a translation (looked up with wildcards,
  `Lang.GetMatchingIfExists`, so `game:tapestry-{value}` names a piece by its picture, as BlockTapestry.cs:359). Parts after the
  first start in lower case. Then the freedom (`anyKey`, as today). A level's optional `memberKey` words the whole name for a
  language's grammar: `{0}`, `{1}`… the parts, the freedom written in the text (seashells `types`). No part: the first item's name and
  the freedom (butterflies; a piece a mod added). `inLists` goes (the rock is now in the name). Planned shape, for the session
  that builds it: `DefinedCollection.NameParts(level, member)` in the core replaces `Listed`; `LevelDef.MemberKey`; a third
  tapestry dimension `section` (`3x1-2`, `2x2-21`, `1x1`; dropped `any`; from `TapestryPieces`, a config-added piece `1x1`) named
  by our keys `curiocatalogue:tapestries-section-{value}` (18, none for `1x1`); `nameKey` on the tapestries' `piece` (not on
  `picture`: a named first `groupBy` makes tabs, AlbumPages.cs:134); `crystals.json` `all` keeps `["ore", "grade", "rock"]` (the
  member id follows the definition's dimension order, not `keep`, DefinedCollection.cs:211, so stored ids do not change); one
  adapter helper for a value's name used by the window too (`AlbumDialog` widths, tabs, `ValueName`, which today special-cases
  the tapestry picture).
- **Three points of L1 settled before the code (the author, 10.11.2026).** (1) The picture's group heading keeps its own case
  (the picture has no `nameKey`, else tabs), looked up through the same helper as `game:tapestry-{value}`. (2) A member with no
  part (butterflies) has no name line in the album cell's tooltip: the stack's title already is that name; only the freedom, as
  before. The want list's line names it by the item as before. (3) Chat messages are not changed in L1 (not in "Where shown");
  a roadmap line if wanted.
- **The section's words (L1a, 10.11.2026; drafts, reviewed with the lang at release):** halves and thirds by side ("left third",
  "top half"), a 2x2 piece as "top left quarter", a 4x1 piece as "first quarter from the left" (the game's own words for `4x1`
  are wrong, above). Until L1b, the want list's line says the parts its level does not keep, as `inLists` did (`ItemInvoice.Listed`).
- **Small points of L1b, 10.11.2026** (agent; the author may overturn at the review): one helper `Adapters/MemberNames` (a value's
  name, a member's name, what it leaves free); a level's `memberKey` is used only when every part has a translation, else the plain
  form (its `{0}`, `{1}` would not fit); a collection switched off shows no name line in the cell, as it showed no freedom; the album
  cell's freedom line stays only for a member with no name (butterflies, a piece of a mod), the name already says it.
- **The crystals' look, the author's notes (L2, the author, 10.11.2026):**
  - `rocks`: the table keeps the definition's order. Tried in game: a table's `"sort": "fullest"` (the fullest column left, the
    fullest row on top), on `rocks` only. Rejected (the author): it does not look better, and to match it the tables of `all`
    would have to be sorted per page too, so the columns would jump from page to page. Rejected before it: the value lists
    reordered by hand (they order the tabs and the other levels too).
  - `richest`: 12 + 4 cells → evenly, 8 + 8, the page centred in the window (the author); to L6, the same code as the window's width.
  - `all`: a tab's page frame stretched to the controls and the window's right edge; to L6 (the tapestries' frames alike).
  - Icons for words (ores as nuggets, the grade as a round mark of four sizes, the level and layout choices): a step of its own,
    L2b; the dropdown with icons is a new element, worth the effort for what else it opens (the author).
  - The album recipe kept as it is (the author). The only reason to change it would be choosing the level at crafting: overengineering.
- **The album window's width and rows (L6, the author agreed 10.11.2026 to the agent's proposal):**
  - The page's width is the nearest whole number of cells to the base width (652 → 13 cells, 663: 12 left a strip of 40), and
    when the rows of controls or the album's own row are wider, the level is laid out again with their width, in whole cells
    (`AlbumDialog.Arrange`, only ever wider). The page element is as wide as the window.
  - Members in order spread evenly over the lines the width needs (`AlbumPages`): 16 at 13 a line is 8 + 8. A grid stays a
    grid: 25 is 9 + 9 + 7, only the last line shorter.
  - A page with no frames (members in order, a table with no grouping) narrower than the window stands centred, by the widest
    page of the level (`AlbumPages.UsedWidth`), so it does not move from page to page. Pages with frames stay at the left:
    centred, the frames would jump from tab to tab.
  - A group alone on its page has its frame over the whole page element: to the window's right edge and down to the controls,
    with the same margins. Its table stays at the top left. The tapestries (several pictures a page) unchanged; the author looks.
  - The column headings in words cut to a cell ("Бісму…", V2; the seashells' colours "Звича…" alike): not fixed here; L2b
    gives ores (and may give colours) icons, which ends it.
  - In game (the author, 10.11.2026, uk): `rocks` fills the window; `richest` 8 + 8 centred; a tab of `all` with its frame to the
    edges; seashells centred; the author likes the crystals better now, the gaps of the tables no longer bother. Seen and fixed:
    a long level name ran under the dropdown's arrow (measured in the window's font, drawn in vanilla's larger `WhiteSmallText`).
  - A crash on opening the level dropdown (seashells, `GL_INVALID_VALUE` in `LoadOrUpdateCairoTexture`, fatal with
    `glDebugMode`; the same kind as A4's in the item tooltip, 09.11.2026) with quick clicks; not reproduced with slow ones.
    Cause not found; not put down to L6 (the window there was small, the layout unchanged in height).
- **Icons for words (L2b, the author, 10.11.2026, on the agent's proposal):**
  - A dimension's `icon` stays a template with `{value}`; `icons` (value → icon) gives the values the template does not fit, and
    may stand alone, a whole table by hand (the butterflies' species after L4a). Crystals: `game:nugget-{value}`, the three ores
    in a host (`quartz_nativegold`, `quartz_nativesilver`, `galena_nativesilver`) by the table, to the game's `nativegold` and
    `nativesilver` nuggets.
  - The two silvers share one nugget: the cells at the crossing are what is collected, the heading only points (the author).
    Rejected: the graded ore chunk (`ore-medium-{ore}-granite`, the game shapes the three hosts apart) out of style with nuggets;
    a nugget with the host in a corner, a composed kind of icon.
  - An icon is an item or block code, or an SVG of ours by its path ending in `.svg`: the grade is four round marks
    (`curiocatalogue:textures/icons/grade-{value}.svg`), drawn by us in the heading. Data, so other collections may use marks too
    (a seashell colour as a round swatch).
  - An SVG is drawn in its own colours, not tinted by code: a mod sets the look by its files alone (the author: what we can change
    in code, other mods' authors cannot). The grades yellow, the small mark dim, the large one full (the author, after A28's
    first look in game, 10.11.2026: the size right, white too plain). The ores' short names (`shortNameKey`) unused now, kept.
  - The richer grade above the poorer in the tables of `all` (the author, 10.11.2026): a table's `"reverseRows": true` in the
    definition, not the grades' value list reversed (that order is "lowest first" for `max`). Taking new in that layout follows
    the rows, richest first, as the page shows them.
  - The other collections looked at: seashells fit (a type as `seashell-{value}-plain`, a colour as a shell or a swatch); butterflies
    fit with a table by hand; tapestries do not fit and need none (values known only from the game, pieces told by attributes,
    one level and one layout). An icon "a sample of the group's first member" would serve them: not now.
  - The dropdown with icons and the icons of levels and layouts: a step of their own, L2c (the author: one session is not enough).
- **Two more steps** (the author, 10.11.2026): L7, L8; both were `ROADMAP.md` lines of item 6 with no step.
- **The icons of levels and layouts (L2c, the author, 10.11.2026):** a sample from the game, not a scheme of the page drawn by us
  (rejected: SVG schemes, a row / a table / a stack of tables; a mix, schemes for crystals and samples for seashells). Crystals:
  `richest` a bountiful native copper crystal (`crystalizedore-bountiful-nativecopper-basalt`: basalt, the only rock the game
  has it in), `rocks` granite (`stone-granite`), `all` a medium one (`crystalizedore-medium-nativecopper-andesite`); the layouts
  as their tabs, `byore` `nugget-nativecopper`, `byrock` `stone-granite`. Seashells: `types` `seashell-scallop-plain`, `colors`
  `seashell-scallop-turquoise`. Tapestries and butterflies have one level and one layout: no choice, no icon.
- **Small points of L2cb, 10.11.2026** (agent; the author may overturn at the in-game check):
  - The open list stands under the box when it fits whole (as vanilla's), else on the side with more room, over the page,
    scrolled (`Gui/DropDownList`, tested); at most 12 lines. Scrolled by the wheel and the keys; the thin bar at its right only
    shows where the lines are, it is not dragged.
  - The closed box shows the icon alone only when the row with the names is wider than the page, and only for a dropdown whose
    every choice has an icon ("Choose a level" has none, so the level dropdown keeps its names); its name then on hover.
  - The author, after A30's first look (10.11.2026, screenshots, en): an empty album whose level is chosen shows the level
    dropdown closed as its icon alone, whatever the room (the name on hover and in the open list); before a level is chosen,
    "Choose a level" in words. The layout dropdown closed is its icon alone too, always. Icon and name together in the open
    lists look plain but stay: they say more.
  - Escape with a list open closes the list and keeps the window; the next Escape closes the window.
  - The row of controls is added to the window before the page and the page buttons (a click goes to the first element that
    takes it, `docs/modding.md`, GUI); the page shows no cell tooltip under an open list.
  - The side tabs keep vanilla's element: the icon's room is half leading spaces, half more padding (vanilla sizes a tab by its
    name's ink, which leading spaces are not), the icon drawn in it; the tabs' bounds 260 wide, not 200, for the longest ore name.
- **The rarity corner of seashells (L3, the author, 10.11.2026):**
  - 0.2 shows it: seafoam and turquoise cells (14 at `colors`; none at `types`, a type of any colour has no rarity) get A2's
    corner and a tooltip line. Rejected: leaving it all to `ROADMAP.md`, Maybe, "Rarity in the album".
  - How the definition carries it: `marks` in each collection, attribute → value → `color` and `nameKey`, the first that fits a
    member shown; any attribute, so another mod marks its own by its file alone. Rejected: one table of tiers for the whole mod
    (rare, epic…) that collections name (alike everywhere, but another mod could not have its own colours without a patch).
  - Gold (`#E0B040`), "Rare colour" / "Рідкісний колір" (says what is rare). Rejected: blue "Rare" (the game tradition, but
    lost on the blue-green shells), violet (reads as "epic" in games).
  - Small points (agent; the author may overturn at the in-game check): the colour as `#RRGGBB` or `#AARRGGBB`; the tooltip line
    under "In the collection" / "Missing", in the mark's colour; no line for a mark without `nameKey`; a collection switched off
    shows no mark line, as no name line (L1b); `AlbumPageLook.Rarity` renamed `Mark`.
  - In game (the author, 11.11.2026): works; the corner moved to the bottom right (A2 of the album view plan had the top right).
  - The seashells' look otherwise: no notes, the crystals' fixes (L6, L2b, L2c) covered it. The album recipe kept as it is.
- **The butterflies' grouping (L4a, the author, 11.11.2026, on the agent's proposal).** Facts (game 1.22.7, `game/lang/en.json`
  `itemdesc-creature-butterfly-{type}`; `entities/animal/invertebrate/butterfly.json`, `chanceByType`): the 169 types make
  **109 species** by genus and species (C3 counted 110 with the game's capitals; subspecies and aberrations go with their species:
  *Polygonia c-album ab. dilutus* with *Polygonia c-album*); 60 species of one type, 39 of two, 9 of three, one of four
  (*Ornithoptera priamus*). The game's Latin has stray spaces and capitals (*Parnassius Mnemosyne*, *Mesosemia Pacifica*).
  21 species have types in different steps of spawn chance (a common male, a rare female).
  - **Layout `byfamily`, the default: tabs by family, a frame per species** (as a tapestry's picture). Families by hand (the game
    has none), in the usual order of butterflies, moths last: Papilionidae (24 types, 14 species), Hesperiidae (1, 1), Pieridae
    (52, 24), Riodinidae (3, 2), Lycaenidae (16, 11), Nymphalidae (62, 47), moths (11, 10: Saturniidae, Erebidae, Sphingidae,
    Uraniidae, Geometridae, Noctuidae as one). Species in a tab by Latin name; types in a species in the game's order (by code).
    Rejected: 109 frames with no tabs (about ten pages, the pager alone); 74 tabs by genus.
  - **A species' heading is its Latin binomial**, our own keys (`curiocatalogue:butterflies-species-{value}`), the same text in
    `en` and `uk`, the game's typos fixed. Rejected: the common names (109 to write in both languages; the game names types, not
    species), both.
  - **Layout `byrarity`: tabs by the six steps of `chanceByType`, rarest first** (6, 7, 19, 38, 44, 55 types), the types in a tab
    in the order of `byfamily`; no frames of species (21 span steps). Named by words, drafts reviewed with the lang at release:
    Legendary, Very rare, Rare, Scarce, Uncommon, Common. Rejected: the corners of L3 for the rarest (no layout); nothing in 0.2;
    the chance in numbers ("1 in 1000": the climate decides more, it reads as a promise); stars. C3 called rarity "a property of a
    cell, not a dimension": here it is a dimension for a layout only; the level keeps `type`, a member's rarity is still its type's.
  - **No second level by species** (109, any form) in 0.2: a line in `ROADMAP.md`, Maybe.
  - **How the definition carries it: a derived dimension.** `{"code": "species", "from": "type", "values": [...], "map": {"danaus-plexippus":
    ["monarch"], ...}}`: a value per value of an earlier dimension by a table (value → its source values), so the `variants` source
    gives `type` and the definition the rest; `family` from `species`, `rarity` from `type`. The core refuses a source value in two
    entries of one `map` (`CheckDefinition`). The level `all` keeps `["type"]`, so member ids and stored albums do not change; a level
    that keeps a derived dimension's source needs no `drop` for it. **A derived dimension is never a part of a member's name**
    (its value follows from a kept one; L1's rule would else name a butterfly by its species): a butterfly keeps the game's name.
    **A source value the `map` lacks** (another mod's butterfly; `reedbutterfly`, if the game adds its item) is its own value, after
    the listed ones, with one warning: the rule of a value a list lacks. The tables are generated once from the game's assets by a
    script and written into `butterflies.json`; a server test checks them against the game (every type's Latin name begins with its
    species', its step of chance is its tier), so an update of the game that moves one is caught. Not read at run time (C3: a
    translation may change the lang). Rejected: `rules` setting an attribute per type (169 rules, and the window groups by
    dimensions only); a source kind of its own in C# (a collection is data, `docs/decisions.md`).
  - Small points (agent; the author may overturn at L4b's look): tab icons a sample butterfly of the family or the step; the
    layouts' icons a sample too (L2c had none: one layout then); a frame's heading inside a tab names the species alone, not
    "family · species"; family names in words, drafts: Swallowtails, Skippers, Whites and sulphurs, Metalmarks, Gossamer-winged
    butterflies, Brush-footed butterflies, Moths (uk drafts in L4b, reviewed with the lang at release).
- **Small points of L4ba, 11.11.2026** (agent; the author may overturn at the review): `from` names an earlier dimension (else
  refused, so a chain cannot loop) and needs a `map`, a `map` needs a `from`; a value the source itself gives in a derived
  dimension is replaced by the derived one; a level needs no `drop` for a derived dimension whose source it keeps or whose source
  follows so (a family from a species from a kept type); a source value the map lacks gives one warning per dimension, which
  replaces the list's "does not list" warning for it (a family from such a species warns once more, in its own dimension); a
  derived dimension is no part of a member's name even when a level keeps it.
- **Small points of L4bb, 11.11.2026** (agent; the author may overturn at the look): the tables come from `tools/butterflies.ps1`
  (types and steps from `chanceByType`, the Latin from `itemdesc-creature-butterfly-{type}`, the families by genus written in the
  script); the types keep the game's order; a species' code is `genus-species` in lower case (`polygonia-c-album`), the species
  sorted by it; the rarity values `legendary`, `veryrare`, `rare`, `scarce`, `uncommon`, `common`. A frame inside a tab leaves out
  the tab's dimension only when the layout groups by more than one (`AlbumPages.HeadingFrom`): the rarity tabs' single frame keeps
  its step's name. Sample icons: Papilio machaon for the swallowtails and the layout by family, Queen Victoria's birdwing for the
  legendary step and the layout by rarity, a monarch for the common. Uk drafts of the families by the Ukrainian names of the
  families (Косатцеві, Товстоголовки, Біланові, Ріодиніди, Синявцеві, Сонцевики, Нічні метелики), of the steps Легендарні,
  Дуже рідкісні, Рідкісні, Нечасті, Незвичайні, Звичайні; reviewed with the lang at release.
- **The album recipes, 11.11.2026** (L5, the author): the four share one pattern (shapeless 2 × 1, an empty ledger or bill of
  materials and a member as a tool that stays, group 4, the cover and sheets carried over). The tapestries' recipe, `ledger-album`
  since 0.1, is renamed `ledger-album-tapestries`, named by its collection like the other three; no recipe name is stored, so
  albums made before are untouched. The ingredients' letters (`T`, `C`) are left as they are: the handbook does not show them.
- **The frames stretched, 11.11.2026** (L11, the author's idea; checked in game by the author):
  - The rule lives in the core: a group's frame (`GroupBox.Frame`) apart from where its heading line and cells stand. On a shelf
    the frames are as tall as the tallest; the room left on the shelf's right is shared equally (the remainder to the last), so
    the last frame ends at the page's width and the frames after the first move right by the shares before them; the last
    shelf's frames reach down to the tallest page of the level (`AlbumPages.Height`). L6's group alone on its page is now a case
    of this rule. The page element draws a frame that reaches the page's right edge or bottom on to its own edge (a strip short
    of a cell, side tabs taller than the page). A page with no headings has no frames to stretch.
  - The group's name stands at the frame's top left, and has the frame's width for its room (cut with "…" less often).
  - Centred in the frame: a picture (`Arrangement.Shape`, the tapestries), by the arrangement and not the collection's id (L12);
    members in order and tables (butterflies' species, crystals) stay at the frame's top left under the name: the author looked
    at both, the butterflies "look fine now". A picture is centred by its cells, not its box: a picture narrower than its name
    has a box as wide as the name (`MinGroupWidth`), and centring the box left it at the left (seen in game, Translocator).
- **The look of a tapestry collection by its source** (L12, 11.11.2026): `MemberNames.ValueName(source, dimension, value)` names a `picture` of a `CollectionSource.Tapestry` source by the game (`game:tapestry-{value}`); `CollectionRegistry.SourceOf(id)` gives the source to the window. No code compares a collection's id with `tapestries` for its look (the id stays in the config's defaults and the old-config move only). Rejected: a flag in the definition JSON (the source kind already says it, and a mod author would have to know to set it).
- **The want list window follows the album** (L13, the author, 11.11.2026): the invoice window (`InvoiceDialog`: a want list and the invoices of the ledger and the bill of materials, one window) spreads its lines evenly over rows as `AlbumPages` does and has a slot for each line, none empty. The rule is `EvenLines.PerLine` in the core, used by both; `GridDialog.FillView(evenly: true)`. The window is never narrower than 6 slots (its texts and the Cancel button need the room). The ledger window keeps the game's chest look (10 columns, the last row filled up). Rejected: keeping the chest look for the want list too (the only window of ours that looked unlike the album, seen in L6).

## Where we stopped

- 10.11.2026: L1 decided (final, Decisions: "Members named by their values", "The rule and how the definition carries it"), branch
  `feat/member-names` from `main`, no code yet. Next: L1's code by the planned shape (core and tests, the adapters and the window,
  lang en / uk, the server tests' want list lines in `CrystalsTests`), then `functional.md`, the upgrade table, `CHANGELOG.md`,
  the in-game check (en, uk) for the author, the pull request.
- 10.11.2026: L2's notes all decided (Decisions, "The crystals' look"): the sort tried and dropped, no code change in L2; the
  rest went to L6 and L2b. Branch `feat/crystals-look` from `main` before L1 is merged: the plan's Decisions and this section
  will meet L1's in the merge. L2 done; its pull request carries only the plan and `ROADMAP.md`.
- 10.11.2026: L6 done on `feat/album-layout` (final, checked in game by the author): Decisions, "The album window's width and
  rows"; `AlbumPagesTests` (even lines, mutation-checked; the used width), all test projects green; case A27 in
  `functional.md`, `CHANGELOG.md`. A27 passed (uk: crystals, seashells; then en, butterflies, tapestries, the dropdown fix).
  Next: the pull request; then L9 before L2b.
- 10.11.2026: L9 done on `refactor/album-controls` (final code; the in-game check is the author's): `Gui/AlbumControls` holds the
  row of controls (`Build`: the parts, the width, the left and right placing; the dropdowns' widths) and the page buttons; the dialog
  says what the row holds now (`Content()`) and keeps the actions. `AlbumDialog` 600 → about 530 lines. All test projects green
  (no test of its own: a client-only GUI refactoring, no mocks of the dialog). Check: A27 and A22, nothing changed to the eye.
- 10.11.2026: L2b's code done on `feat/crystals-icons` (final unless the game says otherwise): `DimensionDef.Icons` and `IsSvg`,
  the reader's `icons`, the page element draws SVG headings with the name on hover, `crystals.json` (nuggets, the table, four
  `grade-*.svg` marks); tests mutation-checked (core, reader, every icon in the game on the server); case A28 in `functional.md`,
  `CHANGELOG.md`, `docs/decisions.md`, `docs/domain.md`. Then, by A28 in game (the author): the marks yellow, dim to full, in the
  SVG's own colours (drawn through Cairo: `LoadSvg` swaps red and blue, `docs/modding.md`); the richest grade on top
  (`reverseRows`). A28 passed; L2b done (final). Next: the pull request, then L2c.
- 10.11.2026: L1a done (final) on `feat/member-names`, not pushed. Next: L1b (see the step). Note for L1b: in the window,
  `AlbumDialog.Metrics` measures every dimension's value names with `Lang.Get`, now also the pieces and sections: through the helper.
- 10.11.2026: L1b code done (final) on `feat/member-names`: `MemberNames`, the want list's line and slot, the cell's tooltip, the
  window's value names; server tests (`TooltipTests`, `CrystalsTests`, `SeashellsTests`, `ButterfliesTests`), mutation-checked;
  `functional.md` A29 (new; A27 when written, renumbered after L6 and L2b), A24, W5; the upgrade table; `CHANGELOG.md`.
- 10.11.2026: `feat/member-names` rebased on `main` after L6, L9 and L2b (`inLists` gone next to L2b's `icons`; the window's
  widths by `HasIcons` and `ValueName`); all test projects green. A29 passed in game (en, uk; the author). L1 and L1b done (final).
  Next: the pull request, then L2c.
- 10.11.2026: L2c split in two (the author); L2ca done (final) on `feat/icon-dropdown`, not pushed: the icons as data (Decisions,
  "The icons of levels and layouts"). Next: L2cb, the element. Vanilla's dropdown cannot draw an item in a line (L2cb), so the
  element is ours: closed, the icon and the name (the icon alone when short of room); open, lines of icon and name over the page,
  scrolled; focus, Escape and the wheel as vanilla's (GuiElementDropDown.cs, GuiElementListMenu.cs).
- 10.11.2026: L2cb code done on `feat/icon-dropdown` (final unless the game says otherwise), not committed: `Gui/IconDropDown`
  (ours, icons in the closed box and the open list), `Gui/DropDownList` (where the list stands, scrolling; `DropDownListTests`,
  mutation-checked), `Gui/SvgIcons` (the page's SVG texture shared), `AlbumControls` (choices with icons, the compact row),
  `AlbumTabs` (an icon before the name), `AlbumDialog` (icons of levels, layouts, tabs; Escape; the row added before the page);
  Decisions, "Small points of L2cb"; case A30 (new) and A27 in `functional.md`, `CHANGELOG.md`, `docs/domain.md`,
  `docs/modding.md` (GUI). Next: A30 and A27 in game (the author, `en` and `uk`), then the pull request; L2cb and L2c ticked then.
- 10.11.2026: by A30's first look (the author): the level dropdown of an empty album whose level is chosen, and the layout
  dropdown always, show the icon alone (Decisions, "Small points of L2cb"). A30 passed in game (the author). L2cb and L2c done
  (final). Next: the pull request.
- 10.11.2026: L3's corner done on `feat/seashells-look` (final unless the game says otherwise; Decisions, "The rarity corner of
  seashells"): `MarkDef` and `CollectionDefinition.MarkOf` in the core, `marks` in the reader, the window's `Mark`, `seashells.json`,
  lang en / uk; tests mutation-checked (`DefinedCollectionTests`, `CollectionRegistryTests`, `SeashellsTests`); case A31 in
  `functional.md`, the upgrade table, `CHANGELOG.md`, `docs/domain.md`, `docs/decisions.md`. Next: the author plays the seashell
  album (A31, both levels, `en` and `uk`) and writes the notes, as L2; the album recipe kept or changed; then the pull request.
- 11.11.2026: A31 passed in game (the author), the corner moved to the bottom right; no other notes, the recipe kept. L3 done
  (final). Next: the pull request.
- 11.11.2026: L4a done (final) on `docs/butterflies-grouping`: Decisions, "The butterflies' grouping"; `docs/decisions.md`
  (Collections: the derived dimension); `ROADMAP.md`, Maybe (a level by species). Next: the pull request, then L4b.
- 11.11.2026: L4ba done (final) on `feat/butterflies-look`, not pushed: `DimensionDef.From` and `Map`, the derivation in
  `DefinedCollection.Build`, `CheckDefinition` (Decisions, "Small points of L4ba"), the reader's `from` and `map`; core and
  adapter tests mutation-checked, all test projects green. Nothing the player sees. Next: L4bb on the same branch.
- 11.11.2026: L4bb built on `feat/butterflies-look`, not pushed (Decisions, "Small points of L4bb"): `tools/butterflies.ps1`,
  `butterflies.json` (`species`, `family`, `rarity`; layouts `byfamily`, `byrarity`), 124 lang keys in en / uk (drafts),
  `AlbumPages.HeadingFrom` (a frame inside a tab), `ButterfliesTests` against the game's assets and `AlbumPagesTests`,
  mutation-checked; case A32, `CHANGELOG.md`, the upgrade table, both READMEs, `docs/development.md` (Tools). Next: the author
  plays A32 (`en`, then `uk`) and writes the notes, as L2; the album recipe kept or changed; then the pull request.
- 11.11.2026: A32 passed in game (the author): the look liked, no notes; the album recipe kept as it is. L4bb and L4b done
  (final, the uk drafts reviewed with the lang at release). Next: the pull request of `feat/butterflies-look`.
- 11.11.2026: L5 done (final) on `feat/album-recipes`: the tapestries' album recipe renamed `ledger-album-tapestries` (Decisions,
  "The album recipes"); the definition, tests, `GoldenCapture`, `docs/domain.md`, `docs/decisions.md`, the upgrade table,
  `CHANGELOG.md`; L8 also covers the ledger's and the bill's handbook texts. Checked in game at the release by U9 of the upgrade
  plan (the album still crafts) and the handbook's album page. Next: the pull request.
- 11.11.2026: L7's code done on `feat/wantlist-cycle` (final unless the game says otherwise): `ItemInvoice.SlotStacks` (a want
  list line's items, its own first), `InvoiceDialog` cycles them by `Slideshow` (held while `CurrentHoveredSlot` is the slot),
  `GridDialog.ViewSlot`; `CrystalsTests` mutation-checked, all test projects green; case W7 in `functional.md`, `CHANGELOG.md`.
  W7 passed in game (the author: seashells, crystals, tapestries). L7 done (final). Next: the pull request. During W7 the GL
  texture trouble came back (L10, a new step; `glDebugMode` set off in the dev client's `clientsettings.json`, so that it shows
  instead of crashing).
- 11.11.2026: L10's cause found and fixed on `fix/gui-textures` (final unless the game says otherwise): the game disposes a GUI
  element more than once, the second time later (`GuiComposerManager.Create` disposes the last composer of a name again on
  the next one; decompiled), and `AlbumPageElement` freed its slot and corner textures by bare id, so the next album opening or
  page freed a texture GL had meanwhile given to a tooltip or a scrollbar. Both are `LoadedTexture` now (freed once), the textures
  of a recompose freed first, a debug line per dispose; `GuiTexturesTests` refuses a delete by id (mutation-checked);
  `docs/modding.md` (GUI), case A33 in `functional.md`. No `CHANGELOG.md` line: the album page is new in 0.2. The game's
  `AsyncRecompose` is not a race here: its upload runs on the main thread (the crash's stack, `ExecuteMainThreadTasks`), and
  its error is the update of an id whose texture is gone, as in the other two crashes; A33 confirms it. Next: A33 in game (the author,
  `glDebugMode` on), then L10 ticked and the pull request.
- 11.11.2026: A33 passed in game (the author, `glDebugMode` on): no crash, no OpenGL error in the log; 15 album pages disposed
  twice each, 14 of them a third and fourth time later (the last page waits in the game's cache). L10 done (final). Next: the
  pull request.
- 11.11.2026: L8 done (final text for the author's review, reviewed with the lang at release) on `docs/album-texts`: `guide-text`, `handbook-use-album-text`, `handbook-use-ledger-text`, `handbook-use-materials-text` in en and uk (four collections named; an album from a piece of any collection; no "pictures"); the upgrade table; `AssetsTests` green. No `CHANGELOG.md` line: the guide's album text is new in 0.2. Next: the pull request.
- 11.11.2026: L11 done (final) on `feat/album-frames`: Decisions, "The frames stretched"; `AlbumPagesTests` mutation-checked, all test projects green; A34 in `functional.md` passed in game (the author, en and uk), `CHANGELOG.md`. Next: the pull request.
- 11.11.2026: L12 done (final) on `fix/tapestry-names`: the game's names of tapestry pictures follow the source kind, not the id (Decisions, "The look of a tapestry collection by its source"); `TooltipTests` has a server test with a second collection of the `tapestry` source, mutation-checked; all test projects green. No case in `functional.md` (nothing changes for the vanilla collections). Next: the pull request.
- 11.11.2026: L13 done (final) on `feat/wantlist-grid`: Decisions, "The want list window follows the album"; `EvenLinesTests` mutation-checked; all test projects green (Core 210, adapters 240, server 232; the compat tests not run). W8 in `functional.md` passed in game (the author, en and uk, 11.11.2026; 3, 11, 23, 42 lines and two invoices). Next: the pull request.
