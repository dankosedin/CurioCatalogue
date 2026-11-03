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

## Shot list

Each shot in `en` and `uk`, as `<shot>.<lang>.<ext>` (P3 of the plan). The macro of each shot (camera point, resolution,
HUD) comes with its test frame.

| Shot | In the hands | In the frame |
|---|---|---|
| `title` | the album | the album's window moved aside, the tapestry wall beside it: the frames on the wall are pieces in its "Missing" grid; a third of the frame free for the title text (drawn by the script, P3) |
| `album-window` | the album | the album's window in the middle: name, progress line, "Missing" |
| `album-chest` | the album | looking at the chest, the Shift hint at the bottom; in the chat the message of a "take new" just done (shoot within 15 s) |
| `wantlist` | a want list | the want list's window |
| `ledger-intake` | an intake list, the ledger in the off-hand | the list's window, the chest behind it |
| `materials` | a bill of materials | its window with counts, sorted by count |
| `first-ledger` | — | the inventory, the first ledger's recipe in the 3×3 grid (vanilla book, nails and strips, ink and quill) and the ledger as the result |

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
