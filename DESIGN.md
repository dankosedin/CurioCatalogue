# Curio Catalogue: design

Ukrainian name: Каталог дивовиж ("a catalogue of curiosities").

Why the mod exists, for whom, and the ideas that decide what it does. How each mechanic works: `docs/domain.md`. What may come
next: `ROADMAP.md`.

**Read** before changing what the mod does for the player. **Update** only after agreeing the change with the author.

## What it is

A Vintage Story mod about keeping account of things, in the spirit of an old counting house.

Its heart is the **album**: a collector's catalogue of a collection (tapestries first) that knows what is still missing, takes new
pieces straight from a chest, and makes **want lists** for other players. Next to it, the **bill of materials**: a shopping list for a
project that shrinks itself as you take what you need from chests. Both are made from the **ledger**, the base book that compares
itself with a container and writes **invoices**: documents listing what to move.

## For whom

- **Collectors.** Tapestries come in 88 pieces from ruins, and duplicates pile up; the album tells what is new and what is missing.
  More collections follow: butterflies next, then the player's own.
- **Builders.** A bill of materials for a project tells what is still missing and picks it from chests.
- **Players on a server.** Want lists go to friends; the books and invoices are items that can be handed over.

## Main ideas

- **Documents are items.** A book or an invoice holds its data itself: it can be handed over, put on a shelf, carried, lost. No
  hidden database, no data that lives only in a window.
- **A counting house, not a machine.** A register, incoming and outgoing documents, parchment as their paper, recipes from vanilla
  things. The mechanics should feel like bookkeeping a person could do by hand.
- **Lighter, not invisible.** We take the tedium out of routine tasks but keep them felt: the player still walks to the chest,
  clicks it, and sees the items move.
- **Adding to the world, not taking from it.** The mod gives vanilla things new meaning (a tapestry is a piece of a collection,
  parchment is a sheet in a book) rather than replacing or hiding what the game has.
- **Immersive.** Gestures, items, texts and looks belong to the world: a book in the off-hand, a document in the hand, no
  floating menus that the world would not have.
- **The album looks at a collection; it does not own it.** The collection is the items themselves, wherever they lie or hang.
- **Looks matter.** Collectors love beautiful things, and things that players use earn beauty: the album's own label, a stamp-album
  view, decorations for a complete collection.

## Principles for decisions

- **No more access than by hand.** Land claims, padlocks and reinforcement apply to every book action, as to opening a chest.
- **The server decides, and the state stays consistent.** What moved is exactly what got recorded, or nothing changes.
- **The game keeps what is not ours.** Clicks and gestures we do not use behave as in vanilla.
- **The player is never left guessing.** Every refusal says what stopped it and what to do.

## What the mod does not do

- **No remote access, no storage network.** Items move only where the player stands.
- **No automatic sorting.** The player decides what goes where; the books help to see and to move.
- **No prices.** Trading stays between players; the mod only makes carrying things easier and more tangible.

## Where next

See `ROADMAP.md`. Next in short: the album shows the whole collection, butterflies, partial posting of invoices, recipes into a
bill of materials, the player's own collections, chests on boats.
