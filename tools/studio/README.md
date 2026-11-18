# Shooting the screenshots

The steps to take the mod page's 14 screenshots in one language, for the author or a translator. Why each step is
as it is: `docs/screenshots.md`. The files here: the studio (`studio.json`, a WorldEdit schematic), the macros of the
shots (`macros/`), the positions of the windows (`dialog-positions.json`).

**Needs:** a copy of Vintage Story 1.22.7 with its own data folder (`docs/onboarding.md`: `VINTAGE_STORY_DATA`), only
Curio Catalogue in its `Mods` folder, and PowerShell in the repo root. One run takes about 30 minutes.

## 1. Before the game

The game closed:

```
.\tools\screenshots.ps1 -Action Prepare -Lang uk
```

(`en` or `uk`.) It removes the shot macros of an older list, copies the macros and the studio into the data folder and
sets the window positions and the game's language.

If PowerShell says "running scripts is disabled on this system", allow scripts for this window only (nothing changes on the
machine): `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`, then run the command again.

## 2. The studio

1. Start the game. Create a new world: **creative**, world type **superflat**.
2. In the world, **Alt+0**: the studio appears, the time and the weather are set. Look around: walls, tapestries, a chest
   between two cabinets, a basket on the left cabinet, trunks beside the floor.
3. From the trunks (right click) take into the inventory (the props are listed in `docs/screenshots.md`, "Props"):
   the **tapestries, ore crystals, seashells and butterflies albums** (about a fifth full), the **empty ore crystals
   album**, the **want list**, the **ledger**, the **bill of materials**, and the first ledger's ingredients (a green
   **book**, two **nails and strips**, **ink and quill**). The books, the lists and the ingredients are in one trunk; the
   ore crystals, seashells and butterflies albums each lie in a trunk of their own, among spare items of their collection.

## 3. The 14 shots

Each shot: press its macro key if the row says so (it moves the camera; **do not touch the mouse** after it), do what the
row says, press **F12** once. Take them **in this order** and press F12 nowhere else: the script takes the 14 newest
frames in this order. A frame came out wrong: delete it at once from `Pictures\Vintagestory` and take it again (but a shot
that changed a prop, the `album-chest` ones, is retaken from a rebuilt world). A frame found wrong after the run: take it
alone in the same world, then the agent puts it in place by hand (`Collect` takes 14). The world is in survival after
`first-ledger`, and the camera keys do nothing there: type `/gamemode creative` first.

Keys: **Alt+1** goes to the common camera point, **Alt+2** to the chest's, **Alt+3** is the first ledger's. After
Alt+1 you stay at the point for the shots that follow until the row says another key.

| # | Shot | Key | Before | Then | F12 when |
|---|---|---|---|---|---|
| 1 | `title` | **Alt+1** | the tapestries album in the hand | right click into the air: the album opens on the left | at once. Then in the album window's ☰ menu (top right) choose **auto**, close it (Esc) |
| 2 | `collection-tapestries` | | the tapestries album | right click into the air | at once; Esc |
| 3 | `collection-crystals` | | the ore crystals album | right click; choose the level **Every crystal**, turn to the page of native copper | at once; Esc |
| 4 | `collection-seashells` | | the seashells album | right click; the level **Every type in every colour** | at once; Esc |
| 5 | `collection-butterflies` | | the butterflies album | right click; the layout **By rarity**, the tab **Very rare** (Legendary is empty) | at once; Esc |
| 6 | `album-chest-1` | | the tapestries album | right click into the air: the page with the gaps the chest will fill | at once; Esc |
| 7 | `album-chest-2` | **Alt+2** | the tapestries album | Shift + right click (the camera looks at the chest) | the chat shows "Taken for the collection…" (within 15 s) |
| 8 | `album-chest-3` | **Alt+1** | the tapestries album | right click into the air: the same page, the new pieces in place (keep the taken pieces in the hotbar, as in shot 7) | at once; Esc. Then put the six taken pieces back into the chest: the ledger's list of shot 12 has 7 lines (8 pieces), as in `en` |
| 9 | `album-level` | | the empty ore crystals album | right click into the air: the choice of a level | at once; Esc |
| 10 | `wantlist` | | the want list of ore crystals (level *The richest of each ore*) | right click into the air; do not hover a slot (its crystals change in turn, so a tooltip would differ between languages) | at once; Esc |
| 11 | `butterflies-family` | | the butterflies album | right click; the layout **By family** | at once; Esc |
| 12 | `ledger-intake` | **Alt+1** after the list is in the hand | the ledger in the off-hand (F), the hand empty; aim at the **chest between the cabinets** (not the basket on the left cabinet) and Shift + right click: an intake list of tapestry pieces appears. Take it into the hand | right click into the air | at once; Esc |
| 13 | `materials` | **Alt+1** after the list is in the hand | the bill of materials in the off-hand, the hand empty; aim at the basket and Shift + right click: an intake list appears. Take it into the hand | right click into the air | at once; Esc |
| 14 | `first-ledger` | **Alt+3** | the ingredients in the inventory | the game switches to survival. **E**: the inventory opens in the middle. Into the 3×3 grid: the book in the centre, nails and strips above and below it, ink and quill on its right; the ledger appears as the result | wait until the chat fades (15 s) |

Close the game.

## 4. After the game

```
.\tools\screenshots.ps1 -Action Collect -Lang uk
```

The ribbon's text on the first shot is read from `tools/studio/ribbons.json` (`{"en": "...", "uk": "..."}`); a translator adds a line for
their language there before shooting. `-Ribbon "<text>"` overrides it for one run.

It writes `docs/images/<shot>.<lang>.png` (`.jpg` for `title` and the four `collection-*` shots; `title` gets the ribbon) and
prints which frame went to which shot. **Open the 14 files and look:** each must show what its row above says. Then:

```
.\tools\screenshots.ps1 -Action Check
```

It lists the shots still missing (the other language) and files over 2 MB.
