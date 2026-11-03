# Taking screenshots

How we shoot the mod's screenshots (ModDB gallery, READMEs) so that a shot can be taken again, in `en` and `uk`, with the same
framing. Facts checked in engine 1.22.7 (decompiled) and in game on 01.11.2026 (superflat creative world, `uk` → `en`).
The studio (a WorldEdit schematic) and the macros live in `tools/studio/`; the plan behind them is
`docs/plans/2026-11-01-mod-page.md`.

**Update when** the game surprises you while shooting.

## Frame

- 16:9, 1920×1080. No sky in the frame: clouds differ after a client restart, so `en` and `uk` frames would differ
  (if some gets in, the difference is ignored).
- Only screenshots: no video, no shader or texture packs (the player sees what the page shows).
- Keep the GUI scale at 100 %.

## Studio

- The scene is kept as a WorldEdit schematic (`/we export`, JSON) in `tools/studio/`, not as a world file: a `.vcdbs` is a
  ~9 MB SQLite database that every save rewrites whole.
- The schematic `tools/studio/studio.json` (exported 03.11.2026, 465 blocks, 21×8×24): a raised floor of walnut planks, a wall
  of claystone bricks with leaded windows, tapestries with plank frames where a piece is missing, a chest between two
  cabinets, a display case, a mannequin, a bookshelf; beside the floor, a stack of four trunks. Only vanilla blocks and
  ours: no other mod is needed to rebuild it.
- It carries the block entities: the chest's contents, the books with their data (checked in game 03.11.2026 on a test
  schematic), the cabinets', the display case's and the mannequin's contents (in the file). A book's entries keep the
  right items only when the world has the same mods as the one it was exported from (`docs/testing/commands.md`,
  WorldEdit). Rebuilt in a new world by the recipe below and compared, 03.11.2026: all in place.
- The tapestry wall: complete pictures and pictures with a piece missing, a plank frame in the missing piece's place
  (`seraphim` without its left third, `return` complete, …). For the album shots the album has taken every piece on the
  wall, so its "Missing" grid shows the frames' pieces among the rest.
- WorldEdit commands and their traps (the exclusive end, the import point): `docs/testing/commands.md`.

### Rebuilding the studio

1. With the client closed, copy `tools/studio/studio.json` into `VintageStory-DevData\WorldEdit\` and
   `tools/studio/macros/10-studio-rebuild.json` into `VintageStory-DevData\Macros\`.
2. Create a world: creative, superflat, with only this mod (the `Mods` folder of the data folder empty).
3. In game, Alt+0 (the macro "Studio: rebuild in a new world") runs:
   ```
   /we mark =512010 =3 =512010 =512010 =3 =512010
   /we import studio
   /we cs
   /time set lunch
   /time stop
   /weather setprecip -1
   /weather acp
   ```
   The `=` makes a coordinate absolute; both marks on one block put the import point there (the import goes to the
   selection's minimum corner). Y 3 is the first block above the superflat ground. The studio takes X 512010…512030 and
   Z 512010…512033, clear of the spawn at 512000, 512000. The macros' `.cam` points are taken in this rebuilt world.
   Time and weather are stored in the world, so the macro is run once per new world (`/time set` moves to the next day
   each time).
4. Macros run only by their key (the editor has no "run" button). A key taken by another hotkey is skipped with a warning
   in `client-main.log`: change it in the editor (Ctrl+M).

### Changing the studio

Change it in the rebuilt world, then export it again from the same import point:

```
/we mark =512010 =3 =512010 =512031 =11 =512034
/we export studio
```

The end mark is one block past the studio on each axis (the end is exclusive); grow it if the studio grows. Copy the file
back into `tools/studio/` and check that the answer counts the blocks you expect.

## Props

The player's inventory is not in the schematic, so everything the shots hold lives in the studio's **props trunks** (the
stack of four beside the floor) and goes into the schematic with them. A rebuilt world has the props ready; each language
starts from a rebuilt world, so a shot that changes a prop (taking new, filling a list) finds it fresh.

- **One cover for every book:** `darkgreen`, which exists both normal and aged (teal and purple have no aged variant);
  the icons of P4 use the same cover. The vanilla book of `first-ledger` is `game:book-normal-darkgreen`, so the
  ledger it makes has that cover too.
- A prop that a shot changes has its own copy (the album of `album-chest`), so the shots can be taken in any order.

| Trunk (bottom = 1) | Props |
|---|---|
| 1 | the album (`title`, `album-window`); a copy of it (`album-chest`) |
| 2 | the want list; the ledger and its intake list (`ledger-intake`); the bill of materials (`materials`) |
| 3 | the first ledger's ingredients: `game:book-normal-darkgreen`, nails and strips, ink and quill (`first-ledger`) |

The album has taken every piece on the tapestry wall, so its "Missing" grid has the frames' pieces among the rest.
The studio's **chest** holds duplicates of pieces on the wall, three or four pieces the album lacks (what `album-chest`
takes) and nothing else. The ledger's intake list is made from a **handbasket** on the cabinet with items that are not tapestries
(planks and resin in the draft), so `ledger-intake` shows a book of another kind than the album.

## Shot list

Each shot in `en` and `uk`, as `<shot>.<lang>.<ext>` (P3 of the plan). A shot's macro (Alt+1 … Alt+7, the files
`tools/studio/macros/11-shot-<shot>.json` … `17-…`) sets the resolution, hides the HUD parts and moves the camera to the
shot's point; the window, the hands and the gesture are done by hand, as the table says.

| Key | Shot | In the hands | By hand after the macro | In the frame |
|---|---|---|---|---|
| Alt+1 | `title` | the album | right click into the air | the album's window on the left (its position from `tools/studio/dialog-positions.json`), the tapestry wall on the right: the frames on the wall are pieces in its "Missing" grid; one corner of plain brick for the slanted ribbon of two or three words (drawn by the script, P3) |
| Alt+2 | `album-window` | the album | right click into the air | the album's window in the middle: name, progress line, "Missing" |
| Alt+3 | `album-chest` | the copy of the album | Shift + right click on the chest, F12 within 15 s | looking at the chest, the Shift hint at the bottom; in the chat the message of the "take new" just done |
| Alt+4 | `wantlist` | the want list | right click into the air | the want list's window |
| Alt+5 | `ledger-intake` | the ledger's intake list, the ledger in the off-hand | right click into the air | the list's window (tapestries), the chest behind it |
| Alt+6 | `materials` | the bill of materials' intake list, the book in the off-hand | right click into the air | the list's window with counts (planks, resin), the basket behind it |
| Alt+7 | `first-ledger` | — | survival mode (the creative inventory has no grid); E; the ingredients into the 3×3 grid; F12 after the chat is gone (15 s: the game mode messages come after `.clearchat`) | the inventory, the first ledger's recipe in the grid and the ledger as the result |

Alt+0 is the rebuild, Alt+9 the point-finding macro. Ctrl+1 … Ctrl+4 are taken by the game (the bag slots of the
inventory). On the author's machine the macros that give tapestry pieces are on Ctrl+Shift+1 … Ctrl+Shift+9.

**Finding the points:** Alt+9 (`20-cam-point.json`) sets 1080p, adds a camera point where you stand (`.cam p`) and copies
all the points to the clipboard (`.cam save`). Start with `.cam clear`, then for each shot in the table's order: frame it,
Alt+9, F12 for a draft. The last clipboard text has every point, in the order of the shots; the shot macros are made from it.
The list lives for the game session and `.cam p` appends to it. A `.cam` command in survival mode fails and the clipboard
keeps the old text, so Alt+9 switches to creative first.
`.cam save` writes `1,` (a version) and six numbers per point: x, y, z ×100, pitch, yaw, roll ×1000. The shot macros load
the whole list and go to their own point, so the list in every macro is the same.

The shot macros end with `.clearchat`, so a frame has no chat unless the shot makes a message (`album-chest`).
`.cam` works only in creative mode: the macro of `first-ledger` switches to creative for the camera and back to survival.

## Window positions

Checked in vsapi `GuiElementDialogTitleBar.cs` (1.22.7): a window dragged by its title bar is remembered by its dialog name
in `clientsettings.json`, `dialogPositions`, and opens there again; "auto" in the ☰ menu of the title bar forgets it and
centres the window. A container's window is placed by its block, not by this list.

- Every shot keeps its windows on "auto" (centred), so `en` and `uk` frames match, except `title`: there the album's
  window is on the left and the tapestry wall on the right (a centred window covers the wall).
- All our books share one dialog name, `curiocatalogue-ledger` (the lists: `curiocatalogue-invoice`), so the position
  for `title` would move `album-window` and `materials` too. It is kept in `tools/studio/dialog-positions.json`; the
  script of P3 writes it into `clientsettings.json` (the client closed) before the `title` shots and removes it before
  the others, and `title` is shot last in each language. Until the script exists, do the same by hand.
- The inventory (`inventory-backpack`) opens on the right on "auto"; for `first-ledger` it is dragged to the centre, and
  that position is in the same file. The inventory is in no other shot, so it may stay.
- "auto" in the ☰ menu writes `null` for the window in `dialogPositions`.
- A window dragged by mistake: "auto" in its ☰ menu, or remove its entry from `dialogPositions` with the client closed.

## Camera points

- `.cam p` adds a point with position and angle. `.cam save` copies the points as text, `.cam load <text>` replaces the list,
  `.cam goto 0` restores position and angle exactly (frames match pixel for pixel).
- The coordinates are absolute: a saved text is valid only in the world it was made in.
- Creative or spectator mode only.
- The wiki's command pages are from 1.15; where they differ from the game, the code is right.

## Macros

- Ctrl+M: a name, a list of commands (both `.` and `/` forms) and a key. Macros survive a restart and a language switch.
- The editor wraps long lines and saves them wrapped, which breaks `.cam load …`. Write long commands into
  `VintageStory-DevData\Macros\<n>-<code>.json` while the editor is closed, then restart the client.
- The source of the macros is `tools/studio/macros/`; they are copied into `VintageStory-DevData\Macros` with the client
  closed (the script comes with P3).

## World, time, weather

- `/time stop`, `/weather setprecip -1`, `/weather acp` are stored in the world: set them once when building the studio.
- `/time set …` moves to the next day each time, so it is not in a macro either.

## Hiding the HUD

- F4 / `.gui false` hide the whole 2D layer, windows included, so they do not fit frames with a window.
- For such frames hide the parts: `.cf showMinimapHud false`, `.cf showCoordinateHud false`, `.cf showBlockInfoHud false`
  (they apply at once and survive a restart).
- The chat hides itself after 15 s without messages. The "Need help…" hint goes by holding K.
- The hotbar and the health bars stay. The crosshair hides while a window is open.
- Do not look at a block: its selection frame shows.

## Taking the shot

- `.resolution 1080p` gives exactly 1920×1080 in a window but does not survive a restart: put it into the macro.
- F12 saves to `Pictures\Vintagestory`. Ctrl+F12 doubles the size (too big).

## Size limits of ModDB

- An uploaded screenshot: at most 1920×1080 and 2 MB; the logo is one of them, 480×480 or 480×320.
- The first image of the gallery is the first screen of the page (shown ~800×516, ratio ~1.55; narrower images get grey bars).
- Measured: an empty field 1.6 MB as PNG, a field with an open album and clouds 1.79 MB; a full scene is likely over 2 MB,
  then JPEG (the decision is in the plan, step P3).

## Item icons

- `.exponepng hand 128` writes the held item's icon with a transparent background to
  `VintageStory-Dev\icons\block\<code>.png`.
- The file name has no variant of the cover, so the next export overwrites it: take the file at once.
- The books' textures (`textures/item/book-*.png`) are only the label on the cover, so book icons come from this export;
  the lists' textures are whole.
