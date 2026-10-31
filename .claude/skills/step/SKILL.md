---
name: step
description: Work on one step of the current plan in docs/plans/ in a short, self-contained session. Use when the author types /step <id> (e.g. /step S4) or asks to take the next step of the plan.
argument-hint: "[step id, e.g. S4]"
---

# One step of the plan

You take exactly one step of the current plan and carry it to its "Done when", or stop cleanly and say where. The session
may run on Sonnet without the context of earlier sessions: the plan and the docs are the source, not memory of past talks.

## 1. Find the step

1. `git status` and `git branch --show-current`. If the tree is not clean, stop and ask.
2. The current plan is the newest file in `docs/plans/` (by the date in its name). Read its "How to work on this plan",
   the step, "Decisions" and "Where we stopped".
3. No id given (`$ARGUMENTS` empty): propose the first unticked step whose `After:` is satisfied, and wait for a yes.
4. Check the step's fields:
   - `After:` not satisfied (a step unticked, a branch not merged into `main`): say so and stop.
   - `Model: opus` while you are not Opus: say so and ask whether to go on anyway or to switch (`/model opus`).
   - `Author:` names a decision: ask it first, before any change. Never guess a decision the author owns.

## 2. Prepare

- Read the documents `CLAUDE.md` lists for this kind of work, always following their "Read" column: `docs/modding.md` before code
  or JSON assets, `docs/development.md` before building, testing or committing, `docs/domain.md` for books, invoices and
  collections, `docs/architecture.md` before a new class, adapter or message, `DESIGN.md` before a change the player sees.
- Branch: the one the step names. A new branch starts from an up-to-date `main` (`git fetch`, then branch from `origin/main`);
  never commit on `main`. An existing branch named by the step: check it out and rebase it on `origin/main` only if the step says so.
- Before touching code, say in two or three lines what you are going to change.

## 3. Work

- Only this step. A new idea, a bug or a smell outside the step: mention it at the end as a proposed line for `ROADMAP.md` or the
  plan; do not fix it now.
- Follow `docs/critical.md` (always loaded). Tests come with code changes; a test meant to catch a mistake is checked by a mutation
  (break the thing, see the test fail, restore).
- Build and test as `docs/development.md` says, and report the real result. A red test is reported with its output, not hidden.
- Something only the game can show: write the exact in-game check for the author instead of claiming it works.
- Another mod's code: integration targets may be read; competitors (ask if unsure) only through a subagent that returns a summary
  and never quotes code.

## 4. Commit

- Show the author a short summary of the diff (files and what changed in each). For a doc (`DESIGN.md`, `ROADMAP.md`, a plan,
  README) give the text or its summary and wait.
- Commit only after the author's "комічь" (or another clear yes). Commit message in English, ending with the attribution lines the
  harness gives for commits. New commits, never an amend of a reviewed commit.
- Never push, open a pull request, comment or publish yourself. Prepare the exact commands for the author to run with `!`: the
  pull request text in a file in the scratchpad, passed as `--body-file`; `gh` is at `/c/Program Files/GitHub CLI/gh.exe`;
  `Co-Authored-By` lines only in commits, never in the pull request text. Show the full text first.

## 5. Close the step

In the same branch, with the step's commit or as the last commit after "комічь":
- tick what is done in the plan (sub-items too); a decision taken in the session goes into the plan's "Decisions", dated;
- not finished: one dated line under "Where we stopped": what is done, what is next, what waits for whom;
- the updates the `CLAUDE.md` table asks for (e.g. `docs/testing/functional.md` with every fixed bug, `CHANGELOG.md`, both READMEs).

Then tell the author, briefly and in Ukrainian: what is done, what is left, the in-game checks for them, the commands for them
to run, and proposed roadmap lines if any. End with the next step: its id, its `Model:` (so the author can `/model` before
`/step`) and its `After:` (whether it waits for a merge), read from the plan.
