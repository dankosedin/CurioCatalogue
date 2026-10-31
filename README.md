# Curio Catalogue

A Vintage Story mod about collecting and keeping account of things, in the spirit of an old counting house.

Українською: [README.uk.md](README.uk.md).

Download: [Vintage Story ModDB](https://mods.vintagestory.at/curiocatalogue).

> **Early development.** This is 0.1.0, the first and minimal version of the mod (an MVP). Try it in a test world first.
> The way books and lists store their data may change in the next two releases, and books and lists made with this
> version may not carry over.
>
> **Back up your world** before installing the mod or a new version of it.

![An album on a table in a room with tapestries, shelves and display cases](tools/modicon-source.png)

## What it does

- **The album** knows your whole collection and shows what you are still missing. Click a chest with it and it takes one of each
  missing piece into your inventory; duplicates stay in the chest. The first collection is **tapestries**: 88 pieces in 31 pictures.
- **Want lists** come out of the album: give one to a friend, and it gathers what you still miss from their chests.
- **The ledger** notes which kinds of items there are; **the bill of materials** how many of each, as a shopping list for a project
  that shrinks by itself as you take what you need from chests.
- Books compare themselves with a chest and write **lists**: an **intake list** (what the chest has that the book lacks) and a **pick list** (what the chest has of the book's entries). Fill a list from a chest and the
  items move into your inventory, all of them or none.

Books and lists are ordinary items. They hold their data themselves, so you can hand them over, put them on a shelf or lose them.

![The album window: 3 of 88 pieces collected, the missing ones in a grid](docs/images/album-window.png)

## Getting started

1. **Your first book.** A vanilla book in the middle of the crafting grid, nails and strips above and below it, ink and quill to
   its right: a ledger with 8 sheets and the cover of the book you used. Anything written in that book is lost. A large book found
   in ruins can be restored with a jug of lime water and ink and quill: that ledger has 32 sheets.

   ![The first ledger in the crafting grid, with its tooltip](docs/images/first-ledger.png)

2. **The album.** An empty ledger and any tapestry piece in the crafting grid; the piece stays.
3. **Collect.** Hold the album, or keep it in your off-hand with the main hand empty, and press **Shift + right click** on a chest.
4. **Look.** Right click into the air opens the album: what is missing, how many pieces and whole pictures you have, a search,
   and the **Make a want list** button.

The in-game handbook has a Curio Catalogue guide and a page for every book.

## Gestures

![An album over a chest: Shift + right click takes new items for the collection](docs/images/album-chest.png)

With a book in hand, or in your off-hand with the main hand empty:

- **Shift + right click** on a container: the album takes what is new for the collection; a ledger or a bill of materials makes an
  intake list for what the book lacks.
- **Shift + Ctrl + right click** on a container: a pick list for what the book lists.
- **Right click into the air:** opens the book.

With a list in hand, **Shift + right click** on any container fills it. With a book in your off-hand the book is updated and
the sheet comes back (an album only takes the sheet); without one the list becomes parchment. A want list is filled the same
way: it takes what it lists, and a finished one becomes parchment.

Plain right click and Ctrl + right click open containers as usual.

## Fair play on servers

- The server decides. A list moves everything it lists or nothing, and says what stopped it.
- No more access than by hand: land claims, padlocks and reinforcement apply to every action, as to opening the chest yourself.
- No remote access: items move only from the container you click.

## Configuration

`ModConfig/curiocatalogue.json`:

- `MaxSheets` (32): sheets a book can hold.
- `SoftEntryLimit` (500): a book with this many entries makes no more intake lists.
- `ConsumeSheets` (false): hardcore; a filled list never gives its sheet back.
- `ExtraIgnoredAttributes` (`["durability"]`): attributes ignored when comparing items, on top of the game's own.
- `TapestryExclude`, `TapestryAdd`: leave tapestry pieces out of the collection, or add pieces of other mods.
- `Diagnostics` (false): write details to the game log, for finding problems.

The server and each client read their own copy of this file, and the game does not sync them. On a server with changed settings,
give players the same file: otherwise the album and the tooltips show the client's values, while the server acts on its own.

## Compatibility and known limitations

- Needs Vintage Story 1.22.7. Install it on the server and on every client.
- Mods that add tapestries to the game's tapestry list are picked up by the album.
- Tested with Upgradeable Storage and Carry On.
- **Better Crates** is not supported: its crates refuse our lists, and they draw our lists dark and our books back side out.
- Our books and lists do not go into a vanilla crate: Shift + right click on a container is ours.
- With `ConsumeSheets` on, the handbook's "the sheet comes back" does not apply.
- Supported containers: chests, trunks, labeled chests, stationary baskets, storage vessels, crates, backpacks and baskets placed on
  the ground, and other mods' chests built on the vanilla ones. On other blocks with an inventory (ovens, barrels,
  shelves…) the mod says "This container is not supported yet". Per-player loot chests in ruins are refused.

## Languages

English and Ukrainian. Translations are welcome: see `CONTRIBUTING.md`.

## Contributing

Bug reports, ideas, code and translations: see `CONTRIBUTING.md`. What may come next: `ROADMAP.md`.

## Similar mods

Curio Catalogue is about collections. Other mods overlap with it a little; if one suits you better, use it.

- **[Infinitum Collections](https://mods.vintagestory.at/infinitumcollections)** by unicorn_ares is a journal of discoveries (fish,
  mushrooms, butterflies) with links to the handbook. Curio Catalogue's album tracks what you actually keep, in chests or on
  display, and takes the missing pieces from there. It also makes want lists for other players.
- **[Bookkeeper](https://mods.vintagestory.at/show/mod/43155)** and **[Quartermaster](https://mods.vintagestory.at/show/mod/57459)**
  by Feyd manage storage: finding items in nearby containers and, with Quartermaster, taking and depositing them remotely. They
  care about what is stored, not about what is missing from a collection.

Thanks to these authors: their work makes the game's mod community richer.

## AI and human work

- **Direction:** human. DankoSedin decides what the mod is, what it does and what it will not do.
- **Code:** written by AI (Claude, Anthropic) within a framework of tests and documentation.
- **Texts and translations:** everything the player reads, in English and Ukrainian, is written by AI under close human supervision.
- **Assets:** item textures are drawn by script; the book shape and the mod icon are based on the game's own assets.

## Credits

- Thanks to Anego Studios for Vintage Story, and for opening the code of its API and of the survival mod: it taught us how the
  game works.
- Thanks to the Fendragon server, where the author collects the very tapestries this mod was made for.

## License

MIT, except a few files derived from Vintage Story: see `LICENSE`.
