# Shooting the screenshots

The steps to take the mod page's seven screenshots in one language, for the author or a translator. Why each step is
as it is: `docs/screenshots.md`. The files here: the studio (`studio.json`, a WorldEdit schematic), the macros of the
shots (`macros/`), the positions of the windows (`dialog-positions.json`).

**Needs:** a copy of Vintage Story 1.22.7 with its own data folder (`docs/onboarding.md`: `VINTAGE_STORY_DATA`), only
Curio Catalogue in its `Mods` folder, and PowerShell in the repo root. One run takes about 15 minutes.

## 1. Before the game

The game closed:

```
.\tools\screenshots.ps1 -Action Prepare -Lang uk
```

(`en` or `uk`.) It copies the macros and the studio into the data folder and sets the window positions and the game's
language.

If PowerShell says "running scripts is disabled on this system", allow scripts for this window only (nothing changes on the
machine): `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`, then run the command again.

## 2. The studio

1. Start the game. Create a new world: **creative**, world type **superflat**.
2. In the world, **Alt+0**: the studio appears, the time and the weather are set. Look around: walls, tapestries, a chest
   between two cabinets, a basket on the left cabinet, trunks beside the floor.
3. From the trunks (right click) take into the inventory: the **album**, the **ledger**, the **bill of materials**, the
   **want list**, and the first ledger's ingredients (a green **book**, two **nails and strips**, **ink and quill**).

## 3. The seven shots

Each shot: press its macro key (it moves the camera; **do not touch the mouse** after it), do what the row says, press
**F12** once. Take them in this order and press F12 nowhere else: the script takes the seven newest frames in this
order. A frame came out wrong: delete it at once from `Pictures\Vintagestory` and take it again.

| # | Before the key | Key | Then | F12 when |
|---|---|---|---|---|
| 1 | the album in the hand | **Alt+1** | right click into the air: the album opens on the left | at once. Then in the album window's ☰ menu (top right) choose **auto**, close it (Esc) |
| 2 | the album in the hand | **Alt+2** | right click into the air: the album opens in the middle | at once; Esc |
| 3 | the album in the hand | **Alt+3** | Shift + right click (the camera looks at the chest) | the chat shows "Taken for the collection…" (within 15 s) |
| 4 | the want list in the hand | **Alt+4** | right click into the air | at once; Esc |
| 5 | the ledger in the off-hand (F), the hand empty; aim at the chest and Shift + right click: an intake list appears. Take it into the hand | **Alt+5** | right click into the air | at once; Esc |
| 6 | the bill of materials in the off-hand, the hand empty; aim at the basket and Shift + right click: an intake list appears. Take it into the hand | **Alt+6** | right click into the air | at once; Esc |
| 7 | the ingredients in the inventory | **Alt+7** | the game switches to survival. **E**: the inventory opens in the middle. Into the 3×3 grid: the book in the centre, nails and strips above and below it, ink and quill on its right; the ledger appears as the result | wait until the chat fades (15 s) |

Close the game.

## 4. After the game

```
.\tools\screenshots.ps1 -Action Collect -Lang uk -Ribbon "<text>"
```

The ribbon's text on the first shot, for now a placeholder until the page's texts are decided:

| Language | `-Ribbon` |
|---|---|
| en | `Possess them ALL!!!` |
| uk | `Заволодіти ВСІМА!!!` |

It writes `docs/images/<shot>.<lang>.png` (the first shot `title.<lang>.jpg`, with the ribbon) and prints which frame
went to which shot. **Open the seven files and look:** each must show what its row above says. Then:

```
.\tools\screenshots.ps1 -Action Check
```

It lists the shots still missing (the other language) and files over 2 MB.
