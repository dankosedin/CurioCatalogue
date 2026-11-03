# Commands for testing

Chat and console commands that set up a test: items, rights, claims; and WorldEdit for the screenshot
studio. Syntax is as the game 1.22.7 registers it.

**Read** when preparing a test from `functional.md` or `upgrade-*.md`, or building the screenshot studio. **Update** when a
command we rely on changes, or when a test setup needs a command that is not here yet.

Chat commands start with `/`; in the server console the same command goes without the slash. Giving items needs the
privilege `gamemode` (a creative world on your own PC has it).

## Vanilla: items and blocks

`/giveitem <item code> [quantity] [target] [attributes]` and `/giveblock <block code> [quantity] [target] [attributes]`.
Checked in `VintagestoryLib.dll` (`CmdGive`, decompiled 31.10.2026): the target is an entity selector, and the attributes
come after it, so to give to yourself with attributes the target must be written: `s[]` is yourself. Without attributes the
target may be left out.

| Need | Command |
|---|---|
| A tapestry piece (not in the creative inventory) | `/giveblock tapestry-north 1 s[] {"type":"ambush1"}` |
| A book of one cover | `/giveitem curiocatalogue:book-ledger-normal-brickred` (code: `book-{kind}-{normal or aged}-{colour}`, kind is ledger, materials or album; colours in `book.json`, aged has no purple and teal) |

Piece names: see "Tapestry pieces" below. Other mods' items need their domain in the code (`domain:path`); vanilla's `game:`
may be left out. A give into a full inventory fails ("Failed, target players inventory is likely full…"); nothing is dropped.

## Ours: `/curio` (branch `dev-commands`, never in `main`)

`DevCommands.cs`, privilege `gamemode`. For a test session, merge or cherry-pick the branch into a scratch branch; never merge
it back into `main`. CI does not build it, so it may lag behind `main`: rebase it and fix what broke.

| Command | Does |
|---|---|
| `/curio sheets <n>` | sets the sheets (0 to `MaxSheets`) in the ledger or album held in the main hand |
| `/curio tapestries [count]` | gives random different tapestry pieces, one of each (default 10; above the total gives all) |
| `/curio tapestry <piece> [qty]` | gives a tapestry piece by name, e.g. `ambush1` (quantity 1-64) |
| `/curio jug [lime\|acid] [litres]` | a fired jug with lime water (restoring a cover) or sulfuric acid (ageing), 1-3 litres |
| `/curio books [ledger\|materials\|album] [normal\|aged\|all] [sheets]` | one book of every cover; `sheets` writes ledger data like a used book, `-1` leaves the book plain |
| `/curio missing` | gives one of each item the album or want list in the main hand lists |

## Vanilla: rights, players, claims

| Need | Command |
|---|---|
| Rights for a player | `/op <name>` in the server console |
| Let a player onto a whitelisted server | `/player <name> whitelist add` (the dedicated test server has `WhitelistMode: 0`) |
| Create a claim | `/land claim new`, `start`, `end`, `add`, `save` |
| Change a claim | `/land claim load <id>` (id from `/land list`), then `grant <name> use` or `revoke <name>`, then `/land claim save` |

The game's hint suggests `/land claim modify`; it does not exist.

## Locks

A padlock needs the block reinforced first (plumb and square, stones in the inventory). Our access checks cover both claims
and reinforcement (`docs/critical.md`), so test a locked chest and a claimed chest separately.

## Tapestry pieces

All 31 vanilla pictures, 88 pieces: the block attribute `sizes` in `blocktypes/cloth/tapestry.json` (width × height), the
piece names as `TapestryPieces` makes them. Order on the wall (`BlockTapestry.cs`, `GetWordedSection` and the `neighbours`
tables): 1×N from top to bottom, N×1 from left to right, 2×2 is `<picture><row><column>` (`11` top left, `12` top right,
`21` bottom left, `22` bottom right). For `gate` (4×1) the game names the pieces "top quarter" … "bottom quarter", but it
checks them as a row from left to right.

| Picture | Size | Pieces |
|---|---|---|
| ambush | 3×1 | `ambush1`, `ambush2`, `ambush3` |
| nightfall | 3×1 | `nightfall1`, `nightfall2`, `nightfall3` |
| holy | 1×2 | `holy1`, `holy2` |
| salvation | 2×2 | `salvation11`, `salvation12`, `salvation21`, `salvation22` |
| rot | 3×1 | `rot1`, `rot2`, `rot3` |
| themorning | 3×1 | `themorning1`, `themorning2`, `themorning3` |
| schematic-a | 2×2 | `schematic-a11`, `schematic-a12`, `schematic-a21`, `schematic-a22` |
| schematic-b | 2×2 | `schematic-b11`, `schematic-b12`, `schematic-b21`, `schematic-b22` |
| schematic-c | 1×2 | `schematic-c1`, `schematic-c2` |
| schematic-c-bloody | 1×2 | `schematic-c-bloody1`, `schematic-c-bloody2` |
| schematic-d | 2×2 | `schematic-d11`, `schematic-d12`, `schematic-d21`, `schematic-d22` |
| rotbeast | 2×2 | `rotbeast11`, `rotbeast12`, `rotbeast21`, `rotbeast22` |
| seraphim | 3×1 | `seraphim1`, `seraphim2`, `seraphim3` |
| blackguard | 2×2 | `blackguard11`, `blackguard12`, `blackguard21`, `blackguard22` |
| forlorn | 1×2 | `forlorn1`, `forlorn2` |
| gate | 4×1 | `gate1`, `gate2`, `gate3`, `gate4` |
| diet | 2×1 | `diet1`, `diet2` |
| oldworld | 1×3 | `oldworld1`, `oldworld2`, `oldworld3` |
| temporality | 1×2 | `temporality1`, `temporality2` |
| aid-arm | 1×2 | `aid-arm1`, `aid-arm2` |
| aid-head | 1×2 | `aid-head1`, `aid-head2` |
| aid-leg | 1×2 | `aid-leg1`, `aid-leg2` |
| contracts | 2×1 | `contracts1`, `contracts2` |
| eidolon | 1×2 | `eidolon1`, `eidolon2` |
| glider | 3×1 | `glider1`, `glider2`, `glider3` |
| notice-blackguard | 1×1 | `notice-blackguard` |
| return | 1×2 | `return1`, `return2` |
| rickbat | 1×2 | `rickbat1`, `rickbat2` |
| symptoms | 2×2 | `symptoms11`, `symptoms12`, `symptoms21`, `symptoms22` |
| tempstorm | 2×2 | `tempstorm11`, `tempstorm12`, `tempstorm21`, `tempstorm22` |
| translocator | 2×2 | `translocator11`, `translocator12`, `translocator21`, `translocator22` |

## WorldEdit (creative mode only)

Read in `VSCreativeMod.dll` (WorldEdit, decompiled 03.11.2026); the selection, export and import also checked in game
03.11.2026, the material replacement not yet. Used to keep the screenshot studio as a schematic (`docs/screenshots.md`).

| Need | Command |
|---|---|
| Mark the selection | `/we s` at one corner, `/we e` at the opposite one (each marks the player's position) |
| Grow it | `/we g up <n>` (also `down`, `north`, …; `l` for the look direction) |
| Remove the highlight | `/we cs` (`clear`): the selection goes, the blocks stay |
| Save the selection to a file | `/we export <name>`: `VintageStory-DevData\WorldEdit\<name>.json`; the answer counts the blocks |
| Paste a file | stand at the target, `/we s` and `/we e` on the same spot, then `/we import <name>` |
| Undo the last action | `/we undo` |
| Turn the tools off | `/we off` (otherwise placing a block with a tool selected runs the tool) |

- **The end is exclusive** (`BlockSchematic.AddArea`: `x < end`). Marks at floor level give zero height and `0 blocks`
  exported: grow the selection up, and put the far corner one block past the last block.
- **Import goes to the selection's minimum corner** (smallest X, Y, Z), not to the `/we s` mark: once both marks exist,
  WorldEdit recomputes the start as the minimum of the two. With one mark only it says "Please mark a start position".
- **Export crops to the blocks:** air is not stored, so the schematic starts at the minimum corner of its blocks, not of the
  selection.
- **Block entities go with it:** a chest's contents and our books with their data (a chest and a book on the ground,
  checked in game 03.11.2026 into a new world with the same mods). Caveat: the item ids inside a book's entries are not
  remapped (they live in a tree array, which `Collectible.OnLoadCollectibleMappings` in vsapi skips), so a book keeps the
  right items only when both worlds have the same mods.

### Replacing a material

**Plain blocks:** the paint brush in the mode "replace selected" (`PaintBrushTool`) replaces, inside the brush, every block
with the id of the block you click by the block in your hand:

```
/we t paintbrush
/we tm 3
/we ts 1
/we tsx 30
/we tsy 12
/we tsz 30
```

`tm 3` is the mode, `ts 1` a cuboid, `tsx`/`tsy`/`tsz` its size: larger than the area, but not reaching anything that must
stay. Then take the new block in hand and right-click a block of the old one, as if placing it. Each variant (another
pattern of stone, a turned plank) is another id and needs its own click; the new blocks are placed as the held block, so
turned variants come out all turned one way. `/we off` when done.

**Chiseled blocks:** `/we repmat` (`replace-material`) changes one material inside every block of the selection that can
exchange it (chiseled blocks, `BEMicroBlock.ExchangeWith`; also support beams and rock rubble), and does nothing to plain
blocks. Mark the selection, put the old material into the first hotbar slot, hold the new one in another hotbar slot,
then `/we repmat`; the answer counts the blocks changed.
