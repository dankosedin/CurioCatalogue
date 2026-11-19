# Review

How the mod is reviewed: by **players** and by **code reviewers**. Both rounds are run by
role subagents in parallel and moderated by the main session; the author decides on every finding.

The order depends on where the change comes from (decided 04.11.2026):
- **A pull request from someone else:** players first, then code. The docs and what the change does in game tell us whether it
  fits the mod at all; there is no point reviewing the code of something that does not.
- **Our own work:** code first, then players. We start from an idea, make the code, then the docs; the players' round needs
  the docs and does not suit reviewing a bare idea yet.
  - **Code review:** after a feature that changes code, as the last step of its plan, or once at the end of a release over
    what the release changed. Each plan says which.
  - **Players' round:** once per release, after the docs for players (both description files, both guide files, `CHANGELOG.md`, the lang texts) are
    brought up to the release; it looks at the release as a player gets it.

## Common to both rounds

- **A fresh session, by a hand-off.** A round is run and moderated in a session that starts with it, never at the end of the
  session that did the work: there every role's report and every check of the moderation is read on top of the whole work's
  context (on 03.11.2026 the moderation in such a session cost five times K7's, see "What a round costs"). The working session
  stops and hands off: it writes into the plan's review step the scope (the branch and the commit where it left `main`), the
  prompts to reuse or what changes in them, and what to look at closely; then the author clears the context (`/clear`) and the
  round starts from the step. If the moderation itself grows long (many groups, fixes discussed at length), it hands off the
  same way: the reports, the merge and the groups decided so far are saved, and a new session goes on from them.
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

**Input** (the same for every role): the change's description and diff (for a whole-mod round: none), `DESIGN.md`, `docs/moddb/description.md`,
`description.uk.md`, the guide in the repo (`docs/guide/README.md`, `README.uk.md`), `curiocatalogue/assets/curiocatalogue/lang/en.json` and `uk.json` (the handbook guide, chat messages, held
help, window texts), `docs/images/` and the screenshots the author gives; and, so that no role re-proposes what is decided or
makes up a mechanic, `docs/domain.md`, `ROADMAP.md`, the Decisions of the plan the change belongs to and those of earlier
players' rounds (what they rejected; R5 of 0.2 re-proposed a P9 rejection). Players do not read the source code;
the makers may read the diff, but they report behaviour, not code.
The two guides tell one story: the players' round reads the guide in the repo and the in-game guide (`guide-text`, the handbook pages) and compares them; a fact that differs, or is only in the shorter one and should be in both, is a finding.

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
> The mod is young: judge what is there. A collection, a book or a feature that is not in the mod is not a finding
> (`ROADMAP.md` lists what may come), unless its absence makes something that is there misleading.
> Input: … (the list above, with the paths of this round). The change: … (its description, or "the whole mod").

**Roles** (Sonnet, read-only: the `Plan` agent type):

1. **Collector.** You want every piece of every collection and keep the album open next to your chests. You care whether the
   album tells you exactly what is missing, what counts as one piece, whether duplicates and aged or variant pieces are handled
   the way a collector expects, and whether a want list is something you would hand to another player.
2. **Storekeeper.** You keep a tidy storeroom: labelled chests, counted stock, nothing on the floor. You care whether the ledger
   and the bill of materials match how you sort, whether a fill moves exactly what it says and never loses an item, what happens
   when your inventory is full, and whether you can trust the numbers. You read the Ukrainian texts (`description.uk.md`, `uk.json`).
3. **Newcomer.** You never open the handbook and learn by clicking. You care whether the first book you craft explains itself
   from its name, tooltip and held help, whether a wrong click does harm, and whether the messages tell you what to do next.
   In a whole-mod round, first meet the mod on its ModDB page (the generated `moddb.preview.html`) and decide in a minute
   whether to install it: the first screen (title card, hook, the row of buttons), then the whole page. Then the in-game texts
   alone, then the guide, and the page's words against the in-game texts; report each pass.
4. **Server admin.** You run a public server with claims, reinforcement and many players. You care about access (can a book read
   or empty someone else's chest), what players can abuse (free items, spam, lag from large containers), the config, the
   commands, and what happens to books when you update or remove the mod.
5. **Player with many mods.** You play with storage, crate, carry and handbook mods. You care whether our books work with other
   containers, whether gestures clash with other mods, and whether the description says clearly what is compatible. Name a mod you
   suspect; do not read its code.
6. **Game designer.** You know Vintage Story's pace and tone. You care whether the books fit the game's progression, crafting
   costs and lore, whether a feature makes the game easier in a way that hurts it, and whether the change follows `DESIGN.md`
   or quietly changes it. You may read the diff to know what changed.
7. **UX specialist.** You care about the windows, texts and gestures: consistent terms (`docs/domain.md` maps them), feedback
   after every action, error messages that say what to do, text length in small windows, and that a gesture's meaning can be
   guessed. You may read the diff to know what changed.

## Code review

Five roles (with the architect, as decided at R1 of 0.2, one with rights) look for defects in the code that passed the players' round. Each may read `game-src` and build against the game
DLLs; never start the game.

**Severity:** `critical`, `major`, `minor`, `nit`. **Categories:** `correctness`, `network`, `access`, `data`, `tests`,
`design`, `style`.

**Shared block:**

> You review the code of Curio Catalogue, a Vintage Story mod (C#, `docs/architecture.md` explains the structure,
> `docs/critical.md` the traps). Scope: … (the diff, or the whole mod). Find defects, not style preferences, unless your role
> says otherwise. Every claim cites its evidence: our file and line, `game-src` (vsapi, vssurvivalmod, vsessentialsmod) file
> and line, or the decompiled engine; otherwise mark it UNVERIFIED and say what would confirm it. Give a failure scenario with
> concrete inputs. Do not start the game, do not commit, do not read competitors' code. Report in the format below and end with
> "Checked, no issues": what you read and found fine. Where the change brings data the game reads (JSON assets, recipes, lang),
> check it against the game's assets it names, not only against our code. What an admin decides (a collection switched off,
> the config's `Exclude`) is theirs: do not report that the mod fails to protect or show what the admin chose to hide.

**Roles:**

1. **Teammate** (Opus, own worktree; may build and test). A colleague who will maintain this code: correctness, the
   server-decides rule, what breaks on the next change. A suspected bug comes with a test that fails, as a diff in the report.
2. **C# expert** (Sonnet, read-only). Language and runtime: nullability, disposal, collections, LINQ, async and threads, the
   engine's lifecycle (client and server sides, events, unloading).
3. **Architect** (Opus, in its own worktree, with rights; before R1 of 0.2 read-only: see "What the code review of 0.2 taught"). The structure against `docs/architecture.md`: the pure core, adapters, network messages,
   where a responsibility lives; whether `docs/decisions.md` records the structural decisions of the change.
   **Questions from the future:** the moderator names two or three coming items of `ROADMAP.md` (or of the next plans) and asks
   what in this change those items will touch and where it will hurt.
4. **QA** (Opus, own worktree; may build and test). A table: each documented case (`docs/testing/functional.md`, `DESIGN.md`,
   the description) → the code that does it → the test that covers it; gaps and wrong tests are findings. Tests come as a diff.
5. **Hostile reviewer** (Sonnet, read-only). Looks for what is wrong, ugly or overbuilt. Taste is allowed, marked `taste`.

Read-only roles use the `Plan` agent type; the worktree roles bring their tests as diffs, and the main session moves the ones
the author accepts into the repo.

**A change that is mostly tests** (learned in T18 of the server tests plan, 03.11.2026). The scope in the shared block names the defects
that matter in test code: a test that passes for the wrong reason (a wait or an assertion that something else satisfies), flakiness
(timing, test order, state shared through one server), leaks, and CI. The worktree roles check every suspicious wait and assertion by a
mutation (break the mod, see the test stay green, restore): in T18 only they found the tests that passed for the wrong reason, both of
them independently. When part of the branch was already squash-merged into `main`, the scope is the diff from where the branch left `main`.

**The architect's rights** (B6 of the backward-compatibility plan, 04.11.2026). Two architects on the same scope and prompt, one
read-only, one in its own worktree with the worktree roles' paragraph. The one with rights proved more (4 findings with a probe
against none) and cost more (14 min and 183 thousand tokens against 6 min and 129 thousand), but 6 of its 12 findings were the
Teammate's: given the shared block's test defects and "check by a mutation", it spent its rights on them. The one structural
finding of the round (album expectations tied to the game's tapestries, while albums follow their collection) came from the
read-only one and the C# expert; the worktree one missed it. Decided then: the architect stays read-only; the experiment runs once
more at the code review of 0.2, as below.

**Architect with rights (run twice; at R1 of 0.2 it became the only architect, see the end of this section).** Next to the read-only architect, one more on Opus in its own worktree,
with the same role paragraph and the same questions from the future, but not the worktree roles' paragraph and not the test
defects of the "mostly tests" scope. Instead:

> You may build and write probes, but only to prove a structural claim: a probe **simulates the next change** (the next release's
> data, a new feature of the roadmap, a new file in a shared folder, a raised format version) and shows what breaks, or shows that
> a boundary of `docs/architecture.md` does not hold (a type crosses it, a file compiles where it should not). Do not hunt for test
> defects by mutating the mod: the Teammate and QA do that; a finding of that kind is out of your role and is not counted.

The moderation compares the two architects by: findings in the role (a finding in the category `tests`, or one the Teammate or QA
also made, is out of the role and not counted), found by it alone, with a proof, the questions from the future answered (by
reasoning or by a probe), and the cost (time and tokens from the task notification).

**What the code review of the core taught** (K7 of `2026-11-07-collections-core.md`, 07.11.2026). Five roles on the diff from where the
branch left `main`, about 35 findings after the merge; costs 157–260 thousand tokens and 5–18 minutes a role (QA the slowest).
- **Agreement is the signal.** R1 (a definition that throws takes every collection down) was found by all five roles independently,
  and the Teammate's and QA's tests for it failed on the old code, so the fix started from proof.
- **The moderator checks reachability before the author sees a finding.** M1 (an album of 0.1 placed on the wrong level) and M2 (an
  album with no snapshot) were true in the code and unreachable with today's data (M1 was fixed together with the reachable M3,
  M2 rejected); the moderation said so and the decision was quick. Verify each claim against the code and say what can reach it.
- **Doc findings are real findings.** Q6 and Q7 came from QA's table (documented case → code → test): the README still said the
  config is not synced after K6 had synced it, and rows of `functional.md` sat above its heading.
- **A flaky test needs its cause, not a mention.** `BookEdgeTests.L1` flaked in K3, K4 and K7; the cause (a dropped list falls out of
  the cube `OnGroundNear` searches) was found by the author from the test world's layout (T4). Ask a role that meets a flake to find
  the cause, or say so in the prompt.
- **Its second run** was R1 of 0.2, below.

**What the code review of 0.2 taught** (R1 of `2026-11-04-release-0.2.md`, 16–17.11.2026). Six roles (the five and the architect
with rights) on `ac8602c..e9677a3` and the page generator; eight groups after the merge; one major, no critical.
- **The architects compared (the second run of the experiment).** Read-only: 264 thousand tokens, 7.9 minutes, 6 findings in
  role (2 of them its alone), no proof. With rights: 245 thousand, 8.6 minutes, 5 in role (2 alone), one probe (the planner:
  "5 planks" from 3 oak and 2 pine); it kept to its role this time, with the paragraph above. The proof is the difference worth
  having; the findings in role are the same count. **Decided: one architect, with rights, instead of the read-only one.**
- **Check data against the game's assets.** Only the Hostile reviewer did (H1: ilmenite in kimberlite never generates, because the
  54 exclusions were compared with `placeblock` and the definition's `inblock` lacks kimberlite); no other role looked at
  the JSON the game reads. The shared block now asks every role to check data against the assets it names.
- **Agreement still signals.** The GUI-scale bug (names and counts kept at the old scale) was found by all six roles.
- **A finding that is right, and wrong for the context** (W1, G4): an album whose collection is switched off shows only what was
  placed, and what the admin's `Exclude` removed is hidden. The switch-off and `Exclude` are the admin's decisions; showing or
  protecting what they hide would decide for the admin. Rejected, and the rule is written where reviewers read it (below).

**What a round costs** (measured 14.11.2026 from the session logs at API prices, the code reviews of 28.10–07.11). A code review
round costs $13–19: the five roles $6–14, the moderation $2–10; a players' round of Sonnet roles about $1. That is about 8% of a
week of work, the price of two or three `/step` sessions, so the review is not where a week's cost goes. In K7: QA (Opus) $5.0,
Teammate (Opus) $3.8, Architect (Opus) $2.4, C# expert (Sonnet) $1.7, Hostile (Sonnet) $1.0.
- **The start is cheap, the walk is not.** A role starts at 19–40 thousand tokens (the worktree roles load `CLAUDE.md`), $0.1–0.3;
  the rest is how long it reads the code and on which model. Fewer roles save little; a narrower scope and Sonnet where the role
  allows save more.
- **Each role earned its place in K7**, except the C# expert, whose findings were mostly the others' (its own: C2, C6). Its
  agreement still counted (R1 by all five). Keep it while it costs under $2; drop it first if a round must be cheaper.
- **Moderate in a fresh session.** The moderation cost $1.7 in K7 (a fresh session, the reports saved outside it) and $9.6 on
  03.11 (in a long session at 280 thousand tokens of context). Hence the hand-off rule at the top of this page.
