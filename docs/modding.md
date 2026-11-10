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
  reports the release. Swallow repeats of our own press by time (`EmptyHandGesture`), before any check of the hands: an event less
  than 500 ms after the previous one is a repeat, and each repeat extends the window, so holding gives one request. Item
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
  - claims: `world.Claims.TestAccess(player, pos, EnumBlockAccessFlags.Use)`, and nothing more. For blocks that are
    `IClaimTraverseable` and allow it (chests, baskets, vessels) the engine itself turns Use into Traverse inside it; asking for
    `Traverse` ourselves would be checked as build-or-break (decompiled `Vintagestory.Common.WorldMap`, `GetBlockingLandClaimant`
    ~326-329, `testBlockAccessInternal` ~243-254). Vanilla chests allow traverse only when `retrieveOnly || isPerPlayer`
    (vssurvivalmod `BEGenericTypedContainer.cs:458`);
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
- **A stack's bytes** (`ItemStack.cs:329#ToBytes`): int32 class (0 block, 1 item), int32 numeric id, int32 stack size, then the
  attribute tree. A stack inside the attributes (`ItemstackAttribute`, attribute id 7, `ItemstackAttribute.cs:49#ToBytes`) is a
  bool "is null" and then the same layout, so every nested stack carries its own numeric id. No codes anywhere.
- **A world keeps its numeric ids for ever** (decompiled `VintagestoryLib` 1.22.7, `ServerSystemItemIdRemapper.RemapItems`, the
  same for blocks; checked 04.11.2026). The save holds the code of every id (`SaveGame` data `ItemIDs`, `BlockIDs`). On load the
  registry is remapped so that each known code gets its stored id back; a new code gets a new id; a code no longer there becomes a
  placeholder item "unknown" that keeps the id. Stacks are never rewritten: a book's bytes stay valid in its own world through game
  and mod updates. Another world (a test server, a schematic pasted elsewhere) has other ids.
- **The schematic remap skips arrays** (`Collectible.cs:3458#OnLoadCollectibleMappings`, `Collectible.cs:3499#OnStoreCollectibleMappings`):
  it walks nested trees and `ItemstackAttribute`, not `TreeArrayAttribute`, where our entries and album members are. Seen
  04.11.2026 on a ledger made by 0.1.1 (plan backward-compat, B1): the store walk recorded the book's own id only, not the copper
  ingot or the tapestry in its entries (`ROADMAP.md`, Maybe, books in schematics).
- `ToJsonToken()` of an attribute tree is for people, not a parser: it writes some keys unquoted (`code: "ingot-copper"`).
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
  `requiresContent`). After changing recipes, run the server tests (`AssetLoadTests` fails on "Failed to parse grid recipe", "cannot be resolved" and the like).
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
- **Taking a grid recipe away on the server** (07.11.2026, K1 of the collections core plan; decompiled `VintagestoryLib` 1.22.7):
  removing it from `api.World.GridRecipes` (IWorldAccessor.cs:185#GridRecipes) is not enough. Resolving a recipe already put it
  into `FastSearchRecipesByIngredient` (CraftingRecipeIngredient.cs:294#AddToFastSearchRecipes), and the server's crafting grid
  looks recipes up there (`InventoryCraftingGrid.FindMatchingRecipe`, which skips only `Enabled: false`). So remove it from the
  list **and** set `Enabled = false`. The list is what clients get: the server serialises every recipe registry into the
  server-assets packet (`ServerMain.BuildServerAssetsPacket`), built once, after the `WorldReady` phase; `Enabled` is not
  serialised (RecipeBase.cs:106#ToBytes), so a recipe left in the list reaches clients enabled (handbook, grid preview). The window:
  after `RecipeLoader.AssetsLoaded` registered the recipes (RecipeLoader.cs:14#ExecuteOrder, RecipeLoader.cs:29#RegisterCraftingRecipe)
  and before that packet, so `AssetsFinalize` or `StartServerSide` on the server. A recipe's `Name` is its file's asset location
  unless the JSON sets one (RecipeLoader.cs:124#recipe.Name), and every recipe expanded from it keeps it
  (RecipeBase.cs:388#recipeBase.Name): find a file's recipes by `Name`. A file's recipe with `enabled: false` is never registered
  (RecipeLoader.cs:115#Enabled).
- The handbook shows the grid recipes of one `recipeGroup` as one slideshow (ours: 1 making, 2 switching the mode, 3 recharging, 4 album, 5 recolouring, 6 aging; see `docs/domain.md`).

## Handbook

- `handbook.groupBy` groups only the icons of the recipe slideshows ("Ingredient for", "Created by"): a stack matching *any* of the patterns shows the recipes of all of them. It does not merge pages: every stack of the creative inventory has its own page, and the list is not folded. Placeholders (`book-{kind}-*`) make one group per variant.
- To group differently per variant use `groupByByType` (any `*ByType` key takes the first matching wildcard; an object is merged into the base object, anything else, arrays included, replaces the base value: `RegistryObjectType.cs:352#Merge`, `RegistryObjectType.cs:356#jsonObj`). Keep no base `groupBy` next to it anyway. Ours: one icon group per kind and age (`book-{kind}-normal-*`, `book-{kind}-aged-*`), guarded by `AssetsTests.Recipe_icons_group_by_kind_and_age`.
- **A handbook page exists only for what is in the creative inventory** (or has `handbook: { include: true }`; vsapi Collectible.cs:2474#GetHandBookStacks). A variant left out of `creativeinventory` has no page: pressing H over it, or a link to it, opens the handbook's main page instead, with no error in the log (`aged-*` books, found 30.10.2026; `AssetsTests.Every_book_is_in_the_creative_inventory` guards ours). The creative inventory is the only thing that decides which pages exist, so a variant listed there has a page of its own; `groupBy` does not fold the list. Vanilla does the same with its 29 book colours.
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
  `<a href="handbook://item-curiocatalogue:book-ledger-normal-darkgreen">` (the link names one cover and opens that cover's page).
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
  it name them (`ModNetwork.MissingItemsRefusal`). Checked on a dedicated server 27.10.2026; `LanguageTests` check it on the server (a mutation to `GetName()` or `Lang.Get` fails them).
- **Chat commands:** do not use `Parsers.IntRange`; its error "Number out of range" does not say the range. Parse a plain `Int` and
  check it in the handler with a message that names the range.

## GUI

- Slot grid search: `FilterItemsBySearchText(text)` without the name cache matches nothing for a non-empty text (vanilla skips every
  slot). Pass `searchCacheNames` (and `searchCache`); see `GridDialog.FilterByName`.
- **Composers are not freed by the setter.** `SingleComposer = …` keeps the old composer and its textures (vsapi `GuiDialog.cs:129`;
  only `ClearComposers` and `Dispose` free them, `:42-59`). Dispose the old one before recomposing, as vanilla does
  (vsessentialsmod `GuiDialogAddWayPoint.cs:164`): `HeldItemDialog.SetComposer`.
- **Closed dialogs stay loaded.** `TryOpen` registers a dialog in `LoadedGuis` (vsapi `GuiDialog.cs:289`); on close the game drops it
  only when `UnregisterOnClose` is true (decompiled `GuiManager.OnGuiClosed`), default false, `GuiDialogConfirm` included. A dialog
  made per opening must set it and dispose itself on close (`HeldItemDialog`, `ConfirmDialog`); otherwise it lives until the game quits.
- **The hotbar switches while a dialog is open:** number keys and the wheel change the active slot (decompiled `HudHotbar`,
  `OnKeySlot`; checked in game 28.10.2026). A window about the item in the hand must notice it (`HeldItemDialog.Watch`) and name the
  item in its requests (`ShownItem`).
- **A tree array never equals its own clone** (`ItemStack.Equals`, vsapi `Other/ArrayAttribute.cs:26`: elements compared with `Equals(object)`, which `TreeAttribute` does not override; shown by `ReadingCostTests`, server 04.11.2026). A stack with a `TreeArrayAttribute` (our `entries`, `members`) is never "equal" to a copy, so the game's tooltip, which recomposes unless the hovered stack equals its copy (`GuiElementItemstackInfo.SetSourceSlot`, `:252`), rebuilds its text every frame for about a second after the tooltip appears (checked in game 04.11.2026, C6 of the collections-design plan); harmless for a text this cheap.

- **A slot grid always draws stacks white** (`GuiElementItemSlotGridBase.cs:504#WhiteArgb`). To change how a stack looks in a
  vanilla grid, use a slot class of our own: `ItemSlot.OnBeforeRender` (`ItemSlot.cs:529#OnBeforeRender`) runs on every draw
  and may set `renderInfo.OverlayTexture`/`OverlayOpacity` (as rot does) or `Transform`. The tooltip draws the same slot, so
  its icon changes too. `GetStackName` and `GetStackDescription` of the slot are the tooltip's title and text. A colour or
  alpha needs our own element with `RenderItemstackToGui(slot, …, color)`: the shader multiplies the texture by it (`gui.fsh`).
  Checked in game 05.11.2026 (A1 of the album view plan).
- **Composing costs per grid, not per slot** (dev client 05.11.2026): one grid of 400 slots 7 ms, 30 grids of 2 slots 32 ms.
  Change slots' stacks or flags in place instead of composing again; a slot's stack can change with no composing.
- **Tapestry icons are 3D at an angle**; each piece's flat image is `textures/block/cloth/tapestry/{type}.png`
  (`BlockTapestry.cs:40#texturePath`), and drawn side by side the pieces make the picture.
- **Text smaller than 14** (`WhiteDetailText`) does not read at the default GUI scale (the author, 05.11.2026).
- **Vertical tabs light their first tab in the constructor** (vsapi `GuiElementVerticalTabs.cs:52`), whatever the `GuiTab`s say:
  set `Active` after constructing. Several tabs may be lit at once by setting `Active` yourself; a click clears the others unless
  `ToggleTabs` (`GuiElementVerticalTabs.cs:293#SetValue`). `Gui/AlbumTabs`.
- **`OnEscapePressed` returning true means "closed".** The game calls it on every open dialog and, for each true, steps back in
  its list (decompiled `GuiManager.OnEscapePressed`, `i--`), assuming the dialog left it. A dialog that uses Escape for itself
  (cancelling an edit) and stays open must return false. Escape reaches `OnKeyDown` only of a dialog that captures all inputs.
  `AlbumDialog.OnEscapePressed`.
- **Mouse events go to a composer's elements in the order they were added,** not by draw order (vsapi `GuiComposer.cs:458#OnMouseDown`:
  the first that sets `Handled` takes the click; a move stops at the first that takes it, `:503`). An element that lies over others
  when open (a dropdown's list) must be added before them, and should not take the moves, so the ones under it can drop their
  tooltips. Vanilla's `AddDropDown` cannot show an item in a line: it composes its VTML names into a static texture
  (`GuiElementListMenu.cs:237`), where an `<itemstack>` is not drawn. `Gui/IconDropDown`.
- **Our own icon for `AddIconButton`:** `capi.Gui.Icons.CustomIcons[name] = capi.Gui.Icons.SvgIconSource(location)` (vsapi
  `IconUtil.cs:21`); the button draws it in its own colour. `Gui/AlbumHeader`.
- **An SVG in its own colours: draw it into a Cairo surface, not with `LoadSvg`.** With no colour given, the game's rasterizer
  swaps red and blue for Cairo's byte order, and `LoadSvg` uploads those bytes as RGBA: yellow comes out blue (decompiled
  `SvgLoader.rasterizeSvg`, `LoadSvg`; seen in game 10.11.2026). With a colour given both are right (the file's shape, one colour).
  `capi.Gui.DrawSvg` into an `ImageSurface`, then `generateTexture`, as vanilla's icons (vsapi `IconUtil.cs:31`): `AlbumPageElement.SvgTexture`.
- **A passive item slot is always 48×48** (vsapi `GuiElementPassiveItemSlot.cs:30` sets the bounds' size). **A statbar draws a
  segment line every 10** of its range (`GuiElementStatbar.cs:224`); `SetLineInterval` with a large value removes them.
- **Text textures need whole pixels.** `TextTextureUtil` loads text without smoothing (`TextTextureUtil.cs:85`, `linearMag`
  false), so one drawn with `Render2DLoadedTexture` half a pixel off loses rows and columns of its letters ("i" without its dot).
  A centred window can stand half a pixel off: round every position (`AlbumPageElement.Snap`). Seen in game 08.11.2026 (A4 of
  the album view plan).

## Config and logging

- **Logger:** never pass dynamic text as the format string: `api.Logger.Notification("{0}", text)`. Text with `{}` (e.g. attribute
  JSON) otherwise throws `FormatException` and the line is lost.
- The game writes the mod config back after loading it and drops comments: example settings cannot live as comments in the config file.
- Config is read on each side: tooltip limits come from the client's own config, which may differ from the server's.
- **Assets in `config/` are read on both sides, and a JSON patch with `"side": "server"` changes only the server's copy**
  (`AssetCategory.cs:16`, `JsonPatchLoader.cs:254`): a definition read by the client then differs from the server's. Clients get
  the server's mods (`ModsByServer`), so a universal patch reaches them. `blocktypes` and `itemtypes` load on the server only
  (`AssetCategory.cs:11-12`); the client gets them, attributes included, from the server's registry.
- **`api.Assets.GetMany(path)` without a domain returns that path in every domain** (AssetLocation.cs:255#BeginsWith), so
  `config/curiocatalogue/collections/` finds `<anymod>:config/curiocatalogue/collections/*.json` (07.11.2026, K1 of the collections
  core plan). Patches are applied in `AssetsLoaded` at order 0.05 (JsonPatchLoader.cs:195#ExecuteOrder) by rewriting the cached
  asset's bytes (JsonPatchLoader.cs:359#IsPatched), and `GetMany` hands out the same cached asset: read after that (our
  `AssetsLoaded` at a later order, or `AssetsFinalize`) and the patches are in. But **a client has only the mods the server tells it to
  get:** the server lists only universal mods for it (`ServerMain`, `Info.Side.IsUniversal()`), and one not `requiredOnClient`
  may be missing there, so the assets of a server-side mod, `config/` included, exist on the server only. A collection from such a mod is unknown to the
  client, as is one patched with `"side": "server"`.
- **`AssetLocation.GetName()` keeps the file ending** (`AssetLocation.cs:383`): `tapestries.json` for `…/tapestries.json`. The id of a
  definition is `Path.GetFileNameWithoutExtension(location.Path)` (07.11.2026, K3).
- **`IAssetManager` and `IAsset` cannot be mocked in `CurioCatalogue.Tests`:** NSubstitute's proxy needs `SkiaSharp` (the bitmap
  methods of `IAsset`), which the test project does not have (`FileNotFoundException` on the first call). The registry takes a
  function that gives `DefinitionAsset` (a location and a text), made from the game's assets in one line; the real read is tested
  in the compatibility tests with a test mod (`cccollections`).
- **A patch on a `config/` asset of another domain works like any patch:** `"file": "<domain>:config/curiocatalogue/collections/x.json"`
  with `"op": "add", "path": "/exclude/-"` changes what `GetMany` hands out (compatibility tests, 07.11.2026, K3). The mod project
  references the game's `Lib/Newtonsoft.Json.dll` (`Private=false`) for the definitions and the config's legacy keys.
- Every launch moves the previous logs to `Logs\Archive`. Red in-game errors are **not** logged. The server audit log names bag
  inventories `blockcontainedbaginv-…`, which tells which position was a bag.

## Network

- **A message sent in `PlayerJoin` reaches the client's handler** (07.11.2026, K1 of the collections core plan; decompiled
  `VintagestoryLib` 1.22.7). The client registers its handlers in `StartClientSide`, which runs on the server's "ready" packet
  (`ClientSystemStartup.HandleServerReady` → `StartModsFully`), before it asks to join. The join on the server
  (`ServerMain.HandleRequestJoin`) sends, in this order, the channel list, the world data, the server assets, the player's entity,
  then fires `PlayerJoin`, then sends "level finalize". The client queues channel messages until the channel list has come
  (`NetworkAPI.HandleCustomPacket`, `earlyPackets`) and runs packets on its main thread in the order they came, but holds them
  back while it decodes the server assets: when our handler runs, the items and blocks are registered, the grid recipes may not be
  yet (they come last, `HandleServerAssets_Step11`), and the client's `LevelFinalize` event has not fired (its packet comes after
  ours). So: the handler stores what came; anything that needs the world built is done at `LevelFinalize` (or rebuilt there).
  Reasoned from the code, not yet watched in game.

## Vanilla content we rely on

- **Tapestries:** block `tapestry-{north|east|south|west}`, the picture is the stack attribute `type` (`blocktypes/cloth/tapestry.json`,
  `BlockTapestry.cs`). Taken off a wall it always drops as `tapestry-north` (`BehaviorHorizontalAttachable`, `dropBlockFace`).
  A rotten tapestry drops nothing (`BlockTapestry.GetDrops`). Large ones are pieces: `ambush` (3×1) = `ambush1..3`, `salvation` (2×2)
  = `salvation11, 12, 21, 22`, the digits are row then column ("yx": `21` is the bottom left quarter; checked in game 08.11.2026); 31 pictures = 88 pieces, generated from the block attribute `sizes` (a mod that extends `sizes` is
  picked up: `ModdedTapestryTests`; but the mods that add tapestries today add blocks of their own, `ROADMAP.md`). Not in the
  creative inventory; give with `/giveblock tapestry-north 1 s[] {"type":"ambush1"}` (`docs/testing/commands.md`).
- **Books:** empty book `book-normal-<colour>` (2 leather + 8 `paper-parchment`; also `book-aged-*`, `book-rotten-*`); parchment
  stacks to 32; `inkandquill`. A found large book is clutter: block `clutter` with `type: "bookshelves/large-book-closed"` (also
  `-open`, called "Cartography book" in game; `-pile*`, `-standing*`; `-evaporating` is story-only). Clutter is `Reparable`: unless
  repaired with glue, it breaks into nothing.
- Iron parts: `metalnailsandstrips-<metal>`; vanilla has 14 metals.
- **Ore crystals** `crystalizedore-{grade}-{ore}-{rock}` (04.11.2026, details in the collections design plan, C1): 407 allowed,
  but 54 never generate (uranium and stibnite have no deposits at all in 1.22.7); only mining gives them, never an explosion.
  The name has no rock ("Poor crystallized chunk of {ore}"), only the tooltip and the icon do: two codes, one name.
  Names of the values (07.11.2026): `game:ore-{ore}` and `game:rock-{rock}` exist in `en` and `uk` for every value of
  `block/ore-graded` and `block/rockwithdeposit`; the grade has only `game:ore-grade-{grade}`, "Quality: Poor", so its names are ours.
  In `uk` the ore's names are in the genitive and lower case ("самородної міді", for "кристал. шматок {0}"), unusable as a heading,
  so ours too (09.11.2026). **Only 398 are registered** (server tests, 09.11.2026): the `ore` group is loaded from
  `worldproperties/block/ore-graded.json`, which has no stibnite, so its 9 allowed variants never become items.
- **The order of a variant group is not kept at run time** (07.11.2026): an item knows only its own values
  (RegistryObject.cs:25#VariantStrict), item ids follow the world's history (`ServerSystemItemIdRemapper`: an item new to a world
  gets the next free id, whatever its place in the group), and `itemtypes` are not read on the client
  (AssetCategory.cs:12#itemtypes). Where an order matters (the grades poor < medium < rich < bountiful), write it down.
- **Seashells** `seashell-{type}-{color}` (04.11.2026, details in the collections design plan, C2): a block, 42 combinations, a
  picked-up shell is a block stack of the same code. **A block patch's list is not always what generates:** on land
  `BlockSeashell.TryPlaceBlockForWorldGen` (`BlockShell.cs:45`) draws its own type and colour, so the loose-shell patches that
  name only clam and turritella give any of the 42; under water the patch's list holds. Before trusting `blockCodes` of a
  patch, check whether the block class overrides the placement.
- **Butterflies** `butterfly-dead-{type}` (04.11.2026, details in the collections design plan, C3): 169 types, only from the bug net.
  The entity has a 170th, `reedbutterfly`, with no item: catching it kills it and gives nothing. **A `*ByType` with no matching
  key falls back to the C# default, not to "absent":** `reedbutterfly` has no `chanceByType` entry, so its `Chance` is 1, the
  highest of all (`RegistryObjectType.cs:306#solveByType`, `SpawnConditions.cs:265`). The species is not in the code but in the
  Latin name, lang `itemdesc-creature-butterfly-{type}`.

## Other mods

- **One process for all mods' code** (VintagestoryLib, decompiled 31.10.2026: `ModContainer`, `ModAssemblyLoader`). The game
  loads only a mod's main dll (the one with a `ModSystem`) by its path. Any other dll is found through `AssemblyResolve`: in the
  game's folder, its `Lib`, then every mod folder in turn, and the first file named `<assembly>.dll` wins for all mods. Two mods
  shipping a library of the same name share one copy, and a different version can stop one of them from loading ("two mods with
  .dlls with the same name"). So we ship no third-party dll (`docs/architecture.md`, Exhaustive switch).
- **Better Crates 1.10.0** (from its IL): tessellates with its own texture source by baked name (block atlas, then
  `textures/<name>.png`); on an exception logs `<name> Item threw Exception! Shape.Base: …` and retries with the vanilla source, whose
  item-atlas UVs corrupt the chunk mesh; only `… threw Exception again!` means a cube placeholder. It places items with plain RMB, so
  our items fit in, but draws our invoices dark and books back side out. Not supported yet.
- **Carry On 2.0.0-rc.1** cannot lift two-cell trunks even alone. It uses Shift+RMB with empty hands; no conflict with our off-hand
  gesture, though `EmptyHandGesture` swallows RMB for 500 ms after ours (longer while held).
- **Upgradeable Storage** works.
- **Len's Gemology 1.0.15** (04.11.2026, from its assets) crafts vanilla ore crystals: a crystal breaks into shards, shards +
  stone + nuggets make `game:crystalizedore-*` of nearly any combination. A crafted crystal is the same item as a mined one.
- **Terra Prety 7.11.2-pre.1** (04.11.2026, assets and decompiled dll) changes terrain only (landforms, oceans, rivers, upheaval,
  trees, surface layers), not deposits, ores or rock strata: which ore occurs in which rock and grade is vanilla's.
- **Tapestries of other mods** (02.11.2026): Craftable Companion 1.5.4 and Astria Porta 0.2.3 add blocks of their own, class
  `BlockTapestry`, each with its own `sizes`; neither patches `game:tapestry`, so the album does not count their pieces (`ROADMAP.md`).
- **Every file in a mod's `patches/` is applied, whatever its extension:** a `tapestry-sizes.json.off` still patched (seen in the
  compatibility tests 02.11.2026). To switch a patch off, move the file out of the folder.

## A server inside the test process

In `docs/test-server.md`.

## C#

- A tuple element cannot be named `Rest` (reserved by `ValueTuple`).
