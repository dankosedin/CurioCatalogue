# Curio Catalogue — Vintage Story mod

A code mod for Vintage Story (Ukrainian name: Каталог дивовиж). Its selling point is the **album**: a collector's catalogue
of a collection (tapestries first) that knows what is still missing, takes new pieces straight from a chest, and makes
**want lists** for other players. Next to it, books that compare themselves with containers and issue invoices
(set difference / intersection of items): the **ledger** (by type) and the **bill of materials** (by count).

Names (decided 26.10.2026): the mod was "Warehouse Ledger"; the album was "catalog", the want list "collection invoice".
Code still uses `LedgerMode.Types` / `Counts` / `Album` and `ItemLedger` for all three books.

## How these docs work

This file is an index. Each document below says when to read it and when to update it; follow both.
Machine-specific notes and personal preferences live in `CLAUDE.local.md` (git-ignored; template `CLAUDE.local.example.md`).
Steps of the current plan are taken one per session with the project skill `/step <id>` (`.claude/skills/step/SKILL.md`).

@docs/critical.md

| Document | Read | Update |
|---|---|---|
| `docs/plans/` | at the start of a session: the newest plan is the current work | tick items as they are done; a new plan per big feature, named `YYYY-MM-DD-<feature>.md` (start date), with a "Decisions" section for that feature |
| `ROADMAP.md` | when choosing what to do next | an idea comes up; an idea gets its own plan (replace the line with a link) |
| `DESIGN.md` | before changing what the mod does for the player | only after agreeing the change with the author |
| `docs/domain.md` | before touching books, invoices, collections; when a term is unclear | a new term or mechanic appears |
| `docs/architecture.md` | before adding a class, an adapter or a network message | a structural decision is made (Part 1), with the alternatives and why; feature decisions go to the feature's plan |
| `docs/modding.md` | before changing any code or JSON asset | the game surprises you (checked in game or found in vsapi/vssurvivalmod) |
| `docs/development.md` | before building, testing, committing, opening a PR | build, CI or branch flow changes |
| `docs/review.md` | before a review round (players' or code) of a change or a release | the procedure, a role or a prompt changes; what a round taught |
| `docs/testing/` | before a release: `functional.md` (all cases, accumulates), `commands.md` (commands to set a test up) and the `upgrade-<from>-to-<to>.md` of this release | functional: with every feature or fixed bug, in the same PR; upgrade: when a change touches stored data, recipes, item codes, config or lang; a new upgrade plan per release (copy the previous one) |
| `README.md`, `README.uk.md` | before changing what the player sees (gestures, recipes, config, compatibility) | such a change lands; every release (the version in the early-development warning); both languages together. They are the source of the ModDB page |
| `CONTRIBUTING.md` | before opening a PR or an issue | the way contributions come in changes (PRs, translations, issue templates) |
| `docs/onboarding.md` | setting up a new machine | setup steps change (e.g. a new game version) |
| `docs/archive/` | only for history: the original Ukrainian design, research, reviews, setup and MVP plan | never |

