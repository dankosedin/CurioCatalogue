# Changelog

What changed in each version of Curio Catalogue. A version's section is its release notes on GitHub and on the ModDB page.

## 0.2.0 (2026-11-19)

Four collections instead of one, and an album that shows its whole collection, as a stamp album does. Albums of 0.1 are
converted when first read, with nothing lost.

### New

- **The album shows the whole collection, as a stamp album does.** Right click into the air opens it as pages: every picture with its pieces in place, the ones you have and the missing ones as dim silhouettes, the game's tooltip over each. A group has its tab (a dropdown when the tabs do not fit), the pages turn with buttons, a search lights the matches, and **Make a want list** and **Start over** are in the row of controls. Under the title: the album's name, changed by its pencil, a bar of how much is collected, and the sheets left. Before, the album had the ledger's window: a progress line and a grid of what was missing.
- **Ore crystals are the second collection.** An album made from an empty ledger or bill of materials and a crystal (the crystal stays) collects the ore crystals of the game at three levels: *The richest of each ore* (16), *Each ore in each rock* (117) or *Every crystal* (349), shown by ore or by rock.
- **Seashells are the third collection,** made the same way from a seashell: *Every type* (7) or *Every type in every colour* (42, one table of types by colours). Seafoam and turquoise, the game's rare colours, have a small golden corner and "Rare colour" in the tooltip.
- **Butterflies are the fourth collection,** made from a dead butterfly: all 169 of the game, in its own names, shown by family (seven tabs, the moths as one, a frame for each of the 109 species headed by its Latin name) or by rarity (six tabs by the game's spawn chance, from legendary to common).
- **Levels.** A new album of ore crystals or seashells asks for a level first and takes nothing until one is chosen; the first piece taken fixes it, and **Start over** frees it, with everything missing again.
- **Tapestries can be shown by size:** next to "By picture", "By size" has a tab for each size of picture, 1×1, 2×1, 1×2, 3×1, 1×3, 2×2 and 4×1, each with the pictures of that size. Nothing collected changes.
- **Icons instead of words:** a crystal table heads an ore by the game's nugget, a quality by a yellow round mark that grows and brightens with it, a rock by its stone; the seashells' table heads its colours by round swatches; the full name shows on hover. The level, the layout and the tabs show a sample from the game before each name, the icon alone when the row is short of room. A long list of tabs (17 for the crystals) opens over the page and scrolls; Escape closes it before the window.
- **What a collection lacks is named by what it is,** not by the first item that fits, in a want list, in the album's tooltip and in the want list window: "Ambush, left third" for a tapestry piece (pieces of one picture no longer share a name), "Native copper, bountiful, basalt" for a crystal. Butterflies keep the game's own names.
- **Want lists take any item that will do:** where several will ("Native copper, any rock", "Scallop, any colour"), the line says so, the cell and the want list window show those items in turn, and the want list (or an album taking new) takes the one there is room for. Want lists of 0.1 work as before.

### Changed and fixed

- **Albums remember what they collected,** piece by piece and with the item that filled it, instead of a list of what is missing. An album follows its collection as before (a new tapestry is missing, a piece excluded for a while comes back collected). An album of 0.1 is converted when it is first read, with nothing lost. Albums and want lists saved by 0.2 cannot go back to 0.1 (it says "newer version" and leaves them untouched); ledgers, bills of materials, intake and pick lists keep the format 0.1 reads.
- **The tapestries' album recipe** is now `curiocatalogue:ledger-album-tapestries` (was `ledger-album`), named like those of the other collections.
- **The album window is one block:** the page is as wide as its rows, in whole cells; cells spread evenly over their lines (16 crystals as 8 + 8, not 12 + 4); a narrow page stands centred; the dark frames of the pictures fill the page out to the window's edges.
- **The want list window has no empty slots:** the invoice windows (want list, bill of materials, ledger) spread their lines evenly over rows (11 lines as 6 + 5, not 10 + 1 and nine empty slots); the ledger window keeps the look of the game's chests.
- **The lists are out of the creative inventory:** an intake list, a pick list or a want list taken from there was blank and useless. Each keeps its handbook page, and the guide's links to them work; `/giveitem` still gives one. Books stay in the creative inventory.
- **The album window holds up:** after a change of the GUI scale the group names and counts are drawn for the new scale; a level whose pieces the config all excludes opens as an empty page and its layout can be switched; a book opens again after leaving a world and joining another; the tooltips cost less on every mouse move.

### For modders and server owners

- **Collections are data.** Each is defined by an asset (`config/curiocatalogue/collections/*.json`; the tapestries keep the same 88 pieces in 31 pictures). Another mod can bring a collection the same way, its album recipe named in the definition, and a JSON patch can change one. A broken definition is logged once and leaves the other collections working. README, "Adding a collection", has the format.
- **The definition** can derive a dimension from another by a table (`from`, `map`), give its values icons by a template, a table (`icons`) or our own SVG marks, give its levels and layouts an `icon`, mark attributes in a colour (`marks`), word a member's name per level (`memberKey`; `inLists` is gone) and make tabs of a dimension without names of its values (`tabs`).
- **Another mod's tapestries are named like ours:** a collection of the `tapestry` source under any id shows the game's names of its pictures, not the raw picture codes.
- **The config:** `TapestryExclude` and `TapestryAdd` moved into `Collections.tapestries` (`Exclude`, `Add`); the mod moves them on the first start. `Disabled` switches a collection off.
- **A collection switched off is told from one that is gone:** its albums and want lists say so and are refused with that reason (and work again when it is switched on, with everything collected still counted); its album recipe is removed. Each client builds its collections and tells items apart by the server's settings (`Collections`, `ExtraIgnoredAttributes`), sent on join.

## 0.1.1 (2026-10-31)

Fixes after the first release.

- Aged book covers have their own handbook pages: H over an aged book, or a link to it (the guide's "ages its cover"), opens the
  book's page with its recipes instead of the handbook's main page. Every cover is now in the creative inventory.
- The invoices have English names that follow the player's view: **Intake list** (was "Incoming invoice"), **Pick list** (was
  "Outgoing invoice"), and "Post" is now **Fill**. Item codes and saved worlds are unchanged; Ukrainian keeps its own terms.
- A blank list taken from the creative inventory is refused with a message instead of being filled as "0 lines" and giving the
  book a free sheet; cancelling it just removes it, with no sheet and no parchment.
- Cancelling a want list says "want list" (Ukrainian: "Список бажаного скасовано…") instead of calling it an invoice.
- Clearer texts after the first players' review: the album counts "pieces" and "whole pictures"; a new list says how to fill it;
  the book's tooltip says that filling moves the items into your inventory, and that a pick list from a ledger takes every stack;
  the hints over a container say "Make an intake list" / "Make a pick list"; "no sheets left" says to add parchment. README: what
  removing the mod does to books and lists.

## 0.1.0 (2026-10-28)

The first release, for Vintage Story 1.22.7. An early version: try it in a test world first and back up your world.

- **The album** knows your whole collection, shows what you are still missing and takes one of each missing piece straight from
  a chest; duplicates stay. The first collection is tapestries: 88 pieces in 31 pictures, and the tapestries of other mods.
- **Want lists** come out of the album: give one to a friend, and it gathers what you still miss from their chests.
- **The ledger** notes which kinds of items there are; **the bill of materials** how many of each. Both compare themselves with a
  chest and write invoices: an incoming one for what the book lacks, an outgoing one for what it lists.
- **Invoices** move their items from a chest into your inventory, all of them or none. The book in your off-hand is updated and
  takes the sheet back.
- Books work from the main hand or from the off-hand with the main hand empty. Their windows have search and sorting.
- Recipes, a handbook page for every book and a guide; English and Ukrainian.
- On servers, every action respects land claims, padlocks and reinforcement, as opening the chest by hand does.
- Settings: sheets per book, the entry limit, attributes ignored when comparing items, the tapestry collection, and a hardcore
  option where sheets never come back.

Known limitations are listed in the README: [Compatibility and known limitations](https://github.com/dankosedin/CurioCatalogue#compatibility-and-known-limitations).
