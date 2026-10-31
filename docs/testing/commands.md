# Commands for testing

Chat and console commands that set up a test: items, rights, claims. Syntax is as the game 1.22.7 registers it.

**Read** when preparing a test from `functional.md` or `upgrade-*.md`. **Update** when a command we rely on changes, or when a
test setup needs a command that is not here yet.

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

Piece names: `ambush1..3`, `salvation11, 12, 21, 22`, … (`docs/modding.md`, Tapestries). Other mods' items need their domain
in the code (`domain:path`); vanilla's `game:` may be left out.

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
