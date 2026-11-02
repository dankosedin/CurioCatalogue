# Taking screenshots

How we shoot the mod's screenshots (ModDB gallery, READMEs) so that a shot can be taken again, in `en` and `uk`, with the same
framing. Facts checked in engine 1.22.7 (decompiled) and in game on 01.11.2026 (superflat creative world, `uk` → `en`).
The studio (a WorldEdit schematic) and the macros live in `tools/studio/`; the recipe to rebuild the studio and the shot
list come here with step P2 of the plan `docs/plans/2026-11-01-mod-page.md`.

**Update when** the game surprises you while shooting.

## Frame

- 16:9, 1920×1080. No sky in the frame: clouds differ after a client restart, so `en` and `uk` frames would differ
  (if some gets in, the difference is ignored).
- Only screenshots: no video, no shader or texture packs (the player sees what the page shows).
- Keep the GUI scale at 100 %.

## Studio

- The scene is kept as a WorldEdit schematic (`/we export`, JSON) in `tools/studio/`, not as a world file: a `.vcdbs` is a
  ~9 MB SQLite database that every save rewrites whole.
- To rebuild: a superflat creative world, `/we import` at the fixed point written here (the macros' `.cam` coordinates are
  absolute), then time and weather once (below). The recipe with the exact commands comes with P2.
- Not yet checked: whether the schematic carries the chest's contents and the books' attributes.

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
