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
Steps of the open plans are taken one per session with the project skill `/step <id>` (`.claude/skills/step/SKILL.md`).

@docs/critical.md

| Document | Read | Update |
|---|---|---|
| `docs/plans/` | at the start of a session: the open plans (with unticked steps) are the current work; several may run side by side | tick items as they are done; a new plan per big feature, named `YYYY-MM-DD-<feature>.md` (start date), with a "Decisions" section for that feature and its own letter for step ids, not used by another open plan; each step fits one session (principle 8 in `docs/architecture.md`) |
| `ROADMAP.md` | when choosing what to do next | an idea comes up; an idea gets its own plan (replace the line with a link) |
| `DESIGN.md` | before changing what the mod does for the player | only after agreeing the change with the author |
| `docs/domain.md` | before touching books, invoices, collections; when a term is unclear | a new term or mechanic appears |
| `docs/architecture.md` | before adding a class, an adapter or a network message | a principle, a layer or a main flow changes |
| `docs/decisions.md` | before adding a class, an adapter, a network message or a kind of state: the section it belongs to | a structural decision is made, with the alternatives and why; feature decisions go to the feature's plan |
| `docs/modding.md` | before changing any code or JSON asset: the sections the change touches | the game surprises you (checked in game or found in vsapi/vssurvivalmod) |
| `docs/development.md` | before building, running, committing | build, launch profiles or the game version update change |
| `docs/tests.md` | before writing or changing a test | a test project, a fixture or a test convention changes |
| `docs/test-server.md` | before changing the test server fixture; when a server test behaves strangely | the engine surprises you inside the tests |
| `docs/releasing.md` | before opening a PR, merging or releasing; the ModDB page | the branch flow, CI or the release steps change |
| `docs/screenshots.md`, `tools/studio/README.md` | before taking screenshots (gallery, README) or item icons; the README is the steps for a person, give it to whoever shoots | the game surprises you while shooting; the README with any change of the studio, the macros or `tools/screenshots.ps1` |
| `docs/review.md` | before a review round (players' or code) of a change or a release | the procedure, a role or a prompt changes; what a round taught |
| `docs/testing/` | before a release: `functional.md` (all cases, accumulates; a smoke test for a patch), `commands.md` (commands to set a test up) and the `upgrade-<from>-to-<to>.md` of this release | functional: with every feature or fixed bug, in the same PR; upgrade: when a change touches stored data, recipes, item codes, config or lang; a new upgrade plan per release (copy the previous one) |
| `docs/guide/README.md`, `README.uk.md` | before changing what the player sees (gestures, recipes, collections, config, compatibility, removal) | such a change lands; both languages together, and `guide-text` with the handbook pages (the in-game guide) in the same PR. The full guide, the source of facts for players |
| `docs/moddb/description.md`, `description.uk.md` | before changing what the ModDB page says (the showcase: what the mod is, what is new, what it does not do, similar mods, credits) | such a change lands; every release (the version in the early-development warning); both languages together. The only source of the ModDB page (contract: `docs/releasing.md`) |
| `README.md` | before changing the build, the way to contribute or the modders' part | such a change lands; every release (the version in the early-development warning) |
| `CONTRIBUTING.md` | before opening a PR or an issue | the way contributions come in changes (PRs, translations, issue templates) |
| `docs/onboarding.md` | setting up a new machine | setup steps change (e.g. a new game version) |
| `docs/archive/` | only for history: the original Ukrainian design, research, reviews, setup and MVP plan | never |

