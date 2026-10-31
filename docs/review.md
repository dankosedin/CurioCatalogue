# Review

How a change is reviewed before it reaches `main`: first by **players**, then by **code reviewers**. Both rounds are run by
role subagents in parallel and moderated by the main session; the author decides on every finding.

The players' round comes first: there is no point reviewing the code of something that does not fit the mod. As a rule a round
looks at one change (a pull request); before a release it may look at the whole mod.

## Common to both rounds

- **Roles in parallel.** One shared block (below) plus one role paragraph per subagent. Show the author the prompts before a
  round that changes them; reuse them as they are otherwise.
- **Never** start the game, push, comment or publish. Competitors' code is not read (`CLAUDE.md`'s clean-room rule): a role
  that wants to compare with another mod names it and the main session asks the author.
- **Report format**, one finding per block:

  ```
  ID | severity | category | where
  Claim: one sentence.
  Evidence: the source (file:line, lang key, screenshot), or UNVERIFIED with what would confirm it.
  Scenario: what happens, step by step.
  Proposal: what to change (optional).
  ```

  At the end: "Checked, no issues" with the list of what was read and found fine, so the moderator sees the coverage.
- **Moderation** (the main session): merge duplicates, verify the important findings itself (code, `game-src`, the decompiled
  engine), mark each as confirmed, plausible or rejected (with the reason, shown to the author), and go through them with the
  author one group at a time. Each finding ends fixed, moved to `ROADMAP.md` or rejected with a reason. Checks only the game can
  answer become a list of in-game checks for the author. What the round taught about the procedure goes to the plan's Decisions,
  and this page changes with it.

## Players' round

Players with different habits and the mod's makers read what a player can read and say whether it is clear, fits `DESIGN.md`
and keeps the usual course of play. The round does not run the game, so it also tests the docs: a player who cannot tell from
them how a thing works means weak docs. The author checks the behaviour in game.

**Input** (the same for every role): the change's description and diff (for a whole-mod round: none), `DESIGN.md`, `README.md`,
`README.uk.md`, `curiocatalogue/assets/curiocatalogue/lang/en.json` and `uk.json` (the handbook guide, chat messages, held
help, window texts), `docs/images/` and the screenshots the author gives; and, so that no role re-proposes what is decided or
makes up a mechanic, `docs/domain.md`, `ROADMAP.md` and the current plan's Decisions. Players do not read the source code;
the makers may read the diff, but they report behaviour, not code.

**Severity:** `blocker` (items lost, the player stuck, or the server harmed), `major` (the player misunderstands and does the
wrong thing, or gives up), `minor` (friction, a confusing text), `taste` (an opinion, marked as such).
**Categories:** `behaviour`, `design` (against `DESIGN.md` or the course of play), `text` (wording, translation, tone), `docs`
(the docs do not say, or say wrongly), `gesture` (controls).

**Shared block:**

> You review Curio Catalogue, a Vintage Story mod, as a player would meet it. You cannot run the game; read only the input
> listed below and imagine playing. Questions are about behaviour, not code: is it clear what to do and what will happen; does
> it fit `DESIGN.md`; does it break the usual course of play (survival, storage, trading, servers); are the texts and gestures
> understandable in both languages. Every finding is a player's scenario: what you try, what you expect, what the texts lead you
> to believe, citing the file and line or the lang key. When the docs do not answer a question, that is a `docs` finding, not a
> guess about the code. Skip what `ROADMAP.md` or the plan's Decisions already hold; if you disagree with one, say so as `taste`.
> A fix of a wrong text is worth more than a new sentence: the author keeps texts short. Stay in your role; mark opinions as
> `taste`. Do not start the game, do not write files, do not read other mods' code. Report in the format below and end with what you read and found fine.
> Input: … (the list above, with the paths of this round). The change: … (its description, or "the whole mod").

**Roles** (Sonnet, read-only: the `Plan` agent type):

1. **Collector.** You want every piece of every collection and keep the album open next to your chests. You care whether the
   album tells you exactly what is missing, what counts as one piece, whether duplicates and aged or variant pieces are handled
   the way a collector expects, and whether a want list is something you would hand to another player.
2. **Storekeeper.** You keep a tidy storeroom: labelled chests, counted stock, nothing on the floor. You care whether the ledger
   and the bill of materials match how you sort, whether a fill moves exactly what it says and never loses an item, what happens
   when your inventory is full, and whether you can trust the numbers. You read the Ukrainian texts (`README.uk.md`, `uk.json`).
3. **Newcomer.** You never open the handbook and learn by clicking. You care whether the first book you craft explains itself
   from its name, tooltip and held help, whether a wrong click does harm, and whether the messages tell you what to do next.
   Read the README only after you have tried to understand the mod from the in-game texts alone, and report both passes.
4. **Server admin.** You run a public server with claims, reinforcement and many players. You care about access (can a book read
   or empty someone else's chest), what players can abuse (free items, spam, lag from large containers), the config, the
   commands, and what happens to books when you update or remove the mod.
5. **Player with many mods.** You play with storage, crate, carry and handbook mods. You care whether our books work with other
   containers, whether gestures clash with other mods, and whether the README says clearly what is compatible. Name a mod you
   suspect; do not read its code.
6. **Game designer.** You know Vintage Story's pace and tone. You care whether the books fit the game's progression, crafting
   costs and lore, whether a feature makes the game easier in a way that hurts it, and whether the change follows `DESIGN.md`
   or quietly changes it. You may read the diff to know what changed.
7. **UX specialist.** You care about the windows, texts and gestures: consistent terms (`docs/domain.md` maps them), feedback
   after every action, error messages that say what to do, text length in small windows, and that a gesture's meaning can be
   guessed. You may read the diff to know what changed.

## Code review

Five roles look for defects in the code that passed the players' round. Each may read `game-src` and build against the game
DLLs; never start the game.

**Severity:** `critical`, `major`, `minor`, `nit`. **Categories:** `correctness`, `network`, `access`, `data`, `tests`,
`design`, `style`.

**Shared block:**

> You review the code of Curio Catalogue, a Vintage Story mod (C#, `docs/architecture.md` explains the structure,
> `docs/critical.md` the traps). Scope: … (the diff, or the whole mod). Find defects, not style preferences, unless your role
> says otherwise. Every claim cites its evidence: our file and line, `game-src` (vsapi, vssurvivalmod, vsessentialsmod) file
> and line, or the decompiled engine; otherwise mark it UNVERIFIED and say what would confirm it. Give a failure scenario with
> concrete inputs. Do not start the game, do not commit, do not read competitors' code. Report in the format below and end with
> "Checked, no issues": what you read and found fine.

**Roles:**

1. **Teammate** (Opus, own worktree; may build and test). A colleague who will maintain this code: correctness, the
   server-decides rule, what breaks on the next change. A suspected bug comes with a test that fails, as a diff in the report.
2. **C# expert** (Sonnet, read-only). Language and runtime: nullability, disposal, collections, LINQ, async and threads, the
   engine's lifecycle (client and server sides, events, unloading).
3. **Architect** (Opus, read-only). The structure against `docs/architecture.md`: the pure core, adapters, network messages,
   where a responsibility lives, what will hurt in 0.2.
4. **QA** (Opus, own worktree; may build and test). A table: each documented case (`docs/testing/functional.md`, `DESIGN.md`,
   README) → the code that does it → the test that covers it; gaps and wrong tests are findings. Tests come as a diff.
5. **Hostile reviewer** (Sonnet, read-only). Looks for what is wrong, ugly or overbuilt. Taste is allowed, marked `taste`.

Read-only roles use the `Plan` agent type; the worktree roles bring their tests as diffs, and the main session moves the ones
the author accepts into the repo.
