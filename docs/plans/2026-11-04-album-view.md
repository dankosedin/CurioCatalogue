# Stamp-album view: the whole collection in the album

Started 04.11.2026. `ROADMAP.md`, 0.2, item 5: the album window shows the whole collection, as a stamp album does: pages by
group, collected pieces in their places, missing ones as dim silhouettes. Today it is the book window with a progress line and
one flat grid of what is missing (`LedgerDialog`, `GridDialog`; `docs/domain.md`, Windows).

The view is designed **before** the shape of a collection (`2026-11-04-collections-design.md`, C7), so that the shape gives
the view what it needs (groups, order, names, a group's layout), and built **after** the core of collections (0.2, item 3),
on the one collection resolver. Each collection's own look (its grouping, order and names in the view, "Nightfall, 2 of 3")
is item 6, not this plan.

## How to work on this plan

As in the previous plans: one step per short session with the project skill `/step <id>` (`.claude/skills/step/`). Each step
says **Branch**, **After**, **Model**, **Author** (a decision or in-game work only the author can do; "none" means the session
goes ahead on its own) and **Done when**. A session that stops in the middle writes one line under "Where we stopped".

The agent does not run the game: what only the game client can show (the look of a window) is the author's, with exact steps
written by the agent. Mockups are ASCII or screenshots of a probe in game.

## Steps

- [ ] **A1. What the game's GUI can do.** Research in vsapi and the decompiled engine (`ilspycmd`), and a probe if the sources
      do not answer:
      - a stack drawn dim or as a silhouette in a slot (the colour argument of `RenderItemstackToGui`, a slot grid of our own,
        `ItemSlot.BackgroundIcon`, a shader); the tooltip of such a slot (the piece's name without having it);
      - pages: several slot grids in one window, text between them, page buttons or tabs (`GuiElementHorizontalTabs`), how
        vanilla windows with pages do it (the handbook, the journal);
      - slots placed freely (a 2×2 picture as 2×2, not a row), and what that costs against one `SlotGrid` per group;
      - where a tapestry picture's shape comes from: the game's `sizes` and `tapestryGroups`, whether width × height and a
        piece's place in the picture are known (`TapestryPieces` today knows the picture and the order only);
      - the cost: a page recomposed on every refresh (`GridDialog.Watch`, every 500 ms on change) against C6's numbers;
      - a compact header (Decisions, "Compact window"): the name as text that turns into a text input by a small button, the
        button turning into "Save" (and back on Enter, Escape, losing focus); small icon buttons (vanilla's own icons, ours
        as a texture), their tooltips; text sizes smaller than `WhiteSmallText`;
      - facts as elements, not text: a stack drawn outside a slot grid with its count (the sheets as parchment with
        "27"), an icon with a number, a progress bar (vanilla's `GuiElementStatbar` or as the handbook draws one), hover
        tooltips on such elements;
      - how much fits without a scrollbar: the window's height on the smallest screen the game supports at the default GUI
        scale, against today's 6 visible rows.
      Branch: `docs/album-view`. After: the plan's own commit. Model: opus. Author: none.
      Done when: Decisions has the answers with sources; `docs/modding.md` has what surprised us; the open points for A2 are named.
- [ ] **A2. The view on paper.** Mockups of a page for tapestries and for ore crystals, seashells and butterflies as C1–C3
      describe them (or as far as they have got). The author decides:
      - what a page holds: one group or several, by what rule; a group too large for a page (butterflies);
      - the page header and the progress (whole album, this page, this group), a mark on a complete group;
      - navigation: buttons, a list of groups, jumping to the first page with something missing;
      - what stays of today's window: search (jumps to a page?), sorting (no: the collection's order), a filter "missing only";
        the name, sheets, "Start over" and "Make a want list";
      - the tooltip of a collected and of a missing piece;
      - the header by "Compact window": the name and its edit button, the kind of book, the sheets as parchment with a count,
        the progress as elements; what stays as text at all; the sizes of text;
      - whether the ledger and the bill of materials get the same header now (the window is shared, `LedgerDialog`) or later.
      Then **what the view needs from the shape of a collection**, written as input for C7: groups and their order, a group's
      name and layout, a member's place in it.
      Branch: `docs/album-view`. After: A1. Model: opus. Author: decides; must be done before C7.
      Done when: Decisions has the agreed view with its mockups; C7 of the collections plan names A2's list as an input;
      `docs/domain.md` (Windows) describes the album window as agreed; the branch goes to `main` by a pull request.
- [ ] **A3. The view built.** The album window by A2, on the collection resolver of the core (item 3). The ledger and the bill
      of materials keep today's window: the album gets a window of its own, or `LedgerDialog` a mode (A2 decides, with
      `docs/architecture.md` if a class is added). The decisions of the view (which page, what is shown where) pulled into
      testable code where they can be, with unit tests (`ROADMAP.md`, Later, tests for the client code).
      Branch: `feat/album-view`. After: A2, the core's plan done. Model: opus. Author: none.
      Done when: the build and all test projects pass; the author has the exact steps of the in-game check.
- [ ] **A4. Checked in game, docs.** The author checks the window in game (en and uk, memory: in-game checks by language):
      empty, partly full and full album; a refresh while open (taking new from a chest); a long name of a piece in both
      languages. `docs/testing/functional.md` gets the cases; both READMEs say what the album window shows (the screenshots
      come with item 6).
      Branch: `feat/album-view`. After: A3. Model: sonnet. Author: the check in game.
      Done when: the checks pass or their fixes are in; the docs are committed.
- [ ] **A5. Code review.** The roles of `docs/review.md` on the branch (or once at the end of the release: A2 decides).
      Branch: `feat/album-view`. After: A4. Model: opus. Author: goes through the findings.
      Done when: the findings are fixed or rejected with reasons; the branch goes to `main` by a pull request.

## Decisions

- **Pages by group** (the author, 04.11.2026), as a stamp album: a page holds groups (pictures for tapestries) with their
  pieces in place, turned with buttons. Rejected: one scrolling list with group headings (closer to today's window, but not an
  album), and one flat grid with a group per row (cheapest, but groups read poorly).
- **Missing pieces as dim silhouettes** (the author, 04.11.2026), as stamp catalogues show a missing stamp: the player sees
  what to look for. Rejected: an empty cell with the name in the tooltip only. How it is drawn: A1.
- **Compact window** (the author, 04.11.2026). Today's header is too tall and too large, and the collection starts too low;
  the row "label, text input, Save" for the name is the worst of it. Principles for A1 and A2:
  - the collection as high as it can be: the header takes as little height as it can;
  - the name is a label with a small button next to it; the button turns the label into a text input and itself into
    "Save";
  - as little vertical scrolling as possible (pages help);
  - facts are interface elements, not lines of text: e.g. the sheets as parchment drawn with its count. No "of 32": the
    limit is the stack size of parchment, which players know; the limit (`MaxSheets`, set in the config) goes into the
    tooltip. It matters little: it is reached by crafting, and at the limit the recharge recipe stops working;
  - the collection gets the visual focus, since it is the point of the album; the rest gives way to it. Not "everything
    smaller" as a goal; the sizes of text are looked at in A1's probe and decided in A2.
  A1 finds out what of this the game can do, so that A2's mockups use only what is possible.
- **Only the view** (the author, 04.11.2026). Telling tapestry pieces apart in lists ("Nightfall, 2 of 3") and each
  collection's look stay in `ROADMAP.md`, 0.2, item 6.
- **No change to stored data expected:** the view reads what the album already keeps. If A2 or A3 changes that, the change
  goes into the upgrade table (`docs/testing/upgrade-0.1-to-0.2.md`).
- **Step letter A** (album view); B, C and P are taken by open plans.

## Where we stopped
