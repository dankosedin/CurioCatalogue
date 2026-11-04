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
- [ ] **A2. The view on paper.** The input is the collections plan's Decisions, C1–C5 (final, 04.11.2026): collections have
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
  goes into the upgrade table (`docs/testing/upgrade-0.1-to-0.2.md`). One known candidate: remembering the code that filled
  a member filled by any of several codes (A2 decides whether the view shows it).
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

## Where we stopped

- 04.11.2026: A1 (three more GUI questions) and A2 (dimensions, levels, members of several codes, rarity marks, flat and
  disabled collections) updated by C1–C6 (Decisions). Next: A1 (opus, After: the plan's own commit).
- 05.11.2026: A1 done (Decisions, "What the game's GUI can do"; the probe removed). Next: A2 (opus, After: A1), on the same
  branch `docs/album-view`.
