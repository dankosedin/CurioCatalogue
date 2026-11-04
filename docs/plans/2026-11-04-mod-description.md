# The mod's description: its own document, with sequences of screenshots

Started 04.11.2026. Part of `ROADMAP.md`, 0.2, item 8, started early, since it does not wait for the collections. 0.2 changes
the mod a lot, both in content and in look, so the description of the ModDB page is rewritten anyway; this plan gives it what
the README contract of the previous plan (`2026-11-01-mod-page.md`, P5) cannot:

- **The description becomes a document of its own,** read by the generator, instead of being generated from the two READMEs;
  `README.md` becomes the project's README (what it is, a link to the page, building, contributing, licence).
- **Sequences of screenshots with a caption under each,** a plain "carousel": e.g. how the album takes tapestries from a
  chest in three frames: the inventory and the book before, the hint over the chest, the inventory and the book after.

The texts of 0.2 are written in the release plan (`2026-11-04-release-0.2.md`, R2), the shots of 0.2 taken there (R3); this
plan builds the means and decides the 0.2 shot list.

## How to work on this plan

As in the previous plans: one step per short session with the project skill `/step <id>` (`.claude/skills/step/`). Each step
says **Branch**, **After**, **Model**, **Author** (a decision or in-game work only the author can do; "none" means the session
goes ahead on its own) and **Done when**. A session that stops in the middle writes one line under "Where we stopped".

The agent does not run the game and publishes nothing: a test page on the draft mod is pasted by the author, after a "yes" to
its exact text (global rule). Screenshots are the author's, with the steps written by the agent (`tools/studio/README.md`).

## Steps

- [ ] **G1. The split on paper.** Decided with the author, in a session of its own (04.11.2026), before anything is moved:
      - what stays in `README.md`, and whether `README.uk.md` stays at all (if README is for developers and contributions
        come in English, one README and two descriptions may be enough);
      - where the description lives (e.g. `docs/moddb/description.md` and `description.uk.md` beside the template) and its
        format: Markdown with Markdig extensions (e.g. a `::: sequence` block of frames and captions) is the proposal, since it
        stays readable in a diff and on GitHub and in the players' rounds; HTML and YAML were rejected for README in P5;
      - one source per text: a text for players lives only in the description, README links to the page, so the two languages
        stay the only two sources (P5 rejected four sources that drift apart);
      - what the generator reads and the new contract (replaces the README contract of `docs/development.md`); what of P5's
        shape stays (the hero, badges, accordions, the Ukrainian part in one `<details>`, anchors);
      - who reads what: the players' rounds, translators (`CONTRIBUTING.md`), the rows of `CLAUDE.md`'s table;
      - whether the split page goes to ModDB before 0.2 or with it.
      Branch: `docs/mod-description`. After: the plan's own commit. Model: opus. Author: decides.
      Done when: Decisions has the split; G2–G5 are updated where it changed them.
- [ ] **G2. Sequences on ModDB: a probe.** ModDB drops `<style>` (P5, layout test), so a carousel on `:checked`, `:target` or
      `:hover` is impossible, and scripts are not expected to survive (check once). The candidate: a row of frames with
      `overflow-x: auto` and `scroll-snap-type` inline, `scroll-snap-align` on each frame, `<figure>` and `<figcaption>`
      under it; the fallbacks if ModDB drops them: a table of frames with a caption row, or frames one under another. A
      hand-written test page with a sequence of three of today's shots; the author pastes it into the draft mod (desktop
      Chrome and Firefox, the phone). Also how GitHub shows the source of a sequence in G1's format.
      Branch: `docs/mod-description`. After: G1. Model: sonnet. Author: approves the test page, pastes it, looks on the phone.
      Done when: Decisions says which markup survives, how it scrolls on the desktop and the phone, and the chosen shape of
      a sequence.
- [ ] **G3. The generator reads the description.** Players' texts moved from the READMEs into the description by G1 (moved,
      not rewritten: R2 rewrites them), README cut down to what G1 left; the sequence extension rendered into G2's markup;
      the contract in `docs/development.md`; `PageGen.Tests` for the new source and the sequences; `CLAUDE.md`'s table
      (README no longer "the source of the ModDB page", a row for the description); `CONTRIBUTING.md` for translators.
      Check: the generated page before and after the move differs only where intended (a diff of `moddb.html`).
      Branch: `feature/mod-description`. After: G2. Model: sonnet. Author: looks at the diff of the page.
      Done when: CI generates the page from the description; the tests pass and are mutation-checked on the sequence;
      the docs say where the description lives and how a sequence is written.
- [ ] **G4. Shooting a sequence.** One frame is one state of the studio today (`docs/screenshots.md`); a sequence needs a
      chain of states taken again in every language. Research, then the process:
      - the state before and after, reproducible: copies of the props, the order of the frames, a macro per frame;
      - the hint over the chest is seen only while looking at the block, which brings the selection frame
        (`docs/screenshots.md` forbids it): whether the frame can be hidden (client settings, `.cf`, the engine), or it stays;
      - full frames or crops of a region by `tools/screenshots.ps1` (the inventory and the book are a small part of a
        1920×1080 frame, and a sequence is shown at ~800 px); the files' sizes, three per mechanic;
      - a test sequence, the album taking tapestries from a chest, in `en` and `uk`, put into the description.
      Branch: `docs/mod-description` (the process), `feature/mod-description` if the script changes. After: G3. Model: opus
      (the research), sonnet (the script). Author: shoots the test sequence.
      Done when: `docs/screenshots.md` and `tools/studio/README.md` say how a sequence is shot; the test sequence is on the
      generated page in both languages.
- [ ] **G5. The shot list of 0.2.** Which mechanics get a sequence and which a single frame, with the stamp-album view and the
      new collections; the gallery's order and its first image. Written into `docs/screenshots.md`; R3 shoots by it.
      Branch: `docs/mod-description`. After: A2 of the album view, C7 of the collections design, G4. Model: opus. Author:
      decides.
      Done when: the shot list of 0.2 is in `docs/screenshots.md`, approved.

## Decisions

- **Two plans for item 8** (the author, 04.11.2026): this one, started early, and the release plan
  (`2026-11-04-release-0.2.md`), whose steps wait for the end of 0.2.
- **The description out of the READMEs** (the author, 04.11.2026): 0.2 rewrites the page anyway, and the README contract
  stands in the way of what the page needs (sequences, a layout of its own). The details are G1's, decided in a session of
  their own.
- **Code review** of G3 once at the end of the release (R1), with the rest of 0.2; the players' round on the page in R5.
- **Step letter G** (gallery); A, B, C, F, P, R, S and T are taken.

## Where we stopped
