# Changelog

What changed in each version of Curio Catalogue. A version's section is its release notes on GitHub and on the ModDB page.

## 0.1.1 (unreleased)

Fixes after the first release.

- Aged book covers have their own handbook pages: H over an aged book, or a link to it (the guide's "ages its cover"), opens the
  book's page with its recipes instead of the handbook's main page. Every cover is now in the creative inventory.
- The invoices have English names that follow the player's view: **Intake list** (was "Incoming invoice"), **Pick list** (was
  "Outgoing invoice"), and "Post" is now **Fill**. Item codes and saved worlds are unchanged; Ukrainian keeps its own terms.
- A blank list taken from the creative inventory is refused with a message instead of being filled as "0 lines" and giving the
  book a free sheet.
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
