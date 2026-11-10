# Changelog

What changed in each version of Curio Catalogue. A version's section is its release notes on GitHub and on the ModDB page.

## Unreleased (0.2.0)

- **Collections are data.** The tapestries are the first collection defined by an asset (`config/curiocatalogue/collections/tapestries.json`), the same 88 pieces in 31 pictures; other mods can bring a collection the same way, its album recipe named in the definition, and a JSON patch can change one. A broken definition is logged once and leaves the other collections working. The config's `TapestryExclude` and `TapestryAdd` moved into `Collections.tapestries` (`Exclude`, `Add`) and are moved by the mod on the first start; `Disabled` switches a collection off.
- **Albums remember what they collected,** piece by piece and with the item that filled it, instead of a list of what is missing. An album follows its collection as before (a new tapestry is missing, a piece excluded for a while comes back collected), and an album of 0.1 is converted when it is first read, with nothing lost; if a mod renames or removes an album's level, what it collected moves to the level that holds most of it. Albums and want lists saved by 0.2 cannot go back to 0.1 (it says "newer version" and leaves them untouched); ledgers, bills of materials, intake and pick lists keep the format 0.1 reads.
- **Want lists name what the album misses** rather than one item each: where any of several items will do (a shell of any colour), the list says so and takes any of them, the one there is room for (an album taking new does the same). Want lists of 0.1 work as before.
- **A collection switched off is told from one that is gone.** An album of a collection the server switched off says so and is refused with that reason (and works again when it is switched on, with everything collected still counted), and so is a want list of it; its album recipe is removed, and each client builds its collections and tells items apart by the server's settings (`Collections`, `ExtraIgnoredAttributes`), sent on join.
- **Ore crystals are the second collection.** An album made from an empty ledger or bill of materials and a crystal (the crystal stays) collects the ore crystals of the game at three levels: the richest of each ore, each ore in each rock, or every crystal; the window shows them by ore or by rock. Want lists name the rock where one rock holds a crystal and say "any rock" where several do. English and Ukrainian.
- **Seashells are the third collection.** An album made from an empty ledger or bill of materials and a seashell (the shell stays) collects the 42 seashells of the game at two levels: every type in any colour (7), or every type in every colour (42, one table of types by colours). A want list says "any colour" where any will do. English and Ukrainian.
- **Butterflies are the fourth collection.** An album made from an empty ledger or bill of materials and a dead butterfly (the butterfly stays) collects the 169 dead butterflies of the game, one level, every butterfly its own entry, in the game's own names. A want list names each butterfly missing. English and Ukrainian.
- **The album window is one block.** The page is as wide as the rows under it, in whole cells (no empty strip on the right); cells in order spread evenly over their lines (16 crystals as 8 + 8, not 12 + 4); a page narrower than the window stands centred, and a tab of one group has its frame out to the window's edges.

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
