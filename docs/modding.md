# Modding Vintage Story: what we learned

How the game behaves where it matters to this mod, and how to write code and assets that work with it. Game version 1.22.7.
The most dangerous traps are repeated in `docs/critical.md`.

**Read** before changing any code or JSON asset. **Update** when the game surprises you: write the behaviour, how it was
confirmed (a reference into vsapi/vssurvivalmod/vsessentialsmod, decompiled engine code, or "checked in game <date>"), and what we do about it.

**References into the game source** (here, in code comments and in tests) look like `Collectible.cs:89#HeldPriorityInteract`:
file name, line or range (`Block.cs:2195-2207#interactionsIndex`), and a symbol that appears there. They hold for the game version
in `modinfo.json` of the same commit; a reference meant for another version says so (`vsapi@1.21.6 Collectible.cs:...`).
Write each one in full (no `:182` continuations). `tools/check-refs.ps1` checks them all; see "Updating the game version" in
`docs/development.md`.

## Finding out how the game works

In this order:

1. **Vanilla source.** The API and the vanilla mods are open: `anegostudios/vsapi`, `vssurvivalmod`, `vsessentialsmod`. Use a clone at
   the game version (each repo's commit "Updated to Version 1.22.7"); `docs/onboarding.md` says where. Cite file and line.
2. **Vanilla assets.** `<game>\assets\survival\` (blocktypes, itemtypes, recipes). Read them instead of guessing codes and attributes.
3. **The closed engine** (`VintagestoryLib.dll`): decompile with `ilspycmd` to read, never to copy into the repo.
4. **A probe in game:** a small temporary item or command that logs what it sees; then remove it. The first one (stage 1) logged the
   side, keys, block, block entity class, access and container contents on every click.

## Clicks and gestures

- **Item first:** `HeldPriorityInteract = true` (set in `OnLoaded`; not exposed in JSON) makes Shift+RMB on a block call the held item's
  `OnHeldInteractStart` before the block (vsapi Collectible.cs:89#HeldPriorityInteract; vanilla torch does the same, BlockTorch.cs:39#HeldPriorityInteract). Without Shift the block goes first.
- Read modifiers from `byEntity.Controls.ShiftKey` / `CtrlKey`, not `Sneak` / `Sprint` (the API docs say so for click + modifier).
- `groundstorage` delivers plain RMB to the held item too: without Shift, return without setting `handling`, so vanilla behaviour stays.
- **Clicks that are not ours must go to `base.OnHeldInteractStart`:** it runs the item's behaviors, e.g. `GroundStorable` puts the item
  down on Shift+RMB. A bare `return` silently breaks that.
- Shift+RMB is ours only on blocks with an inventory (`ContainerReader.Classify` ≠ `None`), on both sides; elsewhere the game handles it.
- Consequence: the vanilla crate inserts the held item on Shift+RMB (`BECrate.cs`), so our items can never be put into a crate by hand.
- **Held right button with an empty hand:** `Input.InWorldAction` repeats `InWorldRightMouseDown` every 250 ms while held and never
  reports the release. Swallow repeats of our own press by time (`EmptyHandGesture`), before any check of the hands. Item
  interactions do not need this: they get `firstEvent`.
- While any dialog is open, the mouse is free and world clicks do nothing.

## Interaction hints

- An item's `GetHeldInteractionHelp` shows at the bottom of the screen, and **only for the main hand**; the off-hand never shows it
  (a vanilla torch in the left hand shows nothing either).
- Hints over a block come only from the block and its behaviors. Ours go over containers via `LedgerHintsBehavior`, added to storage
  blocks **on the client** in `AssetsFinalize`. Behaviors added in code on the server do not reach the client (checked in game:
  63 blocks on the server, 0 on the client). Shown only while holding a ledger (agreed with the author).
- The HUD asks the block for hints only when the aimed block changes: changing what is in the hand while looking at the same chest
  does not refresh them (engine; checked in game 26.10.2026).
- Key names in hints: `<hk>shift</hk>`, `WorldInteraction.HotKeyCode` and `capi.Input.HotKeys[code].CurrentMapping`
  (vsapi HotkeyComponent.cs:19#HotKeys) all show the player's binding. With the default "bind mouse modifiers to sneak/sprint"
  (`separateCtrlKeyForMouse: false`), rebinding Sneak makes all hints (vanilla too) show the new key until the end of the session,
  while the action stays on Shift: a game bug, not ours (checked in game 27.10.2026).

## Access

- Because the item handles the click first, the block and its behaviors never run, so vanilla lock checks (`BlockBehaviorLockable`)
  do not either. Check on the server:
  - claims: `world.Claims.TestAccess(player, pos, EnumBlockAccessFlags.Use)`; `Traverse` is enough for `IClaimTraverseable` block
    entities (chests, baskets, vessels), as vanilla does;
  - padlocks and reinforcement: `ModSystemBlockReinforcement.IsLockedForInteract(pos, player)`.
- Other protection mods hook `IEventAPI.OnTestBlockAccess`, which `TestAccess` calls, so they are covered.

## Containers and inventories

- Chests, trunks, labeled chests, baskets, storage vessels and crates share `IBlockEntityContainer.Inventory`.
- A backpack or basket on the ground is a stack in a `groundstorage` slot; its contents through
  `stack.Collectible.GetCollectibleInterface<IHeldBag>()` (vanilla: `BEBehaviorContainedBagInventory`).
- Slots handed out by a bag (`IHeldBag.GetOrCreateSlots`) must belong to an inventory that holds them, or the first change throws
  (`InventoryBase.cs:488#ArgumentException`). Use a wrapper `InventoryGeneric`, as vanilla does.
- Only the top level is compared; never descend into nested containers.
- **Multiblocks:** a trunk is two cells; the second is an invisible `BlockMultiblock` filler without a block entity that forwards
  clicks and hints to the main block. Resolve the aimed position with `ContainerReader.MainPos` before reading the block entity.
- Per-player loot chests (`InventoryPerPlayer`) are not supported.
- **All or nothing:** `TryGiveItemstack` puts items in partly. Check the whole plan first (free room via `GetMergableQuantity` for
  partly filled slots, `ItemSlot.CanHold` for empty ones: bags with `storageFlags` refuse some items), then move with `TryPutInto`.
- Player inventory: `IPlayerInventoryManager.GetOwnInventory("hotbar" / "backpack")`; off-hand `EntityAgent.LeftHandItemSlot`.

## Stacks and item identity

- **Identity** = code + attributes minus `GlobalConstants.IgnoredStackAttributes` (`temperature`, `toolMode`, `renderVariant`,
  `transitionstate`) and `durability`, which we add. Vanilla compares with `ItemStack.Equals(world, other, ignoredAttributes)`.
- Vanilla's ignored-attribute check in nested trees passes the parent's path (a vanilla bug); `AttrCanonical` uses full paths on purpose.
- **A stack read from bytes** knows only its numeric id until it is resolved, so the same item can key as `game:tapestry-north` or by id.
  `StackKeys` resolves stacks through the world first.
- **Our key is not the game's merge rule.** The game stacks a source onto a sink by `sink.Collectible.GetMergableQuantity(sink,
  source, priority)` (vsapi `Collectible.cs:2558`, called from `ItemSlot.CanTakeFrom`, `ItemSlot.cs:110`): `Equals` with
  `IgnoredStackAttributes`, but behaviors and items may override it (vssurvivalmod `ItemEgg.cs:12` ignores "chick"). It knows
  nothing of our config, so a worn and a fresh ingot share our key (durability ignored) and still need two slots. Plan moves by
  merge classes (`InvoicePoster.MergeClasses`), not by keys.
- `new ItemStack()` has stack size 0, and our storage code drops it as empty: set the size in tests.
- **Numbers from JSON** (recipe output, `/giveitem`) land in item attributes as longs: read them with `GetAsInt`, not `GetInt`.

## Items, shapes and textures

- Rollable items (a scroll on the ground and on a scroll rack) must derive from `ItemRollable` (vssurvivalmod); the `rolledShape`
  attribute alone does nothing.
- **Off-hand:** an item needs `storageFlags` with bit 256 (e.g. `257`) to be placeable in the left hand.
- On shelves: copy vanilla book's `shelvable: true`, `displayable.shelf { size, category: "book" }`, `displaycaseable`,
  `bookshelveable` (`itemtypes/lore/book.json`).
- **No `overlays` in item JSON.** A texture with overlays has no file of its own, and mods that look textures up by file name fail on
  it (Better Crates broke the chunk mesh around a crate holding such an item; 27.10.2026). Overlays also cannot depend on stack
  attributes: they are baked once per variant at load. For looks that depend on attributes, build the mesh on the client and cache it
  per combination (vanilla `ItemShield`; `IContainedMeshSource` for shelves and the ground).
- Draw whole PNGs with `tools/textures.ps1` (no vanilla pixels). For a mark on a vanilla texture, add an element to our own copy of the shape.
- Check orientation in game: the invoice label's inventory icon came out rotated 180° against the texture, so the generator flips it.

## Recipes

- A code without a domain resolves in the mod's own domain (`curiocatalogue:`), so every vanilla code needs `game:` (also inside
  `requiresContent`). After changing recipes, check `server-main.log` for "Failed to parse grid recipe" and "cannot be resolved".
- No comments inside a recipe array: each comment becomes an array element and logs "Failed to parse" (vssurvivalmod RecipeLoader.cs:89#parsedContent).
- An ingredient with an exact code is matched by `Satisfies`: the recipe's attributes must be a subset of the stack's, so
  `{ code: "clutter", attributes: { type: "bookshelves/large-book-closed" } }` works without code.
- A liquid in a grid recipe is plain JSON, as in vanilla dough (`recipes/grid/dough.json`):
  `recipeAttributes: { requiresContent: { type: "item", code: "limewaterportion" }, requiresLitres: N }`. The liquid is used up, the jug stays.
- An ingredient with `isTool` costs 1 durability by default; for a tool without durability (ink and quill) set `toolDurabilityCost: 0`,
  or the recipe never matches.
- Keep the output plain (no `attributes`) when the handbook should link the recipe to the item's page; set data in
  `OnCreatedByCrafting` instead. The handbook calls `OnCreatedByCrafting` too, with a `DummySlot` output, and links by code +
  attributes: skip writing data when `outputSlot is DummySlot`.
- Placeholders `{name}` in codes (1.22 "advanced wildcard", vsapi RecipeBase.cs:347#ReplaceVariantsToRegex) become `([w-]+)`, which also matches hyphens and
  is greedy: with two placeholders in one code give the first one `allowedVariants`, or it swallows part of the second. `*` mixes with
  placeholders. Every combination of the `allowedVariants` that exists as an item becomes its own recipe.
- The `copyAttributesFrom` ingredient must resolve to one exact item: no plain `*` in its code (use a placeholder with `allowedVariants`).
  A plain wildcard leaves `ResolvedItemStack` null, and the handbook crashes on it
  (vssurvivalmod CollectibleBehaviorHandbookTextAndExtraInfo.cs:2271#ResolvedItemStack; crash 26.10.2026).
- The handbook shows the grid recipes of one `recipeGroup` as one slideshow (ours: 1 making, 2 switching the mode, 3 recharging, 4 album, 5 recolouring, 6 aging; see `docs/domain.md`).

## Handbook

- `handbook.groupBy` groups a stack with everything matching *any* of its patterns; use a placeholder (`book-{kind}-*`) for one page per variant.
- Own items can add to their page via `ICustomHandbookPageContent.OnHandbookPageComposed`. A button on *every* item's page would need a
  Harmony patch of `CollectibleBehaviorHandbookTextAndExtraInfo.GetHandbookInfo`.
- "Ingredient for" first looks pages up in `FastSearchRecipesByIngredient`, whose entries merge ingredients that differ only in
  attributes and keep the last recipe's attributes in the sample; `HandbookIndexFix` gives them a sample without attributes on the
  client. Replace that sample, never change it: on the client it is the last ingredient's own stack (see the comment there).
- Lang keys of another domain work in our lang files (`"game:tapestry-"` names the attribute-less tapestry in recipes; the game puts it
  into its own template as the picture name, «Гобелен «будь-який шматок»», so it must not say "tapestry" itself; checked in game 27.10.2026).
- Guides: a JSON `{ pageCode, title, text }` in `assets/curiocatalogue/config/handbook/` (all domains are read, vssurvivalmod
  GuiDialogSurvivalHandbook.cs:42#GetMany); title and text are lang keys. VTML: `<strong>`, `<i>`, `<font color>`, `<br>`, `<hk>shift</hk>`
  (the player's own binding, localized), `<icon>rightmousebutton</icon>`,
  `<itemstack type="item">curiocatalogue:book-album-normal-brickred|…</itemstack>` (no attributes),
  `<a href="handbook://item-curiocatalogue:book-ledger-normal-darkgreen">` (any cover opens the grouped page).
- A link to a page with attributes: `handbook://block-clutter-{{ \\\"type\\\": \\\"bookshelves/large-book-closed\\\" }}` in the JSON
  file, spaces as `ToJsonToken` writes them; the click handler drops the backslashes (SurvivalHandbook.cs:58#Replace). Checked in game 27.10.2026.
- Whitespace right after a closing tag is dropped: put the space inside the tag (`<i>Italic. </i>`).
- A repaired clutter piece broken out gets `collected: true` (`BehaviorReparable.cs:74#collected`); mods that add their own stacks
  (Better Ruins: `{type, collected}`) get handbook pages separate from vanilla's `{type}` one.

## Texts and messages

- modid `curiocatalogue` (lowercase); lang keys like `curiocatalogue:item-<code>`; every `lang/*.json` has exactly the keys of `lang/en.json`.
- Lang texts go through `string.Format`: write `{` and `}` as `{{` `}}`, or the game logs "Translation string format exception".
- The mod's name in `modinfo.json` is not localizable (`Collectible.cs:2148#Info.Name`): the mod manager shows it before mod code loads.
- **Player messages:** refusals via `PlayerMessages.Refuse` (red in-game error, lang key `curiocatalogue:ingameerror-<code>`), results
  via `PlayerMessages.Tell` (chat). Both translate on the server with `Lang.GetL(player.LanguageCode, ...)`.
- **Item names on the server:** `ItemStack.GetName()` gives the *server's* language (`ServerLanguage` in serverconfig), not the
  player's; singleplayer hides this. Our own items: `PlayerMessages.ItemName`. Any other items: send the stacks to the client and let
  it name them (`ModNetwork.MissingItemsRefusal`). Checked on a dedicated server 27.10.2026.
- **Chat commands:** do not use `Parsers.IntRange`; its error "Number out of range" does not say the range. Parse a plain `Int` and
  check it in the handler with a message that names the range.

## GUI

- Slot grid search: `FilterItemsBySearchText(text)` without the name cache matches nothing for a non-empty text (vanilla skips every
  slot). Pass `searchCacheNames` (and `searchCache`); see `GridDialog.FilterByName`.

## Config and logging

- **Logger:** never pass dynamic text as the format string: `api.Logger.Notification("{0}", text)`. Text with `{}` (e.g. attribute
  JSON) otherwise throws `FormatException` and the line is lost.
- The game writes the mod config back after loading it and drops comments: example settings cannot live as comments in the config file.
- Config is read on each side: tooltip limits come from the client's own config, which may differ from the server's.
- Every launch moves the previous logs to `Logs\Archive`. Red in-game errors are **not** logged. The server audit log names bag
  inventories `blockcontainedbaginv-…`, which tells which position was a bag.

## Vanilla content we rely on

- **Tapestries:** block `tapestry-{north|east|south|west}`, the picture is the stack attribute `type` (`blocktypes/cloth/tapestry.json`,
  `BlockTapestry.cs`). Taken off a wall it always drops as `tapestry-north` (`BehaviorHorizontalAttachable`, `dropBlockFace`).
  A rotten tapestry drops nothing (`BlockTapestry.GetDrops`). Large ones are pieces: `ambush` (3×1) = `ambush1..3`, `salvation` (2×2)
  = `salvation11, 12, 21, 22`; 31 pictures = 88 pieces, generated from the block attribute `sizes` (mods that extend `sizes` are
  picked up). Not in the creative inventory; give with `/giveblock tapestry-north 1 <player> {type:"ambush1"}`.
- **Books:** empty book `book-normal-<colour>` (2 leather + 8 `paper-parchment`; also `book-aged-*`, `book-rotten-*`); parchment
  stacks to 32; `inkandquill`. A found large book is clutter: block `clutter` with `type: "bookshelves/large-book-closed"` (also
  `-open`, called "Cartography book" in game; `-pile*`, `-standing*`; `-evaporating` is story-only). Clutter is `Reparable`: unless
  repaired with glue, it breaks into nothing.
- Iron parts: `metalnailsandstrips-<metal>`; vanilla has 14 metals.

## Other mods

- **Better Crates 1.10.0** (from its IL): tessellates with its own texture source by baked name (block atlas, then
  `textures/<name>.png`); on an exception logs `<name> Item threw Exception! Shape.Base: …` and retries with the vanilla source, whose
  item-atlas UVs corrupt the chunk mesh; only `… threw Exception again!` means a cube placeholder. It places items with plain RMB, so
  our items fit in, but draws our invoices dark and books back side out. Not supported yet.
- **Carry On 2.0.0-rc.1** cannot lift two-cell trunks even alone. It uses Shift+RMB with empty hands; no conflict with our off-hand
  gesture, though `EmptyHandGesture` swallows RMB for 250 ms after ours.
- **Upgradeable Storage** works.

## C#

- A tuple element cannot be named `Rest` (reserved by `ValueTuple`).
