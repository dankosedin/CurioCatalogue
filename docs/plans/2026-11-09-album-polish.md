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
The look steps (L2, L3, L4b) are driven by the author's notes from the game: the plan names what to look at, not what is wrong.
Each step updates `docs/testing/functional.md`, `CHANGELOG.md` and the upgrade table (`docs/testing/upgrade-0.1-to-0.2.md`,
"Changes in 0.2.0 that need a case": lang keys, recipes) in its own pull request, where they apply; the READMEs where the player
sees a change.

## Steps

- [ ] **L1. Members named by their values.** One rule for naming a member wherever members are listed (a want list's line, the
      title of a cell's tooltip, a chat message that names a piece), from its values in the dimensions it keeps and the freedom
      of those it drops: "Nightfall, 2 of 3" for a tapestry piece (`ROADMAP.md`, 0.2, item 6: pieces of one picture share a name),
      "Native copper, bountiful, andesite" for a crystal, "Scallop, any colour" for a seashell at the type level. Replaces the
      first filler's name plus the freedom (K5 of the core plan left this here). The phrasing in en and uk per collection, by the
      definition (each language's grammar, as `progressKey` and `anyKey`).
      Branch: `feat/member-names`. After: V3 merged. Model: opus. Author: the phrasing for each collection, the in-game check (en, uk).
      Done when: tests pass, mutation-checked; the cases in `functional.md` passed; upgrade table: the lang keys; pull request.
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
- [ ] **L2c. A dropdown with icons.** The level, layout and tab choices with icons: a dropdown of our own (closed: the icon and the
      name, or the icon alone when short of room; open: lines of icon and name, scrolled when long: 17 tabs of crystals, the
      butterflies' species after L4a), and the side tabs with an icon beside the name. The icons of levels and layouts (an `icon`
      on `levels` and `layouts` in the definition, the same forms as a value's) decided at the start, before code; a look at
      every collection as in "Icons for words". The element lands in `AlbumControls` (L9).
      Branch: `feat/icon-dropdown`. After: L2b merged. Model: opus. Author: the icons of levels and layouts, the in-game check.
      Done when: tests pass, mutation-checked; A27 and a case of its own in `functional.md` passed; pull request.
- [ ] **L3. Seashells: the look and rarity.** Whether 0.2 shows the rarity corner of A2 (a coloured triangle by the attribute the
      definition of V3 gives seafoam and turquoise; `ROADMAP.md`, Maybe, "Rarity in the album", for the full feature); if so, the page
      element draws it and the cell's tooltip names it; its colour and name from the definition, not code (other mods' authors
      have only the asset; `ROADMAP.md`, item 6, 10.11.2026). Then the author's notes from the game, as L2. The album recipe kept or changed.
      Branch: `feat/seashells-look`. After: V3 merged. Model: sonnet. Author: decides on the corner, plays, writes the notes.
      Done when: the decision in Decisions; the notes fixed or rejected; pull request.
- [ ] **L4a. Butterflies: the grouping on paper.** By species, from the Latin names in the game's assets (110 species by genus and
      species, or 117 names with the subspecies and aberrations; C3), read at build time, or a list written by hand; how it lands
      in the definition (a dimension the `variants` source cannot give: a value per type from a table); its order and names; whether
      it is a layout with tabs or groups on pages by count. Also whether a second layout (by rarity, `chanceByType`, C3) is worth it.
      Branch: `docs/butterflies-grouping`. After: V4 merged. Model: opus. Author: decides.
      Done when: the grouping and how the definition carries it in Decisions; a change of the shape, if any, in `docs/decisions.md`.
- [ ] **L4b. Butterflies: the grouping built.** By L4a; then the author's notes from the game, as L2. The album recipe kept or changed.
      Branch: `feat/butterflies-look`. After: L4a. Model: sonnet. Author: plays, writes the notes.
      Done when: tests pass (every one of the 169 in exactly one group), mutation-checked; the notes fixed or rejected; pull request.
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
- [ ] **L5. The album recipes final.** One pass over the four album recipes as L2–L4b left them: the same pattern, each named by its
      definition, all in the handbook. F3 of `2026-11-04-fixes-0.2.md` waits for this.
      Branch: `feat/album-recipes` (none if nothing changes). After: L1–L4b. Model: sonnet. Author: yes or no.
      Done when: the author's yes; the recipe rows of `docs/domain.md` agree.
- [ ] **L7. Want list window: a line cycles through its items** (`ROADMAP.md`, 0.2, item 6, asked 09.11.2026). A slot of a line any
      of several items fills (a shell of any colour, a crystal of any rock) shows them in turn, as the album's cells do (`Slideshow`,
      once a second, paused while hovered).
      Branch: `feat/wantlist-cycle`. After: V4 merged. Model: sonnet. Author: the in-game check.
      Done when: tests pass, mutation-checked; a case in `functional.md` passed; pull request.
- [ ] **L8. The guide and the album's handbook text name four collections** (`ROADMAP.md`, 0.2, item 6, seen 09.11.2026): `guide-text`
      and `handbook-use-album-text` (en, uk) say there are four collections, not "the first is tapestries", and describe the album
      without "pictures". Branch: `docs/album-texts`. After: V4 merged. Model: sonnet. Author: the text (reviewed with the lang at release).
      Done when: both languages; `AssetsTests` pass; the upgrade table's lang keys; pull request.

## Decisions

- **Tapestry pieces told apart by the same rule as every member** (the author, 09.11.2026): "the picture, the place in it" is a
  name from values like the others; one step (L1), not a step for tapestries and another for the rest.
- **L1 after the seashells, not at once** (the author, 09.11.2026): the rule is right only with crystals and seashells in view.
  Rejected: L1 for tapestries now, the rest later (the rule made twice).
- **The look steps are driven by the author's notes from the game** (the author, 09.11.2026), not guessed in advance.
- **No code review of its own** (the author, 09.11.2026): R1 of the release plan reads it, as item 4 and the album window.
- **A branch and a pull request per step** (the author, 09.11.2026).
- **Step letter L** (look); A, B, C, F, G, K, P, R, S, T and V are taken.
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

## Where we stopped

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
