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
- [ ] **A3. The view built.** The album window by A2, on the collection resolver of the core (item 3). The ledger and the bill
      of materials keep today's window: the album gets a window of its own, or `LedgerDialog` a mode (A2 decides, with
      `docs/architecture.md` if a class is added). The decisions of the view (which page, what is shown where) pulled into
      testable code where they can be, with unit tests (`ROADMAP.md`, Later, tests for the client code). Both header
      variants (A and C) and both tab variants (several groups a page, or one tab one page; Decisions) built behind
      temporary switches, for the author to compare in A4.
      Branch: `feat/album-view`. After: A2, the core's plan done. Model: opus. Author: none.
      Done when: the build and all test projects pass; the author has the exact steps of the in-game check.
- [ ] **A4. Checked in game, docs.** The author checks the window in game (en and uk, memory: in-game checks by language):
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

## Where we stopped

- 04.11.2026: A1 (three more GUI questions) and A2 (dimensions, levels, members of several codes, rarity marks, flat and
  disabled collections) updated by C1–C6 (Decisions). Next: A1 (opus, After: the plan's own commit).
- 05.11.2026: A1 done (Decisions, "What the game's GUI can do"; the probe removed). Next: A2 (opus, After: A1), on the same
  branch `docs/album-view`.
- 07.11.2026: A2 done (final: Decisions, "The view on paper (A2)", with the mockups and the list for C7; `docs/domain.md`,
  Windows). The branch `docs/album-view` waits for the author's pull request into `main`; then C7 of the collections plan.
  Next here: A3 (opus, After: A2 and the core's plan done).
