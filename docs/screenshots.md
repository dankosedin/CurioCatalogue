# Taking screenshots

How we shoot the mod's screenshots (ModDB gallery, the description) so that a shot can be taken again, in `en` and `uk`, with the same
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
- The schematic `tools/studio/studio.json` (exported again 19.11.2026 with the props of 0.2, 472 blocks, 21×8×24): a raised floor of walnut planks, a wall
  of claystone bricks with leaded windows, tapestries with plank frames where a piece is missing, a chest between two
  cabinets, a display case, a mannequin, a bookshelf; beside the floor, the props trunks. Only vanilla blocks and
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

The player's inventory is not in the schematic, so everything the shots hold lives in the studio's **props trunks** (beside
the floor) and goes into the schematic with them. A rebuilt world has the props ready; each language
starts from a rebuilt world, so a shot that changes a prop (taking new, filling a list) finds it fresh.

- **One cover for every book:** `darkgreen`, which exists both normal and aged (teal and purple have no aged variant);
  the icons of P4 use the same cover. The vanilla book of `first-ledger` is `game:book-normal-darkgreen`, so the
  ledger it makes has that cover too.
- In the schematic (exported 19.11.2026, 00:55), as a rebuilt world shows them (checked in game 19.11.2026):
  - **Tapestries album**, named "Tapestries": 18 of 88 (the wall's pieces and four whole pictures: Ambush, The Morning,
    Salvation, Diet of Kings); `album-chest` makes it 24.
  - **Ore crystals album**, level *Every crystal*: 70 of 349, native copper 6 of 31 (the page of `collection-crystals`).
  - **Seashells album**, level *Every type in every colour*: 8 of 42, two of them rare colours (seafoam, turquoise).
  - **Butterflies album**: 34 of 169 (every fifth species of the definition's list).
  - An **empty ore crystals album** (no level chosen) for `album-level`, and a **want list** of ore crystals at *The
    richest of each ore* (16 lines) for `wantlist`.
  - The **ledger**, the **bill of materials** (empty) and the first ledger's ingredients.
  - The books, the lists and the ingredients share one trunk; the other three albums each lie in a trunk of their own,
    with spare items of their collection (what the album took went into the inventory, and back into the trunk).
    Other trunks hold spare tapestry pieces.
- How the props were made (19.11.2026, for the next version's fuller albums): the items were given by macros of
  `/giveitem` and `/giveblock` (crystals only of the grades and rocks the game's deposits generate, read from
  `worldgen/deposits/metalore/*.json`), put into a trunk outside the studio and taken by Shift + right click; the albums
  were crafted from ledgers of the first-ledger recipe (a given ledger has 0 sheets, so it makes no want list).
- The shots are taken in the order of the list, so a shot that changes a prop (`album-chest` adds pieces to the album)
  comes after the shots that need it unchanged. The intake lists of `ledger-intake` and `materials` are made during the
  run (`tools/studio/README.md`).

The studio's **chest** holds duplicates of pieces on the wall and six pieces the album lacks (what `album-chest` takes);
the ledger's intake list (`ledger-intake`) is made from it. The **handbasket** on the cabinet holds planks and resin: the
bill of materials' intake list (`materials`) is made from it. The bill of materials itself is empty (nothing posted into
it yet), so `materials` shows its intake list, not its window.

## Shot list

The shot list of 0.2 (decided 13.11.2026, G5 of `docs/plans/2026-11-04-mod-description.md`). Each shot in `en` and `uk`, as
`<shot>.<lang>.<ext>` in `docs/images/`. The column "Gallery" is the order of the ModDB gallery (the `en` files only);
`docs/releasing.md` follows it.

- **One camera point** for every shot but `album-chest-2`: the background is the same in all of them, so what changes (the
  window, the page) draws the eye by itself, and a carousel turns without the scene jumping (the frames of G4 were shot so).
  `album-chest-2` looks at the chest, so it has a point of its own.
- **The albums are as full as the mod's version:** in 0.2 about 20 % of each collection collected, in 1.0 full (the author's
  Easter egg, 13.11.2026: the fuller the mod, the fuller the albums). The progress line reads about a fifth: tapestries ~18 of
  88, seashells ~8 of 42, butterflies ~34 of 169; the crystals' page in the frame (native copper) about a fifth full.
- **Windows centred ("auto")**, except `title`: there the album's window is on the left (`tools/studio/dialog-positions.json`)
  and the tapestry wall on the right, its plank frames being gaps on the album's page.

| Shot | Gallery | Where | In the hands, by hand | In the frame |
|---|---|---|---|---|
| `title` (JPEG, the ribbon) | 1 | the hero of the showcase and README | the tapestries album; right click into the air | the album's window on the left on a page of the tapestries, the wall on the right; one corner of plain brick for the slanted ribbon (drawn by the script) |
| `collection-tapestries` (JPEG) | 2 | the showcase's carousel | the tapestries album; right click into the air | page 1 |
| `collection-crystals` (JPEG) | 3 | the carousel | the ore crystals album | level "Every crystal", the page of native copper |
| `collection-seashells` (JPEG) | 4 | the carousel | the seashells album | level "Every type in every colour" |
| `collection-butterflies` (JPEG) | 5 | the carousel; the guide, second of the butterflies' pair | the butterflies album | "By rarity" |
| `album-chest-1` | — | the guide, Gestures: first of three | the tapestries album; right click into the air | the page with the gaps the chest will fill |
| `album-chest-2` | 6 | the guide, Gestures: second | the album; Shift + right click on the chest, F12 within 15 s | looking at the chest, the Shift hint at the bottom; in the chat the message of the "take new" just done |
| `album-chest-3` | — | the guide, Gestures: third | the album; right click into the air | the same page, the new pieces in place |
| `album-level` | — | the guide, Collections | an empty ore crystals album; right click into the air | the window with the choice of a level |
| `wantlist` | 7 | the guide, Collections | a want list of ore crystals at "The richest of each ore" (its lines are "any rock"; "Each ore in each rock" names one rock in every line); right click into the air | the want list's window, its slots showing the crystals of each line in turn (no tooltip) |
| `butterflies-family` | — | the guide, Collections: first of the butterflies' pair | the butterflies album | "By family" |
| `ledger-intake` | 8 | the guide, Gestures | the ledger's intake list, the ledger in the off-hand; right click into the air | the list's window (tapestries) |
| `materials` | 9 | the guide | the bill of materials' intake list, the book in the off-hand; right click into the air | the list's window with counts (planks, resin) |
| `first-ledger` | 10 | the guide, Getting started | survival mode (the creative inventory has no grid); E; the ingredients into the 3×3 grid; F12 after the chat is gone (15 s: the game mode messages come after `.clearchat`) | the inventory, the first ledger's recipe in the grid and the ledger as the result |

`album-window` of 0.1 is gone: the hero is `title`, the album's window is shown by the carousel. The props, the macros and
the script for this list are made (R3a of `docs/plans/2026-11-04-release-0.2.md`), the camera points taken in the
studio of 0.2 (19.11.2026).

**Macros by camera, not by shot** (decided 18.11.2026): the shots share one camera point, so three macros do for 14 shots,
and the book in the hand says which shot it is. **Alt+1** the common point (`11-shot-point.json`: `title`, `collection-*`,
`album-chest-1/3`, `album-level`, `wantlist`, `butterflies-family`, `ledger-intake`, `materials`), **Alt+2** the chest's
point (`12-shot-chest.json`: `album-chest-2`), **Alt+3** the first ledger's (`13-shot-first-ledger.json`: the common
point, in survival). The list of points in every macro is the same: 0 the common point, 1 the chest's.

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
- The album's window has its own dialog name, `curiocatalogue-album` (since 0.2; the ledger and the bill of materials:
  `curiocatalogue-ledger`, the lists: `curiocatalogue-invoice`), so the position for `title` moves every album shot. It
  is kept in `tools/studio/dialog-positions.json` (78, 52: the album's window is 975 px high at GUI scale 1.0, so its
  top stays where "auto" puts it) and written by `tools/screenshots.ps1 -Action Prepare`; `title` is shot first, then
  "auto" in the album window's ☰ menu puts it back in the centre for the rest of the run.
- The stored X and Y are the window's top left corner in pixels divided by the GUI scale (vsapi
  `GuiElementDialogTitleBar.cs`, 1.22.7).
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
  closed by `tools/screenshots.ps1 -Action Prepare`.

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

## The script

`tools/screenshots.ps1` (PowerShell 5.1, `System.Drawing`; the header of the file has the options). The steps for a
person are in `tools/studio/README.md`; here, why the script works as it does:

- **One run per language, three commands:** `Prepare` before the game (macros, the studio, the window positions, the
  client's language; it removes the `*-shot-*.json` macros of an older list from the data folder, or their keys would
  fight the new ones), the 14 shots in game, `Collect` after it. Rejected (03.11.2026): a `Collect` after every frame
  and closing the client before `title` to write its window position (the author went back and forth between the game
  and the terminal).
- **`Collect` takes the 14 newest frames** of `Pictures\Vintagestory` in the order of the list, so the run takes
  exactly 14 frames in that order; a frame retaken at once keeps the order. It checks all 14 before writing any.
- **Both window positions are written before the game:** `title` comes first, then the album's window goes back to
  "auto" in its ☰ menu, and the inventory (`first-ledger`) is in no other shot.
- **The language is written into `clientsettings.json`** (`stringSettings.language`), so the client starts in it;
  switching in game restarts the client. To check on the first `uk` run.
- `-Action Check` lists missing shots and files over the limit.

**Format and ribbon (03.11.2026, widened 18.11.2026):** `title` is a JPEG (0.5 MB; as PNG with the ribbon it is 2.1 MB, over
the limit), and so are the four `collection-*` shots of the carousel (a full album window is near the limit as PNG); the
other shots are PNG (1.4–1.9 MB). `Check` reports a file over 2 MB: that shot goes into `jpegShots` in the script. The ribbon (text from `tools/studio/ribbons.json`, decided in P5): `-Corner BottomRight -Angle 25`, font Georgia; the slope follows the floor boards.
