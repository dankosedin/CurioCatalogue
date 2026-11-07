# A server inside the test process

How the game server that the server tests and the compatibility tests start inside their own process works, and its traps.
Game version 1.22.7; references into the game source as in `docs/modding.md`.

**Read** before changing the fixture (`CurioCatalogue.TestServer`) or when a server test behaves strangely. **Update when** the
engine surprises you there: the behaviour, how it was confirmed, and what we do about it.

`CurioCatalogue.ServerTests` (`docs/tests.md`, Server tests) starts the dedicated server the way `ServerProgram.Main` does
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
