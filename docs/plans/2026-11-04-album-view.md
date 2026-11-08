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

- [x] **A1. What the game's GUI can do.** Research in vsapi and the decompiled engine (`ilspycmd`), and a probe if the sources
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
        scale, against today's 6 visible rows;
      - a mark drawn over a slot (a frame or a small icon in a corner), for rarity (collections plan, Decisions, C2, C3);
      - a slot whose icon cycles through several stacks, as the handbook may show a recipe ingredient that takes any of several
        items, for a member filled by any of several codes (collections plan, C1 level 1, C2 level 1, C4 berry bushes);
      - a table: a slot grid with row and column headings (text or icons), for a page of two Cartesian dimensions (ore
        crystals: rock × grade, seashells: type × colour; collections plan, C1, C2).
      Branch: `docs/album-view`. After: the plan's own commit. Model: opus. Author: none.
      Done when: Decisions has the answers with sources; `docs/modding.md` has what surprised us; the open points for A2 are named.
- [x] **A2. The view on paper.** The input is the collections plan's Decisions, C1–C5 (final, 04.11.2026): collections have
      **dimensions**, hierarchical (tapestries: picture → piece, the picture's size) or Cartesian (ore crystals: ore × rock ×
      grade, only some points allowed; seashells: type × colour), **levels** (crystals 16 / 118 / 353, seashells 7 / 42),
      **members filled by any of several codes** (level 1 of both) or by a rule on an attribute (C4, berry bushes),
      **per-member attributes** such as rarity, and flat collections with no dimensions (custom, C4). Mockups of a page for
      tapestries, for ore crystals at levels 1 and 3, for seashells at both levels and for a flat collection; butterflies only as
      a list too long for a page (their grouping is decided later, collections plan, "Butterflies last"). The author decides:
      - what a page holds: one group or several, by what rule; a group too large for a page (butterflies);
      - a Cartesian collection: which dimension turns the pages and which two make the table on a page (e.g. a page per ore,
        rock × grade on it); whether the player can switch the dimension, and what the default is;
      - levels: one album with a switch of level, or a level chosen when the album is made (then fixed); the player's side of
        C1's open question "one album or three collections";
      - a member filled by any of several codes: its silhouette (one icon, or cycling ones), its tooltip ("bountiful native
        copper, any rock"), and whether a collected one shows the stack actually put in (the album then remembers that code:
        a change of stored data, for C7 and the upgrade table);
      - the mark of a per-member attribute (rarity) on a cell, with no tiers decided yet (`ROADMAP.md`, Maybe);
      - what the player sees of a collection disabled by the config (`Collections.<id>.Disabled`, collections plan, C5) in an
        album already made;
      - the page header and the progress (whole album, this page, this group), a mark on a complete group;
      - navigation: buttons, a list of groups, jumping to the first page with something missing;
      - what stays of today's window: search (jumps to a page?), sorting (no: the collection's order), a filter "missing only";
        the name, sheets, "Start over" and "Make a want list";
      - the tooltip of a collected and of a missing piece;
      - the header by "Compact window": the name and its edit button, the kind of book, the sheets as parchment with a count,
        the progress as elements; what stays as text at all; the sizes of text;
      - whether the ledger and the bill of materials get the same header now (the window is shared, `LedgerDialog`) or later.
      Then **what the view needs from the shape of a collection**, written as input for C7 in the words of dimensions: which
      dimensions turn pages and make tables, the order and the names of a dimension's values (and their icons, if headings
      are icons), a group's layout (a tapestry picture's width and height), a member's place in it, levels, the code that
      filled a member, per-member attributes.
      Branch: `docs/album-view`. After: A1. Model: opus. Author: decides; must be done before C7.
      Done when: Decisions has the agreed view with its mockups; C7 of the collections plan names A2's list as an input;
      `docs/domain.md` (Windows) describes the album window as agreed; the branch goes to `main` by a pull request.
- [x] **A3. The view built.** The album window by A2, on the collection resolver of the core (item 3). The ledger and the bill
      of materials keep today's window: the album gets a window of its own, or `LedgerDialog` a mode (A2 decides, with
      `docs/architecture.md` if a class is added). The decisions of the view (which page, what is shown where) pulled into
      testable code where they can be, with unit tests (`ROADMAP.md`, Later, tests for the client code). Both header
      variants (A and C) and both tab variants (several groups a page, or one tab one page; Decisions) built behind
      temporary switches, for the author to compare in A4.
      Branch: `feat/album-view`. After: A2; the core's plan (`2026-11-07-collections-core.md`) merged into `main` after its review, K7. Model: opus. Author: none.
      Done when: the build and all test projects pass; the author has the exact steps of the in-game check.
      Split in four (the author, 08.11.2026: one layer each), each on the branch above, After the one before, Model: opus,
      Author: none:
  - [x] **A3a. The page model in the core.** Pure C#, no game DLLs: from a level, a layout, the album's collected members and
        a page's size, the pages (whole groups in order, a group larger than a page going on with "(continued)"), the tabs
        (when the grouping has names and there is more than one page; ✓ of a complete group; both tab variants), the cells
        of a table (rows and columns only of values that occur; a point with no member is background) and of a shape (x, y),
        a cell's state (collected, missing) and what it shows (the stack put in, or the member's stacks to cycle), search
        matches and the next page with one.
        Done when: unit tests pass, mutation-checked; nothing draws it yet.
  - [x] **A3b. The element that draws a page.** A `GuiElement` of ours: stacks (missing translucent, alpha 0.35), flat tapestry
        textures, table headings (stacks or text), the rarity corner, cycling stacks (once a second, paused while hovered),
        the game's tooltip through a slot of ours; a refresh flips flags without composing again.
        Done when: the build and all test projects pass.
  - [x] **A3c. The album window.** `AlbumDialog`, a class of its own (`docs/architecture.md`): the row of controls (level
        while empty, layout, search, page buttons, "Make a want list", ↺), both tab variants behind a temporary switch, the
        line of a collection disabled on the server; an album opens it, the ledger and the bill of materials keep
        `LedgerDialog`.
        Done when: the build and all test projects pass.
  - [x] **A3d. The header, variants A and C** behind a temporary switch: the name with our SVG pencil turning into a text
        input and "Save" (Enter, Escape), the sheets as parchment with a count, the progress bar with its number.
        Done when: A3's "Done when".
- [x] **A4. Checked in game, docs.** The author checks the window in game (en and uk, memory: in-game checks by language):
      empty, partly full and full album; a refresh while open (taking new from a chest); a long name of a piece in both
      languages; the levels and the layouts; the header variants A and C and the two tab variants side by side, the author
      keeps one of each and the others are removed. `docs/testing/functional.md` gets the cases; both READMEs say what the album window shows (the screenshots
      come with item 6).
      Branch: `feat/album-view`. After: A3. Model: sonnet. Author: the check in game.
      Done when: the checks pass or their fixes are in; the docs are committed.
- [ ] **A5. Into `main`.** No review of its own: the code review of the window is part of R1 of the release plan (A2,
      Decisions). The branch goes to `main` by a pull request.
      Branch: `feat/album-view`. After: A4. Model: sonnet. Author: approves the pull request.
      Done when: the branch is merged into `main`.

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
  goes into the upgrade table (`docs/testing/upgrade-0.1-to-0.2.md`). One known candidate: remembering the code that filled
  a member filled by any of several codes (A2 decides whether the view shows it). A2 (07.11.2026) adds two: the chosen level
  and that code ("The view on paper (A2)", the end).
- **A1 and A2 updated by the results of C1–C6** (the author, 04.11.2026): written before the collections research, they spoke
  of groups only. The order stays (A1, A2, then C7): what A2 owes C7 changed, it did not go away; levels and the code shown for
  a collected member are the player's side and are decided in A2. Rejected: C7 first, with the view adapting later (the shape
  would guess the player's side).
- **Step letter A** (album view); B, C and P are taken by open plans.

### What the game's GUI can do (A1)

Final. Researched 04-05.11.2026 in vsapi, vssurvivalmod, vsessentialsmod (1.22.7), the decompiled engine and a probe in game
(a temporary window `.ccprobe` and a compose benchmark `.ccbench`, the author's screenshots 05.11.2026; the probe is removed).
"Probe" below means seen in that window.

- **A missing piece, drawn dim.** The slot grid always draws a stack white (`GuiElementItemSlotGridBase.cs:504#WhiteArgb`), but
  there are two ways around it, both shown by the probe:
  - **a slot of our own** in a vanilla grid: `ItemSlot.OnBeforeRender` is virtual (`ItemSlot.cs:529#OnBeforeRender`) and
    runs on every draw, after the game's rot overlay and before the block's own `OnBeforeRender` (decompiled
    `InventoryItemRenderer.GetItemStackRenderInfo`). It can set an overlay texture and its opacity, as the game does for rot
    (`ItemRenderInfo.cs:57#SetRotOverlay`): a dark overlay at 1.0 is a black silhouette, at 0.6 the item shows through it.
    Search, scrolling and tooltips stay vanilla's. The tooltip's icon is drawn from the same slot, so it is dark too;
  - **our own element** drawing stacks with `RenderItemstackToGui` and a colour: the colour multiplies the texture
    (`gui.vsh` line 59 `color = rgbaIn`, `gui.fsh` `texture(tex2d, uv) * color`), so a dark colour is a silhouette and
    white with alpha 0.35 is a translucent item. Hover and tooltips are then ours: `capi.Input.TriggerOnMouseEnterSlot(slot)`
    shows the game's tooltip for any slot (probe), as the grid itself does (`GuiElementItemSlotGridBase.cs:904#TriggerOnMouseEnterSlot`).
  - **The author's choice of look** (05.11.2026): the translucent item (alpha 0.35, "E" of the probe) reads most like a dim
    silhouette, then the overlay at 0.6 ("C"); the black ones (overlay 1.0, dark colour) less so.
- **The tooltip of a piece not had:** the title is `slot.GetStackName()` and the text `slot.GetStackDescription()`
  (`GuiElementItemstackInfo.cs:126#GetStackName`), both virtual (`ItemSlot.cs:497#GetStackName`, `ItemSlot.cs:508#GetStackDescription`):
  a slot of ours gives any name and any lines ("missing", "any rock") above the item's own text (probe).
- **Pages and tabs.** Vanilla pages: the journal turns pages with buttons and a "1 / N" text
  (`GuiDialogJournal.cs:144#Previous`); character creation uses the icon buttons "left" and "right"
  (`GuiDialogCreateCharacter.cs:232#AddIconButton`). Tabs: horizontal tabs above the dialog (the trader,
  `GuiDialogTrader.cs:116#AddHorizontalTabs`), vertical ones at the side (the handbook). Several slot grids, texts and
  custom elements live in one composer (probe).
- **Slots placed as a picture.** A grid lays its slots out in rows of `cols`, packed with no gaps
  (`GuiElementItemSlotGridBase.cs:243#SlotBounds`), so one grid per picture with `cols` = the picture's width gives 2×2 as 2×2
  and 3×1 as a row (probe). A point of a table that does not exist cannot be a gap in one grid: it is a slot of its own
  (empty, tinted) or the table is drawn by several grids.
- **Tapestries drawn as a picture.** The inventory icon is the 3D model at an angle (`tapestry.json` `guiTransform`, rotation
  x -22.6, y 25), so pieces side by side do not make a picture. Two ways, both in the probe:
  - **the piece's own texture, flat:** every piece has its own image, the one on the wall,
    `textures/block/cloth/tapestry/{type}.png` (`BlockTapestry.cs:40#texturePath`); drawn as 2D textures with no gaps the
    pieces make the whole picture, and a missing piece at alpha 0.35 shows where it goes (probe: salvation 2×2, ambush 3×1).
    A mod's tapestry must have its image there too, or the game cannot draw it on the wall either;
  - the 3D model turned to the viewer by the slot's `renderInfo.Transform` (rotation y 0 is the front, 180 the back): each
    piece keeps its own frame and the slots their gaps; no picture.
  The flat textures are drawn by our own element, so hover, tooltip and search are ours there.
- **A picture's shape is known.** `sizes` gives width × height: "3x1" is three pieces in a row (`BlockTapestry.cs:140#neighbours4x1`:
  the neighbours of 2x1, 3x1, 4x1 differ in x), "1x2" a column. A piece's place is in its code: one digit for a row or a column
  (1 = left or top), two for 2×2, row first (`BlockTapestry.cs:441#Bottom`: "21" is bottom left). `TapestryPieces` makes the
  same codes. A vanilla oddity: the texts of the 4×1 sections say "Top quarter" etc. (`BlockTapestry.cs:431#Top`) while the
  game treats 4×1 as a row; the gate is the only 4×1. `tapestryGroups` is not a grouping for us: it lists pieces of the same
  position that worldgen may swap with each other (`BETapestry.cs:101#blockGroups`).
- **The cost of composing** (`.ccbench`, dev client, the author's laptop, median of 11): one grid of 88 slots 6.4 ms, of 400
  slots 7.2 ms; a page of 12 grids × 4 slots 16.1 ms, of 30 grids × 2 31.7 ms; the probe window (9 grids, ~40 slots, texts) 41-80 ms.
  The cost is per grid (each composes its slot, highlight and cross textures), not per slot. A page of ~30 pictures is two
  frames, fine for turning a page, not for every refresh. **Hence:** a refresh (a piece taken from a chest) changes the slots'
  "missing" flag and the progress, without composing again; both the overlay and the colour are read on every frame. Today's
  `GridDialog.Watch` recomposes the whole window on any change.
- **A compact header.**
  - The name: a label, and an icon button that composes the header again with a text input in its place; Enter and Escape
    are passed on by a one-line input (`GuiElementEditableTextBase.cs:656#handled`, `GuiElementEditableTextBase.cs:660#Escape`), so the dialog
    catches them in its own `OnKeyDown`; Escape normally closes the dialog (`GuiDialog.cs:506#OnEscapePressed`, virtual).
    Losing focus has no event, only the virtual `OnFocusLost` of the element (a subclass of the input, or a check of `HasFocus`).
  - Icon buttons: `AddIconButton` draws a named icon (`GuiElementToggleButton.cs:102#DrawIcon`); vanilla's: `left`, `right`,
    `plus`, `paintbrush`, the clothes slots and the waypoint icons, and few SVGs (`checkmark`, `copy`, `paste`, `warning`). No
    pencil. Our own SVG goes into `capi.Gui.Icons.CustomIcons` (`IconUtil.cs:19#CustomIcons`), as the waypoint map does
    (`WaypointMapLayer.cs:116#CustomIcons`). Their tooltips: `AddHoverText` over the same bounds (probe).
  - **Text sizes:** `WhiteSmallText` 16, `WhiteDetailText` 14 (`GuiStyle.cs:76#DetailFontSize`); any size by `WithFontSize`.
    The author (probe, 05.11.2026): 12 and 11 do not read; **14 is the smallest size we use**.
- **Facts as elements** (all in the probe): the sheets as a parchment stack with its count, drawn by `AddPassiveItemSlot`
  without a background (the count is drawn by the game), its tooltip by `AddHoverText`; a progress bar `AddStatbar`
  (`GuiElementStatbar`) with a text on hover (`GuiElementStatbar.cs:43#ShowValueOnHover`, `onGetStatbarValue`: "57 / 88"); it
  draws segment lines, and its hover text shows below the bar.
- **A mark on a slot** (probe): a whole slot tinted by `ItemSlot.HexBackgroundColor` (`ItemSlot.cs:54#HexBackgroundColor`); a small
  texture over a corner, drawn after the grid at z 250, as the grid draws its own red cross over an item
  (`GuiElementItemSlotGridBase.cs:512#crossedOutTexture`); or an icon in the slot's background (`AlwaysRenderIcon` and a
  `DrawIconHandler` of ours, `GuiElementItemSlotGridBase.cs:459#AlwaysRenderIcon`), under the item.
- **A slot cycling several stacks:** setting the slot's stack every second works with no composing (probe, a crystal changing
  under the overlay). The handbook's slideshow does the same, one second per stack, paused while the mouse is over it
  (`SlideshowItemstackTextComponent.cs:33#secondsVisible`).
- **A table with headings:** texts (`AddStaticText`) or stacks (`AddPassiveItemSlot` without background) placed at the rows and
  columns of a grid; nothing vanilla does as a table, but all of it is plain placement.
- **How much fits** (by calculation, not measured): a slot row is 51 (slot 48 + padding 3). The game's default window is
  1024×768 and its default GUI scale 1.0 (decompiled `ClientSettings`: `screenHeight` 768, `guiScale` 1; the slider goes 0.5-3).
  At 768: screen padding 20, title 31, dialog padding 40 leave ~677; a one-line header (~30), a line of page buttons (~30) and
  gaps (~20) leave ~597, **about 11 rows** (10 at 720). Today's window shows 6.

Open points for A2:
- the look of a missing piece: translucent (needs our own element) or overlay 0.6 (keeps the vanilla grid); for tapestries
  the flat textures, which need our own element anyway;
- whether the album window is one element of ours per page (one way of drawing, hover and search ours) or vanilla grids with
  our slots (plus our element for tapestries);
- a refresh without composing again: what changes in place (flags, counts, progress) and what turns a page;
- the name's edit button: our SVG pencil or a vanilla icon;
- what the table of a Cartesian page uses as headings (texts or stacks) and how a point that does not occur is shown.

All five answered in A2 below.

### The view on paper (A2)

Final, but for two pairs of variants left to A4 in game (the header, the tabs). Decided with the author 07.11.2026; an HTML
mockup (not kept) was looked at after the decisions. Mockups are ASCII: `███` a collected piece, `░░░` a missing one (translucent), a
blank a point that does not exist. Texts in the mockups are Ukrainian, as they were discussed; sizes are rough.

**Pages and groups**
- **A page holds whole groups, in order, until it is full.** A group is never split; a group larger than a page goes on over
  the next pages, its heading marked "(continued)". Rejected: one group per page (31 pages of tapestries, a page of seashells
  nearly empty).
- **Tabs for the dimension that groups, when its values have names and there is more than one page:** vertical tabs at the
  side, as the handbook's (ore crystals at level 3: 16 ores, ~480 px, fit at 768). A complete group has ✓ on its tab. No tabs
  for a single page, nor for pages filled by count only (tapestries by picture, butterflies): 31 pictures are too many tabs.
  **Two variants, compared in game in A4** (the author, after an HTML mockup 07.11.2026: several tabs lit for one page felt
  unusual, not surely bad): **pages of several groups**, a tab turning to the page with its group and the tabs of all groups
  on the page lit; or **one tab, one page**, a group per page when the grouping has tabs.
- **Page buttons ◀ N / M ▶ always** when there is more than one page (crystals at level 3: 8 pages, with tabs too). Rejected:
  a "first page with something missing" button (the author: overload; the ✓ on tabs already tells).
- **A Cartesian group is a table** with headings: rows and columns are two dimensions, the third (if any) groups. Headings are
  stacks (a rock's stone, a shell of the colour) with the name in their tooltip, narrower than text in Ukrainian; a dimension
  with no item of its own (the crystal's grade) is headed by text. A column or row exists only for values that occur in the
  group. **A point that does not occur is plain background**: no slot frame, no tint (the author).
- **Layouts: the player switches them at any time** (the author: "should be cheap, fewer screenshots to redo later"). A
  layout says which dimension groups and how a group is laid out; it changes nothing in the data. Each collection has a
  default. Kept **on the client**, per collection, for the game session: no change of stored data, no packet, and an album
  handed over opens in the receiver's usual layout. Rejected: the layout remembered in the album.

The mockups, as agreed (the bottom row of the header variant C, below, left out except in the first):

Ore crystals, level 1 (16): one page, no tabs.
```
┌ Альбом: кристали ──────────────────────────────────────────────── ✕ ┐
│  ███  ███  ░░░  ███  ███  ░░░  ███  ███                              │
│  ░░░  ███  ███  ░░░  ███  ░░░  ░░░  ███                              │
├──────────────────────────────────────────────────────────────────────┤
│ Рівень: [Найбагатші (16) ▾]  [За рудою ▾]  🔍[____]  [Список бажаного] [↺] │
│ Мої кристали ✎               ▓▓▓▓▓░░░░ 9/16                    📜 27 │
└──────────────────────────────────────────────────────────────────────┘
```
Ore crystals, level 2 (118: one member per ore × rock; the grade shows in the icon): ores as rows, two pages of 8.
```
│                  анд гран баз сл  філ крем пісч вапн мрм кмб …       │
│  Самородна мідь  ███ ░░░  ███                                        │
│  Малахіт         ███           ░░░          ███                      │
│  …  (8 руд)                                                          │
│                          ◀ 1 / 2 ▶                                   │
```
Ore crystals, level 3 (353), layout "by ore": tabs by ore, a table grade × rock per ore, 2–3 ores a page.
```
│ ▸Самородна мідь │ Самородна мідь                         21/34      │
│  Малахіт        │          анд  гран  баз  сл  …                    │
│  Каситерит      │ бідна    ███  ███   ░░░  ███                      │
│  Кварц: золото ✓│ середня  ███  ░░░   ███  ░░░                      │
│  …              │ багата   ░░░        ░░░                           │
│                 │ щедра    ░░░        ░░░                           │
│                 │ Малахіт                                3/12       │
```
The same, layout "by rock": tabs by rock, a table grade × ore per rock.
```
│ ▸Андезит │          мідь  мал  кас  кв.з …
│  Граніт  │ бідна    ███   ░░░  ███  ░░░
│  …       │ середня  ░░░   ███  ███  ███
```
Seashells, level 1 (7) and level 2 (42, type × colour): one page each, no tabs.
```
Level 1                               Level 2
 гребінець ███  сонячна ░░░ …                   латте прост морськ пурп кориц бірюз
                                     гребінець  ███   ███   ░░░     ░░░  ███   ░░░
                                     … (7 rows)
```
Tapestries, layout "by picture" (default): pictures as flat textures (A1), pages by count, no tabs; layout "by size": tabs
by size (1×2, 2×1, 3×1, 2×2, 4×1).
```
│ Засідка ✓           Порятунок            Сутінки                     │
│ ███ ███ ███         ███ ░░░              ███ ░░░ ░░░                 │
│                     ░░░ ███                                          │
│ Брама                                    Ловці                       │
│ ███ ░░░ ░░░ ░░░                          ░░░ ███                     │
│                          ◀ 2 / 5 ▶                                   │
```
Butterflies (169) and a flat collection: one group, pages by count (~2 pages of 10 × 9 for butterflies) until their grouping
is decided (collections plan, "Butterflies last").

**Levels**
- **One album, the level chosen in its window while it is empty** (the author: "B + a"). A dropdown in the bottom row,
  shown only while nothing is collected; after the first piece the level is fixed (and named in the progress tooltip);
  "Start over" frees it again. The window follows the level: a level of 7 pieces is a simple page, one of 353 has tabs and
  pages. Rejected: the level fixed when the album is made (by the recipe, or as three collections), and a level switch on a
  filled album (taking new would depend on what the screen shows).
- **An album with no level chosen does nothing:** taking new (Shift + RMB) refuses with "Choose a level in the album's window
  (RMB)"; no want list button; the tooltip says "No level chosen". **No default level:** a player clicking a chest before
  opening the window would get a level fixed that they never chose. A collection with one level (tapestries) has it chosen
  from the start, as today.
- **Stored data:** the album keeps the chosen level (and "none"): for the upgrade table (C7).

**The cell**
- **One element of ours draws a page**, for every collection: stacks, flat tapestry textures, table headings, group frames.
  Hover gives the game's tooltip (`TriggerOnMouseEnterSlot`, A1); search is ours. Rejected: vanilla grids with our slots (two
  ways of drawing; ~32 ms per refresh for a page of 30 pictures).
- **A missing piece is the item translucent** (alpha 0.35; the author's choice in A1).
- **A refresh** (a piece taken from a chest while the window is open) flips the "missing" flags and the progress, without
  composing the page again; composing happens on turning a page, switching a layout or a level.
- **A member filled by any of several codes** (crystals and seashells at level 1): missing, its icon cycles through the
  possible stacks once a second, paused while hovered (as the handbook's slideshow); collected, it shows **the stack actually
  put in**. **Stored data:** the album remembers that code (upgrade table, C7).
- **Rarity mark: a coloured corner** (a small triangle in a corner of the cell; the tier is its colour), drawn over the stack
  as the game draws its red cross. The cell's background is not tinted: it already says collected / missing / does not exist.
  Rejected: a stone or a star icon (the author). A frame and a foil effect (as in MTG) are ideas for the rarity feature
  (`ROADMAP.md`, Maybe). Whether 0.2 shows any rarity is C7's and item 6's (rare seashell colours are the first candidate).
- **Tooltips:** the title is the piece's name; then "✓ In the collection" or "✗ Missing"; for a member of several codes, a
  line naming the freedom ("Any rock"); a rarity line with the mark; then the game's own tooltip of the stack (a crystal's
  "Parent material" tells crystals of two rocks apart, the open point of C1).

**The header and the bottom of the window**
- **Two variants, compared in game in A4, the other removed** (the author):
  - **A:** the game's title bar, and under it our row: the name with ✎, the progress in the centre, the sheets on the right;
  - **C:** the game's title bar with the kind of book and the collection ("Альбом: кристали"), and our row at the **bottom**
    of the window, outside the scrolling area if one appears.
  Rejected: B, our row in place of the title bar (thinner, but dragging the window would be ours to write).
- **The bottom row** (variant C; in A the same elements move up): the name with ✎, the progress in the centre, the sheets on
  the right. Above it a row of controls: the level (while empty), the layout, search, the page buttons, "Make a want list"
  (a text button: the main action) and "Start over" (an icon ↺ with a tooltip, confirmation as today).
- **The name:** a label and our own SVG pencil (the game has none, A1); the pencil turns the label into a text input and
  itself into "Save"; Enter saves, Escape cancels.
- **The sheets:** a parchment stack with its count; the limit (`MaxSheets`) in its tooltip.
- **Progress:** the whole album as a bar in the centre with its number always shown (the game's statbar shows it on hover
  only, A1); a group's "21/34" in its heading; a complete group's ✓ in its heading and on its tab. No progress per page.
- **Text sizes:** group headings 16 (`WhiteSmallText`); table headings, tabs and the bottom rows 14; nothing smaller (A1).
- **What stays of today's window:** search stays (the matches on the page lit, the rest dimmed; Enter turns to the next page
  with a match), though the game's own search works poorly (the author). Sorting goes: the collection's order and the layout
  replace it. No "missing only" filter: every piece has its place, and the want list says what to look for.
- **A collection disabled by the config** (`Collections.<id>.Disabled`), in an album already made: the window opens and shows
  what was collected, with a line "This collection is disabled on the server"; taking new refuses with the same text; no want
  list button; the data is kept, so enabling the collection again brings everything back. Its recipe: C7.
- **The ledger and the bill of materials keep today's window in 0.2:** the album gets a window of its own (a class of its own:
  A3, with `docs/architecture.md`). New windows for the other two: two lines in `ROADMAP.md`, Interface.
- **The code review of A5 is part of R1** of the release plan, the review of all of 0.2: the window is read together with
  the core it stands on, one run instead of two.

**What the view needs from the shape of a collection** (input for C7, in the words of dimensions):
1. **Dimensions:** each with its values in order, their names (lang keys) and optionally an icon stack (table headings,
   tabs); hierarchical (picture → piece, the size belongs to the picture) or Cartesian (ore × rock × grade).
2. **Layouts,** several per collection, one the default; each says which dimension groups (tabs, if its values have names)
   and how a group is laid out: a table of two dimensions (rows, columns), the group's own shape (a picture's width × height
   and a piece's place x, y in it), or filling in order. A collection with no dimensions is one group filled in order.
3. **A group:** a value of the grouping dimension, its name, its members; complete when every member is collected.
4. **A member:** its value in every dimension; what fills it (one code, any of several codes, a rule on an attribute, C4);
   **the stacks to show** (one, a list to cycle, a representative for a rule); per-member attributes (rarity → the corner's
   colour). A point of a table with no member is background.
5. **A flat texture** per member where the definition gives one (tapestry pieces, `textures/block/cloth/tapestry/{type}.png`):
   an option of the definition, not a rule for all.
6. **Levels:** a list, each with its own member set and its own dimensions (crystals: 1 ore; 2 ore × rock; 3 ore × rock ×
   grade). A collection with one level shows no choice.
7. **The collection's name** for the title bar ("Альбом: кристали").
8. **`Disabled` seen by the client:** the config is not synced (C5), so how the client knows is C7's.
- **Stored data** for the upgrade table: the chosen level (and "none"); the code that filled a member of several codes.

### The page model (A3a)

The agent's choices, 08.11.2026 (`AlbumPages`, `docs/architecture.md`, Core); the author may overrule any in A4:
- **Positions in the caller's units** (`PageMetrics`: the page, a cell, a heading line, a gap, the width of a row heading in
  words, the narrowest group with a heading). The window measures texts and passes the numbers; the core never draws.
- **What is collected is not part of the layout:** pages and positions depend on the level, the layout and the metrics only;
  a refresh reads `Album.StacksToShow` and `MemberGroup.CollectedCount` when drawing.
- **Groups stand side by side while they fit the page's width, then on the next line** (tapestry pictures three in a row as in
  the mockup; crystal tables mostly one under another, being wide).
- **A table wider than the page widens the page** (`AlbumPages.Width`), it is not cut by columns: the window grows to it.
  Members in order wrap to the width.
- **A group larger than a page is cut between lines** of cells (table rows, lines of members in order); its first part
  stands where it would if one line fits there; a table repeats its column headings on every part. A group no larger than a
  page is never cut, even where some of its lines would fit.
- **Tabs:** by the layout's outermost grouping dimension, when it has names (`nameKey`) and the groups do not fit one page
  laid out as `Shared`; then `PerTab` lays them out a tab per page. A tab's ✓: all its groups complete.
- **Headings:** a dimension with an icon heads its rows and columns with a cell (a stack); one without, with words: a row
  heading of the measured width, a column heading a line high (a narrow column's word is the drawing's problem, A3b).

### The element (A3b)

The agent's choices, 08.11.2026 (`AlbumPageElement`, `AlbumSlot`); the author may overrule any in A4, where the look is checked:
- **The flat image is a member attribute** (`Member.FlatTexture`, `texture`), set by the tapestry source as it sets x and y,
  to the texture the game puts on the wall. Not a field of the definition: no other collection needs one yet. A server test
  checks every piece has its image.
- **A cell is the game's slot** (48, the slot grid's background texture) in its 51 of the page; a flat image fills the whole
  51, so the pieces of a picture touch. Missing: the stack, or the image, at alpha 0.35; the slot's frame stays opaque.
- **The check mark is vanilla's SVG** (`textures/icons/checkmark.svg`, green), not a "✓" in the text: vanilla's texts never
  use that glyph, so the font may lack it. Progress "21/34" right in the group's heading, the mark before it; the name cut
  with "…" to fit.
- **Words that do not fit are cut with "…"**: a group's name, a column heading in words (a cell wide). Their full text on
  hover is A3c's (the window's `AddHoverText`), as is a hover text on a heading in words; icon headings show the value's name
  as the game's tooltip (title only).
- **Tooltip lines** above the item's own: "In the collection" (green) or "Missing" (red), what a member of several items
  leaves free (`AlbumPageLook.Freedom`, the window's, e.g. `ItemInvoice.Freedom`), the rarity line. Group names, freedom and
  rarity come from the window (`AlbumPageLook`): rarity has no source yet (item 6), so A3c passes none.
- **The corner** is a white triangle tinted by the tier's colour, 12 in the top right, over the stack (z 250, as the grid's cross).

### The window (A3c)

The agent's choices, 08.11.2026 (`AlbumDialog`, `AlbumTabs`, `BookWindow`, `HeldItemDialog`); the author may overrule any in A4:
- **A base for windows about a held item** (`HeldItemDialog`: watching, `Shown`, `SetComposer`), split out of `GridDialog`:
  the album window has no slot grid. `BookWindow.Open` picks the album's window or the ledger's, one at a time; the ledger
  window no longer shows albums.
- **The page's size:** 12 cells wide (a wider table widens it), as tall as the screen allows less 270 for the title bar, the
  line, the page buttons and the rows under the page (A1, "How much fits"); at least 3 rows. Group headings 24 high, gaps 12, a
  group with a heading at least 3 cells wide.
- **Under the page:** the page buttons ◀ N / M ▶ centred (vanilla's `left` and `right` icons), then one row: the level (while
  empty, several levels), the layout (when the level has several), search on the left; "Make a want list" and ↺ (vanilla's
  `undo` icon, "Start over" on hover, only when something is collected) on the right. Variant A or C of the header is A3d's; till
  then the title bar has the album's name, as the ledger window does, and the name and sheets are not shown.
- **Tabs** are vanilla's vertical tabs outside the window on its left (as the handbook's), at 14; a complete tab has vanilla's
  check mark on its left. The temporary switch of the two variants is a button under the row, in English, shown only with tabs.
- **Search:** typing lights the cells whose names (the stack put in, or every stack of the member) contain the text and dims the
  rest (grey at 0.3); Enter turns to the next page with a match and keeps the focus. Typing does not turn pages.
- **Turning a page, a tab, a layout, a level composes the window; a piece taken flips cells** (and the tabs' marks). A new
  layout or tab variant stays on the page with the first piece of the page shown before.
- **With no level chosen,** the window shows the level and a line "Choose a level to start collecting", no page.
- **An album whose collection the game lacks** (switched off: "This collection is switched off on this server"; gone with its
  mod: "The game no longer has this collection…") shows what it collected, as one group in member id order
  (`Album.CollectedOnly`, the collection's own order being unknown); no level, layout, want list or ↺.
- **Cut names get a hover text** with the full name (group headings, headings in words); the room for a group's name is
  measured with its progress full ("34/34"), so the cut does not change as pieces come.

### The header (A3d)

The agent's choices, 08.11.2026 (`AlbumHeader`, a class of its own beside `AlbumDialog`); the author may overrule any in A4:
- **The title bar names the collection in both variants** ("Album: tapestries"; "Album" when the game lacks it); the album's
  name is only in our row. Before A3d the title bar had the name.
- **The row:** the name (14, cut with "…" to its half of the row, the full name on hover; "No name", dimmed, for an unnamed
  album) and the pencil (our SVG, `textures/icons/pencil.svg`, registered as a custom icon; "Rename" on hover) on the left; the
  bar in the centre, 180 wide, with "57/88" over it (as a group's heading) and, on hover, the level's name (a collection of
  several levels) and the progress line of the tooltip; the parchment on the right with the count drawn by us (a stack of 0
  sheets cannot be drawn), "Sheets: 27 of 32" on hover. No bar without a level, nor for a collection the game lacks.
- **Editing:** the pencil composes the window with a text input and "Save" in the name's place, focused. Enter and "Save" send
  the rename (`RenameLedgerRequest`, as the ledger window); Escape goes back without sending and keeps the window open.
  Losing focus does nothing (A1: no event for it). A refresh while editing that composes again (a level chosen) keeps the
  input but puts the stored name back in it.
- **The temporary switch** of A and C is a button "Header: A (top)" / "Header: C (bottom)" in a row of temporary switches
  under everything, beside the tab switch; A is the default. The page is 40 lower for the new row (`AroundThePage` 310); A4
  gives back what the switches' row took. *Superseded by A4: C kept, both switches and their row removed (below).*

**The in-game check of A3** (for A4; setting up: `docs/testing/commands.md`, books and tapestry pieces). In en, then uk:
1. An album of tapestries, empty: the title bar "Album: tapestries"; the row with "No name", the pencil, the bar at 0/N, the
   parchment with its count; pages of pictures with every piece dim; ◀ 1 / M ▶ turns pages.
2. The pencil: a text input with "Save"; type a name, Enter: the name shows, and the item's name in the hotbar too. Again,
   type, Escape: the old name, the window still open. A name of 40 characters: cut with "…", the full name on hover.
3. Shift + right-click a chest with some pieces, the window open: the cells turn full, the bar and its number grow, nothing
   flickers. Make a want list: the sheets' count drops by one.
4. "Header: A (top)" switches to C: the row moves under the controls; turning pages and the pencil work there too.
5. An album of ore crystals (several levels), empty: the level dropdown, "Choose a level to start collecting", no bar. Choose
   level 3: tabs on the left, the layout dropdown, the tab switch beside the header switch; both tab variants; a layout
   switched keeps the first piece of the page in view. Hover the bar: the level's name above the progress line.
6. Search: typing dims the cells that do not match; Enter turns to the next page with a match.
7. ↺ "Start over" after something is collected: the confirmation, then an empty album with the level free again.
8. A window at 1024×768 (the smallest the game supports): the page and every row fit with no part off the screen.

### Checked in game (A4)

Decided by the author, 08.11.2026, from the check in game (tapestries, seashells, `ccores` of the test mod; en and uk):
- **Header C:** the album's own row (name and pencil, progress bar, sheets) at the bottom, under the row of controls. A, the
  row under the title bar, is removed with its switch.
- **One tab, one page:** a tab's groups start a page of their own (`AlbumPages.Build`). Rejected: several groups of different
  tabs on a page, with every tab of the page lit: it surprises.
- **◀ ▶ turn the pages of the tab**, "N / M" counts them (`AlbumPages.PagerAt`); no page buttons for a tab of one page. Another
  tab is reached by its tab only.
- **Tabs taller than the window become a dropdown** in the row of controls, a complete tab with ✓ after its name.
- (09.11.2026) **Tabs are measured against the screen:** at the side while they fit the tallest window the screen allows, the
  page stretched down to them (`AlbumDialog.WindowHeight` with the metrics' height); the dropdown only when they do not (ores
  by ore at 1600×900 showed the dropdown when measured against the window, a page of an ore being 4 rows high).
- (09.11.2026) **The level dropdown leftmost, the tabs' dropdown right after it**, then the layout and search.
- The text of cut names, the page 3 × 4 cells wide, the pager in the row of controls, one left edge, ↺ as our own icon, the icon
  buttons as the game's text buttons, the rename shown at once: see "Where we stopped", 08.11.2026 (late evening, night).

The agent's choices, 09.11.2026; the author may overrule any:
- **No "Ore:" before the dropdown:** a dimension has a name key for its values only (`DimensionDef.NameKey`, "game:ore-{value}"),
  none for itself. A name of the dimension would be a field of the collection's shape (C7); a line for item 6 if wanted.
- **✓ in the dropdown is the glyph** (the dropdown draws text only, no icons, vsapi `GuiElementDropDown`); vanilla's texts never
  use it (A3b), so whether the font has it is for the check in game (A22 of `docs/testing/functional.md`).
- **A tab completed while the window is open composes the window** when the dropdown shows the tabs (its names carry the ✓); the
  tabs at the side flip their mark without composing, as before.
- **The page got back the 40** of the row of temporary switches (`AroundThePage` 282).

## Where we stopped

- 04.11.2026: A1 (three more GUI questions) and A2 (dimensions, levels, members of several codes, rarity marks, flat and
  disabled collections) updated by C1–C6 (Decisions). Next: A1 (opus, After: the plan's own commit).
- 05.11.2026: A1 done (Decisions, "What the game's GUI can do"; the probe removed). Next: A2 (opus, After: A1), on the same
  branch `docs/album-view`.
- 07.11.2026: A2 done (final: Decisions, "The view on paper (A2)", with the mockups and the list for C7; `docs/domain.md`,
  Windows). The branch `docs/album-view` waits for the author's pull request into `main`; then C7 of the collections plan.
  Next here: A3 (opus, After: A2, and the core merged after K7).
- 08.11.2026: A3 split in four (A3a–A3d, the author). A3a done (final): `AlbumPages` in the core with 15 unit tests,
  mutation-checked (six mutations, each caught); its choices in Decisions, "The page model (A3a)". Nothing draws it yet. Next:
  A3b (opus, After: A3a), on the same branch `feat/album-view`.
- 08.11.2026: A3b done (final as code; its look unchecked until A4): `AlbumPageElement` and `AlbumSlot` in `Gui/`, the hit
  test and `Slideshow` in the core (5 unit tests), the tapestry pieces' flat image (a server test); mutation-checked (four
  mutations, each caught). Choices in Decisions, "The element (A3b)". Nothing opens it yet. Next: A3c (opus, After: A3b).
- 08.11.2026: A3c done (final as code; its look unchecked until A4): `AlbumDialog` opens for every album, with tabs, pages,
  the row of controls, search, the temporary tab switch and the view of a collection the game lacks (`Album.CollectedOnly`, 2
  unit tests, mutation-checked). Choices in Decisions, "The window (A3c)". On this branch the album's name cannot be edited and
  its sheets are not shown until A3d. Next: A3d (opus, After: A3c).
- 08.11.2026: A3d done (final as code; its look unchecked until A4): `AlbumHeader` with the name and its pencil, the progress
  bar and the sheets, variants A and C behind a temporary switch. A3 done: its in-game check is in Decisions, "The header
  (A3d)". Next: A4 (sonnet, After: A3), the author's check in game.
- 08.11.2026: A4 half done (docs final as drafts of text, the in-game part waits for the author): cases A12–A21 in
  `docs/testing/functional.md` and the album paragraph of both READMEs (the "view of the whole album" line is gone from "What
  may come next"; screenshots come with item 6). Not done: the author's check in game (the list in Decisions, "The in-game check
  of A3", en then uk), the choice of one header and one tab variant, and removing the temporary switches and the others (the
  page then gets back the 40 the switches' row took, `AroundThePage`). Next: after the check, a session to remove the losing
  variants and fix what the check found; then A5.
- 08.11.2026, evening: A4 in-game check, first rounds (the author, tapestries in `en`; the test mod's seashells). All of this is
  **uncommitted work on `feat/album-view`** (a draft, builds, all tests green) and a list of what is still wrong; the next
  session (the author goes on with opus) starts from here.
  - **Decided by the author:** header **C** (the row at the bottom); variant A and its switch are removed from the code. The
    tab variant (several groups a page / one tab one page) is **not decided**: no album shows tabs yet (see below).
  - **Fixed, to be looked at again in game:** tapestry pieces of a 2×2 picture read as "yx" (row, column; the core, its test and
    `docs/modding.md`); group headings use the game's picture name (`tapestry-<picture>`, wildcard lang keys, so
    `Lang.GetMatching`), with the picture's code added when several pictures share a name ("Schematic (schematic-a)"); the group's
    count stands right after its name, dimmer; the sheets are a stack of parchment with its count as in the toolbar; ↺ is our own
    `restart.svg` (the game's `undo` is short of a turn, `redo` the wrong way round); each group has a frame (`AlbumPageElement.Pad`,
    `DrawFrames`), the gap between groups is 20, `AroundThePage` 322; the place of ↺ is kept when the album is empty (the want
    list no longer jumps); the bottom row: name with an indent, the bar fills the room up to the sheets.
  - **Still wrong (the author's screenshots 22:23–22:27):**
    1. Group names and counts, and table headings, are **hard to read, as if pixels were missing**. Padding the textures did not
       help; try whole-pixel positions (`Render2DLoadedTexture` at rounded x, y), the texture's size against the scaled font, how
       the game's own text elements draw at the same size. Names are also cut too early ("Schematic (…", "Blackguard…",
       "Banner of th…"): the room of a name is a picture's width, and the code added to a shared name makes it longer. Think of
       a shorter tell-apart (a letter, a number) or a wider frame for a name.
    2. **Pages:** still two for the tapestries (15 groups, then 16), and the right fourth of the page is empty: the three
       columns of groups fill ~75% of its width. Fewer groups a page (taller frames, the shelf that fits) did not change the count;
       find why the shelves stop at 3 and what makes the page 12 cells wide.
    3. **The page buttons still have a row of their own**: the test `pagerInRow` (both sides of the controls row must leave
       140 + gaps) fails with the want list (~220) and ↺ on the right. Move the pager into the row some other way, or put it
       elsewhere (beside "Make a want list"? in the bottom row?), or accept the row and fill it.
    4. **The left edges do not line up:** the name in the bottom row (indent 6) vs the search (tapestries) and the level
       dropdown (seashells), both at 0; the frames start at 0 too. One left edge for the page, the rows and the name.
    5. **↺: the author asked for the left horn smaller, the last change made it bigger.** Draw the icon on its own and look at
       it (render the SVG to a PNG) before putting it in game; ask which end the author means.
    6. **The ingots album (test mod `cccollections`) is senseless to look at** (metal codes as names, two identical levels, no
       layout): it exists for `CompatTests`, not for looking at. For the author's check the test mod needs a collection that
       looks like what item 4 will bring.
    7. **No album shows tabs**, so the tab variant cannot be chosen: tabs need a dimension with `nameKey` that groups, and more
       than one page. The seashells at level "shell" give one page of 42 without groups. Either add to the test mod a collection
       with named groups and enough members for several pages (e.g. seashells by type with `nameKey`, a layout grouped by type),
       or take the decision with the ore crystals (item 4). The temporary switch "Tabs: …" stays until then, at the bottom.
  - **Not done in A4:** the check in `uk`, at 1024×768, the search, the pencil (Escape), a full album, a refresh from a chest
    with the window open, the glDebugMode run (no OpenGL error in the last log, 08.11.2026 21:38–21:48; set `glDebugMode` back to
    false in `VintageStory-DevData\clientsettings.json` when done); `docs/testing/functional.md` A16 and W5 say "once item 4
    brings them" (crystals do not exist yet); the plan's "Decisions" (the header chosen; the text of the 08.11 line about the
    temporary switch of A and C), `AroundThePage` (the row of the tab switch stays until the tabs are decided).
  - **In game to try the seashells and ingots** (the test mod is copied to `VintageStory-DevData\Mods\cccollections`; remove it
    afterwards): `/giveitem curiocatalogue:book-album-normal-darkgreen 1 {curiocatalogue:{v:2,sheets:8,album:{id:"ccshells"}}}`
    (id `ingots` for the ingots).
- 08.11.2026, late evening (opus): items 1–5 of the list above done in code, not yet seen in game (uncommitted, builds, all
  tests green). Decided by the author: **the page holds three of the narrowest groups**, a group at least 4 cells (the page
  3 × 4 cells + 2 gaps of 20 = 652, no empty strip on the right; a name has ~144 of room instead of ~93); **pictures the game
  names alike keep the short name** ("Schematic"), the picture's code only in the hover text ("Schematic (schematic-a)");
  **the page buttons in the row of controls**, centred in the room between the search and the want list (their own row only
  when that room is short); **↺ with the tail of the arc shorter** (it ends at −150°, from four drawn variants). Also: text
  drawn at whole pixels (the cause of item 1: text textures are not smoothed, `docs/modding.md`, GUI); one left edge (the
  page has no room round it when no group has a frame; the name in the bottom row without its indent); the page element's
  bounds include the room round the groups (the hover missed the last 12 on the right and at the bottom). Next: the author
  looks at tapestries and seashells in game (`en`); then items 6 and 7 (a test collection with tabs, the tab variant), and the
  rest of "Not done in A4" above.
- 08.11.2026, night (opus): in game (tapestries, seashells; en and uk; 1024×768): the text, the page width, the names, the pager
  in the row, the left edge, ↺, the rename (Enter, Escape, a cut name), the search, the want list's sheets, uk and 1024×768 all
  pass. Fixed and passed in game: the icon buttons (◀ ▶, pencil, ↺) are `IconTextButton`, the game's text button with an icon,
  so they look as "Make a want list"; a rename shows the typed name at once (the stored one again after 2 s if the server never
  changes it); hover texts size to their text, the sheets' one above the slot's count. A14 rewritten: an open window takes the
  mouse, so the refresh is checked by the want list's sheets. The test mod has `ccores` (the game's graded ores; levels "Ores"
  and "Deposits", layouts by ore and by rock, tables grade × rock) for the tabs, and a compatibility test of it. Decided by the
  author: **one tab, one page** (the other variant surprises); **◀ ▶ turn the pages of the tab**, "N / M" counts them;
  **tabs that do not fit the window's height become a dropdown** in the row of controls ("Ore: Native copper ▼", ✓ by complete
  groups). All committed but these three. Next session (A4 still): remove `TabPaging.Shared` and the row of temporary switches
  (the page gets its 40 back, `AroundThePage`), the pager within a tab, the dropdown; then the author looks at `ccores` in game;
  the window stays 1600×900 and `glDebugMode` stays on (the author's choice, 09.11.2026); write
  "Decisions" for A4 (the header C, the tabs, the dropdown); remove `VintageStory-DevData\Mods\cccollections` at the end. Then A5.
- 09.11.2026 (opus): done in code (final as code, builds, all tests green; not yet seen in game): `TabPaging.Shared` and the row
  of temporary switches removed (the page got its 40 back), a tab's pages of its own with the pager within the tab
  (`AlbumPages.PagerAt`, `TabAt`; two unit tests, mutation-checked), the dropdown for tabs taller than the window; Decisions,
  "Checked in game (A4)"; case A22 in `docs/testing/functional.md`. Next: the author looks at `ccores` in game (A22, en and uk),
  then removes `VintageStory-DevData\Mods\cccollections`; A4 is ticked when that passes. Then A5.
  First look (the author, 00:11–00:13): both layouts showed the dropdown; now tabs are measured against the screen and the level
  stands leftmost. The client crashed at 00:13:01 in the game's item tooltip (`GuiElementItemstackInfo.AsyncRecompose`,
  `GL_INVALID_VALUE` in `LoadOrUpdateCairoTexture`, fatal with `glDebugMode` on), hovering the album window; cause not found,
  to be repeated with note of what was under the mouse.
  Then (the author, 09.11.2026): all of A22 passes in game. **A4 done**; the test mod's copy is removed from
  `VintageStory-DevData\Mods`. The crash did not come back; if it does, a line for `ROADMAP.md`. Next: A5 (sonnet, After: A4).
