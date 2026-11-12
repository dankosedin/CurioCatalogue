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
- [x] **G6. Showcase and guide.** Decided 12.11.2026 (Decisions, "The showcase and the guide: the split"); G7 added, G4, G5 and
      R2, R5, R6 of the release plan updated. The page is a showcase, the guide lives in the repo (Decisions, "A showcase and a guide"). Decided
      with the author:
      - which sections of the description stay on the page and which go to the guide;
      - the guide's files (e.g. `docs/guide.md` and `guide.uk.md`) and how the showcase links into it;
      - how the generator gets the showcase: two sources, or marked sections of one;
      - the accordion replaced by ModDB's native spoiler where it fits (and whether the native spoiler shows as expected: a probe
        pasted by the author);
      - what the in-game guide (`guide-text`, the handbook pages) shares with the guide in the repo, and who keeps them close.
      Branch: `docs/mod-description`. After: G3. Model: opus. Author: decides.
      Done when: Decisions has the split; G4, G5 and the release plan (R2) are updated where it changed them.
- [ ] **G7. The guide in the repo, the showcase on the page** (G6, 12.11.2026). Two steps, each leaves the build green:
  - [ ] **G7a. The guide.** `docs/guide/README.md` and `README.uk.md`: the sections that leave the page (Decisions, "The showcase
        and the guide: the split") moved from the description, not rewritten (R2 rewrites them); "Sheets and covers" from
        `guide-text` (until now only in the game); "Questions" as an empty heading or left out (the author decides in the step).
        README gets "What may come next" and a link to `docs/guide/`. `CLAUDE.md`'s table: a row for `docs/guide/` (read before
        changing what the player sees; update with such a change, both languages, and `guide-text` with the handbook pages in the
        same PR), the description's row narrowed to the showcase; `docs/review.md`: the players' round reads both guides and
        compares them. The description and the generator stay as they are (the page is cut in G7b).
        Branch: `feature/mod-description`. After: G6. Model: sonnet. Author: looks at the guide on GitHub's preview (or the diff).
        Done when: both guide files are in place with the same sections; every section that leaves the page is in them; README
        links to them.
  - [ ] **G7b. The generator reads the showcase.** The description cut to the showcase (the sections and their order in
        Decisions); new keys `whats-new` and `collections`, the accordions of the moved sections and "Other" gone; the buttons:
        Guide, README, Bugs, Ukrainian (the Ukrainian part: Guide only); the links to the guide and README made from the version
        (`blob/v<version>/…`); one deep link under the collections table to the guide's collections section, with a check that the
        heading exists in both guide files (the build fails otherwise). The contract in `docs/releasing.md`; `PageGen.Tests`
        (mutation-checked: the missing heading, a missing `whats-new`). The page's size before and after written here.
        Branch: `feature/mod-description`. After: G7a. Model: sonnet. Author: looks at the generated page (`moddb.preview.html`).
        Done when: CI generates the showcase; the tests pass and are mutation-checked; the contract says the new shape.
- [ ] **G4. The test page.** What G6 left to a look (Decisions, "The showcase and the guide: the split"), on one generated page
      with today's shots (a test, not the 0.2 shots: those are R3's), in `en` and `uk`; the author pastes it into the draft mod
      (after a "yes" to its exact text) and looks on the desktop and the phone:
      - the collections carousel in two forms: a row of frames under the table (one album page per collection) and each table
        row paired with its frame;
      - which sections are open and which folded;
      - the folded parts: all in ModDB's native spoiler, all in `<details>`, or `<details>` for the sections and the spoiler for the
        Ukrainian part; whether the spoiler opens (the site's script) and how a spoiler inside a spoiler behaves.
      The frames are single shots of the album window, one per collection, by today's process (`docs/screenshots.md`): no chain
      of states. The album taking pieces from a chest is the guide's now (G6); whether it gets a sequence or a GIF there is G5's.
      Branch: `feature/mod-description` (the variants are generator options or a hand-made copy of the page, decided in the step).
      After: G7b. Model: sonnet. Author: shoots the frames, pastes the page, picks.
      Done when: Decisions has the carousel's form, the open and folded sections and the folding element; the generator makes
      the chosen form.
- [ ] **G5. The shot list of 0.2.** The hero, a frame per collection for the carousel (in G4's form), the pictures of the guide
      (whether the album taking pieces from a chest gets a sequence, a GIF or a frame), with the stamp-album view and the new
      collections; the gallery's order and its first image. Written into `docs/screenshots.md`; R3 shoots by it.
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
  mechanic) and G3b's row of frames; the choice after G4's test. Changed by G6 (12.11.2026): the mechanics go to the guide, the
  page's carousel shows the collections (G4).
- **The showcase and the guide: the split** (the author, 12.11.2026, G6):
  - **What the page holds:** what a player needs to decide whether to install the mod, and what is vital: something that breaks
    the game or loses items, with instructions what to do. A compatibility note is there only for a known incompatibility we have
    instructions for. Removing the mod is not: the game deletes the items of any removed mod. "Server and client" is ModDB's
    classification and a badge, not a section.
  - **The page's sections, in this order:** the head (title, hook, badges, the warning, the hero); the buttons (Guide, README,
    Bugs, Ukrainian); **What's new** in this version, a section of its own and first, since in early development the news matter
    more than what the mod has; **What's inside** (six items with icons); **Collections**, the table, the mod's central feature,
    with the carousel; **What it does not do**; **Similar mods**; **Credits** with "AI and human work" (it is part of the thanks);
    **License**; the Ukrainian part. The warning keeps "back up your world"; that books may not carry over is said only when it is a
    fact of that release. What it does not do and Similar mods stay on the page (they answer "is this mod for me"); weighed again
    if the page reaches the limit.
  - **Everything else leaves the page:** to the guide (getting started, gestures, the album taking pieces from a chest, the
    collections' rules, fair play on servers, configuration, compatibility and supported containers, Better Crates, removing the
    mod, sheets and covers, questions), to README ("What may come next", languages, contributing).
  - **The guide:** `docs/guide/README.md` and `README.uk.md` (GitHub shows them on opening the folder; README links there), one
    file per language. Rejected: `GUIDE.md` in the root (tidier in `docs/guide/`, and README leads there); several files per
    language (one is searchable and has one link).
  - **Links:** the page links to README and to the guide; the Ukrainian part only to `README.uk.md` of the guide. The links go to
    the release's tag (`blob/v<version>/…`), made by the generator from the version, so the guide matches the version a player
    downloads; the tag must exist before the page is pasted (R6). One deep link from the showcase: under the collections table to
    the guide's collections section; the generator checks that heading in both guide files.
  - **The generator** reads the description as the showcase and never the guide (besides the heading check): two sources, no marked
    sections in one file. Built in G7.
  - **Open and folded sections, the native spoiler and the carousel's form** are tried on one test page (G4): the spoiler for the
    Ukrainian part stays as decided; for folded sections, the spoiler or `<details>`, by the look.
  - **The in-game guide:** two hand-written texts, never one generated from the other (VTML's item icons, handbook links and keys
    do not fit Markdown, nor the other way). The guide in the repo is the full one and the source of facts; the handbook is shorter,
    for playing (getting started, gestures, the album, want lists, sheets and covers); configuration, compatibility and removal
    live only in the repo. Kept close by `CLAUDE.md`'s table (both in the same PR) and by the players' round reading both
    (`docs/review.md`). A check of the guide's config keys against the config class: a `ROADMAP.md` line, not now.

## Where we stopped
