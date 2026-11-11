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

The agent does not run the game and publishes nothing: a test page on a draft mod is pasted by the author, after a "yes" to
its exact text (global rule). Screenshots are the author's, with the steps written by the agent (`tools/studio/README.md`).

## Steps

- [x] **G1. The split on paper.** Decided 11.11.2026 (Decisions, "The split"). Decided with the author, in a session of its own (04.11.2026), before anything is moved:
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
      - whether the split page goes to ModDB before 0.2 or with it;
      - where a modder reads how to add a collection with their own mod (07.11.2026, K1 of `2026-11-07-collections-core.md`):
        the definition asset, and that the mod must be universal and required on the client, with no `"side": "server"` patch
        of a definition, or the client does not know the collection.
      Branch: `docs/mod-description`. After: the plan's own commit. Model: opus. Author: decides.
      Done when: Decisions has the split; G2–G5 are updated where it changed them.
- [x] **G2. Sequences on ModDB: a probe.** Done 11.11.2026 (Decisions, "Sequences on ModDB"); what was not checked went to G3.
      ModDB drops `<style>` (P5, layout test), so a carousel on `:checked`, `:target` or
      `:hover` is impossible, and scripts are not expected to survive (check once). The candidate: a row of frames with
      `overflow-x: auto` and `scroll-snap-type` inline, `scroll-snap-align` on each frame, `<figure>` and `<figcaption>`
      under it; the fallbacks if ModDB drops them: a table of frames with a caption row, or frames one under another. A
      hand-written test page with a sequence of three of today's shots; the author pastes it into a draft mod, made for the probe (its name and
      link stay out of tracked files; desktop
      Chrome and Firefox, the phone). Also how GitHub shows the source of a sequence in G1's format.
      Branch: `docs/mod-description`. After: G1. Model: opus (sonnet did not fit: the probe needs the browser and a judgement
      of the look). Author: approves the test page, pastes it, looks on the phone.
      Done when: Decisions says which markup survives, how it scrolls on the desktop and the phone, and the chosen shape of
      a sequence.
- [ ] **G3. The generator reads the description.** Players' texts moved from the READMEs into the description by G1 (moved,
      not rewritten: R2 rewrites them), README cut down to what G1 left, `README.uk.md` removed (its texts go to
      `description.uk.md`), links to it updated; the sequence extension rendered into G2's markup; the arguments `readme`,
      `readmeUk` renamed `description`, `descriptionUk`; the contract in `docs/releasing.md` (the README contract replaced,
      "run the real generator after a change" moved to the description); `PageGen.Tests` for the new source and the sequences; `CLAUDE.md`'s table
      (README no longer "the source of the ModDB page", a row for the description); `CONTRIBUTING.md` for translators.
      Check: the generated page before and after the move differs only where intended (a diff of `moddb.html`).
      Left by G2: the generated sequence pasted into the draft mod (the author, after a "yes" to the exact text): that ModDB
      keeps `id` on `<figure>` and `href="#…"` links, and the arrows and the swipe in Firefox and on the phone; how GitHub shows
      the source of a `::: sequence` block.
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
- **The split** (the author, 11.11.2026, G1):
  - **`README.md` is the project's README:** the title, the hook, a short early-development warning, the hero image; links to
    the ModDB page and to both description files; "Adding a collection" for modders (L14 of `2026-11-09-album-polish.md`);
    building and tests in a few lines with links to `docs/onboarding.md` and `docs/development.md`; contributing, "AI and
    human work" and the licence in a line or two each. Everything for players moves to the description: "What's inside",
    getting started, collections, gestures, fair play, configuration, compatibility, removing the mod, similar mods, credits.
  - **`README.uk.md` is removed:** a Ukrainian player reads `description.uk.md` (GitHub shows it; ModDB in its accordion);
    README is for developers, and contributions come in English. Two languages, one source each.
  - **Where and in what format:** `docs/moddb/description.md` and `description.uk.md`, beside `template.html`; Markdown read by
    Markdig, as now, with one extension, a `::: sequence` block (frames, a caption under each). GitHub shows the `:::` lines as
    text, but the images and captions stay readable (G2 checks it). Rejected: a plain list of image-and-caption items (the
    generator cannot tell a sequence from an ordinary list); HTML and YAML (rejected in P5).
  - **One source per text:** a text for players lives only in the description; README links to it. Only the licence line
    (`MIT, see LICENSE`) is in both, not a text that can drift. Credits and "AI and human work" go to the description (players
    read them); the Fendragon invite is refreshed in one place per language.
  - **The generator** reads the two description files, `modinfo.json`, `lang/*.json` and the template; never README. The README
    contract moves to the description: the head (the title, the hook as the first paragraph, the warning as one blockquote, one
    hero image; "README-only lines" are gone), `##` keys by the English title, the same sections in both languages, "What's
    inside" (six items in the order of the icons, a paragraph, `###` subsections), no HTML, relative paths, language suffixes on
    screenshots; new: the `::: sequence` block, rendered into G2's markup. P5's shape stays whole: the hero, badges, the row of
    buttons, accordions, the Ukrainian version in one `<details>`, anchors.
  - **Who reads what:** the players' rounds read the generated page (`moddb.preview.html`, as R5 says), the description files
    when a text is discussed. Translators (`CONTRIBUTING.md`) translate `lang/*.json`; a description in another language only
    after an issue, since the page shows `en` and `uk` and another language there needs the generator changed. `CLAUDE.md`'s
    table: a row for the description (read before changing what the player sees; update when such a change lands and every
    release, both languages together; the source of the ModDB page); README's row narrows to building, contributing and the
    modders' part.
  - **The new page goes to ModDB with 0.2** (R6): before that it would carry the same texts (R2 rewrites them) and no
    sequences (G4), for one more paste and approval.
  - **The modders' part** is in `README.md`, English only, as "Adding a collection" (L14); the description says in
    Compatibility, in one line, that other mods can add collections and points to the README on GitHub; `docs/decisions.md`
    keeps the why and points to README.
- **Sequences on ModDB** (the author, 11.11.2026, G2; probed on a draft mod in desktop Chrome):
  - **What survives:** `<figure>`, `<figcaption>`, `<table>`, `<details>`, `id` on a `<div>`, and inline `style` with flex,
    `overflow-x: auto` and `scroll-snap-*`. **Dropped:** `<script>` and `on*` handlers (as `<style>` in P5), so no script carousel.
  - **The shape: one frame per width, arrows on anchors.** A row (`display: flex; overflow-x: auto; scroll-snap-type: x
    mandatory; scrollbar-width: thin`) inside a box of `max-width: 900px`; each frame a `<figure id="seq-<name>-<n>">` with
    `flex: 0 0 100%; scroll-snap-align: start; scroll-margin-top: 60px`; under the image a `<figcaption>` with the caption on the
    left and "← back  n / N  next →" on the right (the last frame: "first"), plain links to the frames' ids. In Chrome a click
    turns the row to that frame and the page barely moves (tried on the real page with the markup put in by hand, not saved).
    On a phone a frame is the screen's width and turns with a swipe.
  - **Rejected:** frames of 85% and at most 640 px (the first candidate): on the desktop two and a half frames in a row, which
    the author did not like, though the phone looked fine; a frame over the full width (~1470 px on the desktop, the whole
    screen, and with no arrows nothing says more frames follow); a table of frames (~110 px each on a phone); frames one under
    another. If ModDB drops the anchors, the same row without arrows, with "n / N." at the start of each caption.

## Where we stopped
