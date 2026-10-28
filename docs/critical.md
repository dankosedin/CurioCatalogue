# Critical rules

Rules whose violation fails silently and costs a lot: nothing crashes, but the game, the network or the player's items break.
Imported into every session from `CLAUDE.md`. Details and the reasons live in `docs/modding.md` and `docs/architecture.md`.

**Update when** a mistake of this kind happens (or nearly happens) a second time. Keep the list short; move a rule out when it stops being a trap.

- **Server decides, all or nothing.** Every state change (invoices, transfers, book updates) happens on the server and either
  completes fully or changes nothing. Requests from the client are re-checked on the server (hands, reach, access).
- **Access is our job.** The item handles the click before the block, so vanilla lock checks do not run. Check claims
  (`world.Claims.TestAccess`) and reinforcement (`IsLockedForInteract`) on the server.
- **Clicks that are not ours go to `base.OnHeldInteractStart`.** A bare `return` silently breaks the item's behaviors (e.g. putting it on the ground).
- **Logger:** never pass dynamic text as the format string: `api.Logger.Notification("{0}", text)`. Text with `{}` throws and the line is lost.
- **Core stays pure:** `CurioCatalogue.Core` never references game DLLs.
- **Recipes:** every vanilla code needs `game:` (also in `requiresContent`); no comments inside a recipe array. After a change, check
  `server-main.log` for "Failed to parse grid recipe" and "cannot be resolved".
- **Lang files:** every `lang/*.json` has exactly the keys of `lang/en.json` (`AssetsTests`); write `{` and `}` as `{{` `}}` in texts.
- **Item names on the server** come in the server's language, not the player's: use `PlayerMessages.ItemName` or let the client name the stacks.
- **Mocks carry a source:** every mocked engine behaviour cites vsapi/vssurvivalmod file and line, a section of the docs, or "checked in game <date>".
