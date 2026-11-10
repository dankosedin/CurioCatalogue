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
- [ ] **L2b. Ore crystals: icons for words.** Ores as the game's nuggets (`nugget-{ore}`; gold and silver in quartz or galena by a
      value → icon table, the game has one `nativegold` and one `nativesilver`), the grade as one round mark of four sizes; then the
      level and layout choices with icons: a dropdown of our own that shows an icon beside or instead of the name (a new element, of
      use beyond crystals: the seashells' colours, the tapestries' layouts). How the definition carries an icon per value and what
      the element is: decided at the start of the session, before code. The words stay in tooltips.
      Branch: `feat/crystals-icons`. After: L2 merged. Model: opus. Author: the icons and the element, then plays and checks.
      Done when: the decisions in Decisions; tests pass, mutation-checked; the author's in-game check; pull request.
- [ ] **L3. Seashells: the look and rarity.** Whether 0.2 shows the rarity corner of A2 (a coloured triangle by the attribute the
      definition of V3 gives seafoam and turquoise; `ROADMAP.md`, Maybe, "Rarity in the album", for the full feature); if so, the page
      element draws it and the cell's tooltip names it. Then the author's notes from the game, as L2. The album recipe kept or changed.
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
- [ ] **L6. The album window's width and rows.** The window's width follows the bottom rows of controls, not the page of cells (seen with
      the butterflies, 09.11.2026): a column of cells more on the page and the controls spread a little, so the window is one clean block.
      Looks at every collection (tapestries, crystals, seashells, butterflies), at both layouts and the tabs of crystals; the author's
      notes from the game as L2. The earlier tab-heading cut-off ("Бісму…", V2) belongs here too. From L2 (the author, 10.11.2026):
      a free grid (crystals `richest`: 12 + 4 cells) spread evenly over the lines the window's width needs (8 + 8), and a page
      narrower than the window centred in it; a page of one group (a tab of `all`) has its dark frame stretched to the controls below
      and the window's right edge, with the margins; the tapestries' frames per picture looked at alike.
      Branch: `feat/album-layout`. After: V4 merged. Model: opus. Author: plays, writes the notes.
      Done when: the notes fixed or rejected with reasons (Decisions); pull request.
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
- **The want list's cycling and the album texts get steps** (the author, 10.11.2026): L7, L8; both were `ROADMAP.md` lines of item 6
  with no step.

## Where we stopped

- 10.11.2026: L2's notes all decided (Decisions, "The crystals' look"): the sort tried and dropped, no code change in L2; the
  rest went to L6 and L2b. Branch `feat/crystals-look` from `main` before L1 is merged: the plan's Decisions and this section
  will meet L1's in the merge. L2 done; its pull request carries only the plan and `ROADMAP.md`.
