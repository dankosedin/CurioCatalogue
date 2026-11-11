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
- [x] **G3. The generator reads the description.** Split in two (the author, 11.11.2026): G3a the move, G3b the sequences.
  - [x] **G3a. The move.** Done 11.11.2026. Players' texts moved from the READMEs into `docs/moddb/description.md` and
        `description.uk.md` (moved, not rewritten: R2 rewrites them; image paths made relative to `docs/moddb/`), README cut
        down to what G1 left (with no "Adding a collection" yet: L14 writes it), `README.uk.md` removed, links to it updated
        (`docs/review.md`, `docs/screenshots.md`, `functional.md`, the `step` skill); the generator reads the description
        (`Description.cs`, relative paths read from the description's folder, the head allows nothing but its four parts); the
        arguments `readme`, `readmeUk` renamed `description`, `descriptionUk`; the contract in `docs/releasing.md`;
        `PageGen.Tests` (mutation-checked: the head check, a path above the root, the folder of the paths); `CLAUDE.md`'s table;
        `CONTRIBUTING.md` for translators; `docs/development.md`'s layout.
        Check: `moddb.html` generated before and after the move (`--ref=HEAD`) is the same, byte for byte.
  - [x] **G3b. Sequences.** Done 11.11.2026 (Decisions, "How a sequence is written"; the paste in "Sequences on ModDB";
        the size it found is G3c). The `::: sequence` extension (frames, a caption under each) rendered into G2's markup; the
        contract in `docs/releasing.md` says how a sequence is written; `PageGen.Tests` for the sequences.
        Left by G2: the generated sequence pasted into the draft mod (the author, after a "yes" to the exact text): that ModDB
        keeps `id` on `<figure>` and `href="#…"` links, and the arrows and the swipe in Firefox and on the phone; how GitHub
        shows the source of a `::: sequence` block.
        Branch: `feature/mod-description`. After: G3a. Model: sonnet. Author: approves the test paste, pastes it, looks on the phone.
        Done when: the tests pass and are mutation-checked on the sequence; the docs say how a sequence is written.
      Branch: `feature/mod-description` (stacked on `docs/mod-description`, the author, 11.11.2026). After: G2. Model: sonnet.
      Author: looks at the diff of the page.
      Done when: CI generates the page from the description (G3a); G3b done.
- [x] **G3c. The page within ModDB's size.** ModDB refuses a description over about 65 KB ("Excessive size", G3b, 11.11.2026):
      it counts bytes, not characters (a test page of 66 176 bytes and 58 841 characters was refused), and today's page
      without a sequence is already 60 634 bytes: 23 KB of it inline CSS, 33.6 KB the Ukrainian part (Cyrillic is two
      bytes a letter); a sequence of three frames costs ~5.5 KB in both languages. Without a change to what the player sees:
      - the generator fails above a limit with a reserve (e.g. 64 000 bytes) and prints the size, so CI catches it before a paste;
      - leaner inline CSS: no space after `:` in the styles, no style that repeats the default (`margin` on `li` and `p`),
        a lighter `code`;
      - a lighter sequence: no arrows (the author, 11.11.2026: the scroll bar and the swipe are enough), so no `id` on the
        frames and no links; whether "n / N" stays, the author decides in the step.
      What is left after it decides how much room 0.2 has; shortening the Ukrainian part on the page (the rest by a link to
      `description.uk.md` on GitHub) or moving it off the page is G5's decision, once the shot list is known.
      Branch: `feature/mod-description`. After: G3. Model: sonnet. Author: decides "n / N"; pastes the generated page into
      the draft mod (after a "yes" to its text).
      Done when: the generator checks the size (a test, mutation-checked); the real page is measured before and after and the
      sizes are written here; the contract in `docs/releasing.md` names the limit and the sequence without arrows.
- [ ] **G6. Showcase and guide.** The page is a showcase, the guide lives in the repo (Decisions, "A showcase and a guide"). Decided
      with the author:
      - which sections of the description stay on the page and which go to the guide;
      - the guide's files (e.g. `docs/guide.md` and `guide.uk.md`) and how the showcase links into it;
      - how the generator gets the showcase: two sources, or marked sections of one;
      - the accordion replaced by ModDB's native spoiler where it fits (and whether the native spoiler shows as expected: a probe
        pasted by the author);
      - what the in-game guide (`guide-text`, the handbook pages) shares with the guide in the repo, and who keeps them close.
      Branch: `docs/mod-description`. After: G3. Model: opus. Author: decides.
      Done when: Decisions has the split; G4, G5 and the release plan (R2) are updated where it changed them.
- [ ] **G4. Shooting a sequence.** One frame is one state of the studio today (`docs/screenshots.md`); a sequence needs a
      chain of states taken again in every language. Both forms are tried (Decisions, "Sequences: both tried"): an animated GIF
      and a row of frames. Research, then the process:
      - the state before and after, reproducible: copies of the props, the order of the frames, a macro per frame;
      - the hint over the chest is seen only while looking at the block, which brings the selection frame
        (`docs/screenshots.md` forbids it): whether the frame can be hidden (client settings, `.cf`, the engine), or it stays;
      - full frames or crops of a region by `tools/screenshots.ps1` (the inventory and the book are a small part of a
        1920×1080 frame, and a sequence is shown at ~800 px); the files' sizes, three per mechanic;
      - a GIF: how it is recorded and made (a video of the client, then frames to GIF), its size and quality, one per language;
      - a test sequence, the album taking tapestries from a chest, in `en` and `uk`, put into the description in both forms;
        the author compares how they look on the page and on the phone, and picks.
      Branch: `docs/mod-description` (the process), `feature/mod-description` if the script changes. After: G6. Model: opus
      (the research), sonnet (the script). Author: shoots the test sequence.
      Done when: `docs/screenshots.md` and `tools/studio/README.md` say how a sequence is shot; the test sequence is on the
      generated page in both languages.
- [ ] **G5. The shot list of 0.2.** Which mechanics get a sequence and which a single frame, with the stamp-album view and the
      new collections; the gallery's order and its first image. Written into `docs/screenshots.md`; R3 shoots by it.
      Branch: `docs/mod-description`. After: A2 of the album view, C7 of the collections design, G6, G4. Model: opus. Author:
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
  - **The generated sequence pasted** (the author, 11.11.2026, G3b): the card with the sequences alone (the whole test page
    was refused for its size, G3c); it looks right and turns with a swipe. The arrows go (G3c): the scroll bar and the swipe
    are enough.

- **G3 in two steps, on a stacked branch** (the author, 11.11.2026): G3a moves the texts and points the generator at the
  description, G3b adds the sequences; `feature/mod-description` starts from `docs/mod-description` (G1 and G2 are not on `main`
  yet), so its PR carries G1–G3 or goes after the docs PR.
- **The description's paths are relative to `docs/moddb/`** (G3a, 11.11.2026), as GitHub reads them, so the images show on
  GitHub too; the generator resolves them from the description's folder. The head is strict: a block other than the title,
  the hook, the warning and the hero image is an error (the README-only lines it used to drop are gone with README).
- **How a sequence is written** (G3b, 11.11.2026; the contract in `docs/releasing.md`): a frame is an image paragraph and a
  caption paragraph under it, not both in one paragraph (on GitHub the caption would run on from the image's corner); the
  name after `sequence` makes the ids, `seq-<name>-<n>` and `seq-uk-<name>-<n>` in the Ukrainian part (as `cc-uk-`); the first
  frame has no "back"; the words of the arrows live in `Sequence.cs` (en, uk), since a sequence is rendered before the
  template; both languages have the same sequences, checked; only the block form of Markdig's custom containers is on, not
  the inline `::text::`. The generator's argument for another English description is `descriptionEn`: `description` is a
  flag of Cake itself, so G3a's argument could not be used.

- **The page within ModDB's size** (the author decided "n / N" away, 11.11.2026, G3c): no arrows, no "n / N", no `id` on the
  frames (so no `seq-` ids and no `Words` of the arrows in `Sequence.cs`); the caption alone under the image. The generator fails
  above `Contract.MaxPageBytes` = 64 000 bytes (`PageInput.MaxBytes` for tests). The saving came from the generator, not from the
  template: `ModDbPage.Shorten` writes the inline styles short (no space after `:`, `margin`/`padding` in the shortest form
  that means the same): 2.6 KB, less than the plan guessed (the 23 KB of inline CSS is mostly the styles repeating on 500+
  elements, not the spaces); `code` lost its `border-radius`. `li`/`p` margins stay: they are not the default, the look would change.
  Then the collections table got borders and padding (`.body table`, `th`, `td`; the live page showed it bare, 11.11.2026): the page is 60 424 bytes, +2.4 KB, since every cell carries its style. Room left for 0.2: ~3.5 KB, i.e. one sequence of three frames in both languages; more needs G5's decision (the Ukrainian part
  shortened on the page, or moved off it).
- **A showcase and a guide** (the author, 11.11.2026; replaces "shortened or moved off" above, the details are G6's): the ModDB
  page is a showcase, what draws a player in (what the mod does, the main mechanics in pictures, compatibility, the config in
  short); the guide, what explains (every gesture, recipe, collection, the config in full, questions), lives in the repo as
  Markdown files, versioned and reviewed with the mod (the GitHub wiki was rejected: a repository of its own, edited on the web,
  outside the PRs). The guide in the repo and the in-game guide tell close stories. Why: ModDB refuses a page over ~64 KiB,
  and the page is 60 KB before 0.2's sequences; among 9 080 mods (the ModDB cache, 02.11.2026) the median description is
  1.1 KB, 6.7 KB in the top 300 by downloads, and the few near the limit are encyclopedic mods.
- **The Ukrainian showcase in ModDB's native spoiler** (the author, 11.11.2026): `div.spoiler` with `spoiler-toggle` and
  `spoiler-text`, styled by the site's CSS, so it costs no inline styles; 1 006 mods use it, 130 of the top 300.
- **Sequences: both tried** (the author, 11.11.2026): an animated GIF (one `<img>` on the page, as some popular mods show each
  mechanic) and G3b's row of frames; the choice after G4's test.

## Where we stopped
