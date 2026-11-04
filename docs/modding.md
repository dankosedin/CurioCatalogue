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
  (vsessentialsmod `GuiDialogAddWayPoint.cs:164`): `GridDialog.SetComposer`.
- **Closed dialogs stay loaded.** `TryOpen` registers a dialog in `LoadedGuis` (vsapi `GuiDialog.cs:289`); on close the game drops it
  only when `UnregisterOnClose` is true (decompiled `GuiManager.OnGuiClosed`), default false, `GuiDialogConfirm` included. A dialog
  made per opening must set it and dispose itself on close (`GridDialog`, `ConfirmDialog`); otherwise it lives until the game quits.
- **The hotbar switches while a dialog is open:** number keys and the wheel change the active slot (decompiled `HudHotbar`,
  `OnKeySlot`; checked in game 28.10.2026). A window about the item in the hand must notice it (`GridDialog.Watch`) and name the
  item in its requests (`ShownItem`).

## Config and logging

- **Logger:** never pass dynamic text as the format string: `api.Logger.Notification("{0}", text)`. Text with `{}` (e.g. attribute
  JSON) otherwise throws `FormatException` and the line is lost.
- The game writes the mod config back after loading it and drops comments: example settings cannot live as comments in the config file.
- Config is read on each side: tooltip limits come from the client's own config, which may differ from the server's.
- **Assets in `config/` are read on both sides, and a JSON patch with `"side": "server"` changes only the server's copy**
  (`AssetCategory.cs:16`, `JsonPatchLoader.cs:254`): a definition read by the client then differs from the server's. Clients get
  the server's mods (`ModsByServer`), so a universal patch reaches them. `blocktypes` and `itemtypes` load on the server only
  (`AssetCategory.cs:11-12`); the client gets them, attributes included, from the server's registry.
- Every launch moves the previous logs to `Logs\Archive`. Red in-game errors are **not** logged. The server audit log names bag
  inventories `blockcontainedbaginv-…`, which tells which position was a bag.

## Vanilla content we rely on

- **Tapestries:** block `tapestry-{north|east|south|west}`, the picture is the stack attribute `type` (`blocktypes/cloth/tapestry.json`,
  `BlockTapestry.cs`). Taken off a wall it always drops as `tapestry-north` (`BehaviorHorizontalAttachable`, `dropBlockFace`).
  A rotten tapestry drops nothing (`BlockTapestry.GetDrops`). Large ones are pieces: `ambush` (3×1) = `ambush1..3`, `salvation` (2×2)
  = `salvation11, 12, 21, 22`; 31 pictures = 88 pieces, generated from the block attribute `sizes` (a mod that extends `sizes` is
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

`CurioCatalogue.ServerTests` (`docs/development.md`, Server tests) starts the dedicated server the way `ServerProgram.Main` does
(VintagestoryLib 1.22.7, decompiled 02.11.2026): `ServerMain.xPlatInterface`, `ServerMain.Logger = new ServerLogger(args)`,
`Lang.PreLoad`, `new ServerMain(null, [], args, isDedicatedServer: true)`, `exitState`, `Standalone`, `PreLaunch()`, `Launch()`,
then `Process()` in a loop on its own thread until asked to stop, then `Stop` and `Dispose` on the same thread. `ServerProgramArgs`
takes the command-line options (`Port`, `AddOrigin`, `WithConfig` with any `serverconfig.json` value, e.g. `ModPaths`, `WorldConfig`).
Traps, in the order they showed up (each blocked the next):

1. **`GamePaths.DataPath` defaults to `%AppData%\VintagestoryData`**, the author's main game (`GamePaths.cs:16-27#GamePaths`).
   `ServerMain` reads only this static, not `ServerProgramArgs.DataPath`. Set it first and check it before anything is written
   (`ServerLogger` creates its files).
2. **Two Newtonsoft.Json.** The test SDK brings 13.0.3, the game needs 13.0.4; both have assembly version 13.0.0.0 and only one is
   loaded (`MissingMethodException: JToken.WriteTo` in `ServerConfig.Save`). The project copies the game's DLL over its output.
3. **The base directory.** `GamePaths.Binaries`, `AssetsPath`, the game's `AssemblyResolver` and `ModCompilationContext`
   ("Referenced library not found: SkiaSharp.dll") start from `AppDomain.BaseDirectory`, the test bin. Fix: in a
   `[ModuleInitializer]`, `AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", <game folder with a trailing separator>)` before any
   game type is touched. This also makes `RuntimeEnv.IsDevEnvironment` false, as for the real exe.
4. **The current directory.** Mono.Cecil, which the mod loader reads mod DLLs with, resolves `VintagestoryAPI` from the current
   directory; without it "Failed to resolve assembly", then "game - Missing" and a null reference in the playstyle. Set
   `Environment.CurrentDirectory` to the game folder.
5. **Our assets.** The Debug build has no assets: add our `assets` folder with `AddOrigin`, as the launch profiles do. An origin
   is not a mod: with our mod missing, our item JSON still loads, without our classes.

Not needed: the game's `Lib` on `PATH` (the native libraries load without it, checked 02.11.2026).

**Other mods in the test server** (checked 02.11.2026). A mod zip in a `ModPaths` folder loads as in the game. A code mod's zip is
unpacked into `<DataPath>\Cache\unpack\…` and its DLL is loaded from there; on Windows a loaded DLL stays locked until the process
ends, so `Directory.Delete` of the data folder fails with `UnauthorizedAccessException` on it (the zip itself is free). The fixture
leaves `Cache` and the next run deletes it.

**Our mod from a release zip** (checked 04.11.2026, plan backward-compat, B1). The zip in a `ModPaths` folder and no `AddOrigin`:
the server loads it as any mod zip ("External Origins … mod@curiocatalogue_0.1.1.zip"), and a program compiled against the zip's
DLLs (not copied) binds to the loaded ones by name, as the tests do with the build. On its first start the mod writes its default
config to `<DataPath>\ModConfig\curiocatalogue.json`; the stop saves the world to `<DataPath>\Saves\default.vcdbs` (a superflat
world: about 4 MB).

**Our types in the tests** (checked 02.11.2026). The mod loader loads a mod's DLL with `Assembly.UnsafeLoadFrom(path)`
(`ModAssemblyLoader.LoadFrom`), into the default load context. After that, a reference by name (a test using `ItemLedger`)
gets that loaded assembly, without asking `AssemblyResolve`: tests and game share one copy. If the test bin holds a copy too
(a plain project reference), `LoadFrom` of the mod's path quietly returns the bin's copy (an assembly of that name in the
app's own list), so the game runs a file other than the one in `ModPaths`. A real second copy comes only from `LoadFile` or
another load context.

**A player without a client** (decompiled `VintagestoryLib` 1.22.7, checked 02.11.2026). The server's network classes are public
(`NetServer`, `NetConnection`, `NetIncomingMessage`, `Packet_Client`, `Packet_ClientSerializer`, `Packet_ServerSerializer`), and
`ServerMain.MainSockets[0]` is empty on a dedicated server (the singleplayer client's dummy socket goes there). A socket of our own
put there is read by the server's packet loop like the TCP one: a `Connect` message makes a `ConnectedClient`, then the packets
a client sends on join make a real `ServerPlayer` through the server's own path (`HandlePlayerIdentification` → events,
`OnPlayerJoin` of every system, the entity spawned at the world spawn). Packet ids: 34 positions over TCP, 1 identification,
11 request join, 26 client loaded, 29 player ready; 23 a mod channel message. Traps:

- **`VerifyPlayerAuth`** must be false, or identification waits for the auth server forever. The whitelist is skipped for a local
  address (127.0.0.1). A non-dummy socket gets the default role (`suplayer`), not the singleplayer's top role.
- **`NetworkVersion`** in the identification is compared with `GameVersion.NetworkVersion` ("1.22.6" on game 1.22.7); a wrong one
  disconnects with "Incompatible Server Version". `FakeClient` sends the constant of the API it is built against, and fails at once with
  the reason when the server disconnects it. So does a player name that does not match `^(\w|-){1,16}$`.
- **Packet 34 first.** Without it, `FinalizePlayerIdentification` starts a thread-pool task that checks 10 s later whether UDP
  came; in the test process it throws a `NullReferenceException` there and kills the process. A real client sends 34 when its UDP
  does not get through.
- The server compresses a packet over 5 KB for any socket but `DummyNetConnection` (`Compression.Decompress` reads it).
- A mod channel's id is internal: the server tells the joining client in packet 56 (`NetworkChannels`, names and ids). A message
  id is the type's place in the channel's `RegisterMessageType` calls, from 0.
- The join makes one long tick: "Server overloaded. A tick took …ms" (a warning).

**Several players, clicks and requests** (checked 02.11.2026). The server's default `MaxClients` is 16 and a fake client never leaves, so the
fixture sets `MaxClients = 1000` (30 joins in a row work) and `ClientConnectionTimeout = 3600` (default 150 s: a fake client does not answer
pings; the long value is set, not tried). A player name is at most 16 characters (letters, digits, `-`): a longer one is refused at
identification ("invalid Playername") and the join times out; `FakeClient.WaitFor` now adds the end of the server log to its timeout
message. A click is `item.OnHeldInteractStart(slot, player.Entity, selection, null, true, ref handling)` on the main thread with
`Entity.Controls.ShiftKey` (and `CtrlKey`) set: no network, the same code the game runs for the click. A creative player reaches 100
blocks (`WorldData.PickingRange`), so "out of reach" needs 100+ blocks of distance; a server entity's place is `ServerPos`, and `Pos` follows
it each tick: move both. The red refusals arrive as `Packet_Server.IngameError` (its `Code` is the lang key suffix) and chat lines as
`Chatline`, in `FakeClient.Received`.

**Blocks in the test world** (checked 02.11.2026). `BlockAccessor.SetBlock(id, pos, stack)` near `DefaultSpawnPosition` (the spawn
chunks are loaded) makes the block entity and initializes it with the server api, so `Inventory` exists; the stack's attributes
(`isPerPlayer`) reach it. A trunk's filler cell comes only from `Block.DoPlaceBlock(world, player, selection, stack)`, which needs a
real player (`FakeClient`). Land claims and padlocks work for the fake players, creative or not: a claim keeps a creative visitor
out (`TestAccess`), and `TryLock` locks a chest against them. An `[InlineData]` argument of a type from our DLL (an enum) is read at
discovery, before the module initializer can find the DLL ("Could not load file or assembly 'curiocatalogue'"): pass a string.

**More traps of the tests** (checked 02.11.2026). `FakeClient.WaitFor(done, …)` runs `done` on the server's main thread: a `game.OnMain` inside it
waits for the thread it blocks and times out after 30 s; read the world directly there. Ground storage (`GroundStorable`, Shift + click on a
block) does not use up the item of a creative player: the hand keeps it, and the block entity above the clicked block holds a copy. The
mod's lazily built parts (`keys ??= …` in `CurioCatalogueModSystem`) read the config when first used, so a config changed on the running
server reaches them only after they are dropped; in the game that is a restart.

**A creative player's inventory is never full** (decompiled `VintagestoryLib` 1.22.7, checked 03.11.2026). `TryGiveItemstack` puts the stack into
the best slot of any player inventory the player has opened (`PlayerInventoryManager.GetBestSuitedSlot`); in creative that includes the creative
inventory, which swallows anything. With the hotbar and backpack full it then returns `false` and leaves the stack at size 0, so our
"else drop it on the ground" spawns nothing (in game a new list would vanish; `ROADMAP.md`). `CloseInventory` does not close the creative
inventory while the player is in creative. A test that needs a full inventory switches its own player to survival
(`WorldData.CurrentGameMode = EnumGameMode.Survival`, then `BroadcastPlayerData(true)`; `BookEdgeTests.AsInSurvival`): then the stack is
refused whole.

**Crafting on the server** (decompiled `VintagestoryLib` 1.22.7, checked 02.11.2026). The grid is the player's own inventory
`craftinggrid-<uid>` (`InventoryCraftingGrid`: slots 0–8 row by row, 9 the output), there on the server for a creative player too
(only closed for the client). A stack put into a slot plus `MarkDirty` makes it look for a recipe (`OnItemSlotModified` →
`FindMatchingRecipe`: `Matches`, so `MatchesForCrafting`; then `GenerateOutputStack`, so `copyAttributesFrom` and `OnCreatedByCrafting`).
The client takes the output with packet 7 (`ActivateInventorySlot`); the server hands it to `InventoryCraftingGrid.ActivateSlot(9,
mouseSlot, op)` (`ServerSystemInventory.HandleActivateInventorySlot`), so a test calls that with `MouseItemSlot` and an
`ItemStackMoveOperation` whose `ActingPlayer` is set: `CraftSingle` then calls `ConsumeCraftingIngredients` (ours) or the recipe's
`ConsumeInput`. A liquid ingredient takes `requiresLitres` out of the jug and the jug stays in the grid.

## C#

- A tuple element cannot be named `Rest` (reserved by `ValueTuple`).
