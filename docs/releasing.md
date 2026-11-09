# Branches, PRs and releases

How changes reach `main` and how a release is made, the ModDB page included.

**Read** before opening a pull request, merging or releasing. **Update when** the branch flow, CI or the release steps change.

The repository: github.com/dankosedin/CurioCatalogue. Its rulesets enforce the rules below for `main` and `release`.

- Work on `feature/<name>`. `main` changes only through a pull request: squash merge only, linear history, the branch must be
  up to date with `main`, CI must pass. No force pushes, no deletion.
- The squash commit on `main` takes the PR's title (plus ` (#N)`) and description, so write them as a commit message. Its author
  is whoever merges, under their GitHub profile name. GitHub itself appends the `Co-authored-by` lines of the branch's commits:
  keep such trailers in the commits, not in the PR description, or they appear twice.
- Reviews: GitHub does not let anyone approve their own pull request, so the `main` ruleset requires 0 approvals while one
  person has write access; that person reviews before merging. With a second one, set 1 required approval in the `main` ruleset.
  How a change is reviewed (a players' round and a code review, both by role subagents; the order depends on who made the
  change): `docs/review.md`.
- CI (GitHub Actions) runs the core tests first (they need no game), then builds against the game server and runs all four test
  projects (`dotnet test curiocatalogue.sln`, including the server and the compatibility tests, which start a real server on the
  Linux runner); on `feature/*`, `main`, `release` and on every PR (on the merge result).
  After the build it runs the generator `ModDbPage` (`--ref` is the commit): a README that breaks the contract fails CI; the page is
  uploaded as the artifact `moddb-page` (14 days), so it can be looked at before a merge.
  It downloads the game's server archive for the version in `modinfo.json` to get the DLLs, cached between runs (saved right after the download, so a red run does not download it again).
  - **The pinned mods** (`CurioCatalogue.CompatTests/mods/`) are cached with the key `pinned-mods-<hash of mods.json>`, saved also when a test fails; a changed pin
    downloads again, a wrong hash still fails.
  - **When a test fails**, the step "Server logs of a failed run" uploads the artifact `server-logs` (14 days): the fixture copies the
    `Logs` of each server into the folder named by the environment variable `CURIOCATALOGUE_KEEP_LOGS` (set only in CI) before it
    deletes the temp world; a start that failed or a crash keeps the whole data folder, whose `Logs` are uploaded too.
  - **How long:** locally the four projects take about 1.5 minutes (server tests 70 s, compatibility tests 7 s, measured 03.11.2026; the server starts in about 10 s); on GitHub (ubuntu, game cached) the Test step took 78 s
    and the whole job about 2 minutes (first run, 02.11.2026); the Test step prints each project's duration.
- **Release:** `release` only moves forward to a commit of `main` (`git push origin main:release`, by the author). CI then builds,
  tests, makes the zip (CakeBuild), tags `vX.Y.Z` from `modinfo.json` (fails if the tag exists) and creates a GitHub Release
  with the zip and `moddb.html` (the page for that tag, made by `ModDbPage` in the job); the release notes are that version's section of `CHANGELOG.md`. The author uploads the same zip and notes to the
  ModDB by hand (it has no release API). This is the job `release` in `ci.yml`, after `build`.
- **ModDB** (https://mods.vintagestory.at/curiocatalogue) takes HTML, not Markdown: paste the generated `moddb.html` in the editor's source
  code mode (`<>`). The description is generated from the two READMEs (below): the English page with the whole Ukrainian version
  in one accordion; images from raw GitHub at the release tag (or a SHA), relative links made absolute. The release's changelog is
  the `CHANGELOG.md` section. Screenshots and their size limits: `docs/screenshots.md`.
  - **The gallery** (uploaded by the author on the mod's page, not generated): the first image is the title card (`title.en.jpg`,
    the gallery's first place decides what a player sees without scrolling); then, in this order, `album-window`, `album-chest`,
    `wantlist`, `ledger-intake`, `materials`, `first-ledger` (the shot list of `docs/screenshots.md`); the `en` files (the `uk` ones are used by `README.uk.md`).
  - **The generator** (shape: "The generator's shape" in `docs/plans/2026-11-01-mod-page.md`): the CakeBuild task `ModDbPage`,
    `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ModDbPage` (`.\build.ps1 --target=ModDbPage` does the same where
    the execution policy allows scripts). It reads `README.md`, `README.uk.md`, `modinfo.json`, `lang/*.json` and the template
    `docs/moddb/template.html` (the page's look: its CSS in one `<style>`, Scriban); the code is `CurioCatalogue.PageGen`.
    It writes `Releases/moddb.html` (the file to paste) and `Releases/moddb.preview.html` (the same with a charset, to look at in
    a browser). It fails with the list of every contract break, and when the template's CSS cannot be inlined (`@media`,
    `:hover`, pseudo-elements: ModDB drops `<style>`); the output has no `<style>`, no comments, no relative links.
    Arguments (`--name=value`):
    - `ref`: the tag or SHA that images and links point to on GitHub; default `v<version>`, so before a release the images 404:
      a page between releases uses the SHA of the merge commit on `main`;
    - `moddbLinks` (default `true`, the author chose it 03.11.2026): Source and Report a bug in the row of buttons; `false` leaves them out;
    - `out`: the output file; `rawBase`, `blobBase`: other places for images and links, e.g.
      `--rawBase=file:///C:/Projects/VintageStory/CurioCatalogue/` to look at a page before the files are on GitHub;
      `readme`, `readmeUk`: other READMEs.
  - **The README contract** (for the generated page, `docs/plans/2026-11-01-mod-page.md`; the generator fails with a clear
    message when it is broken). Both READMEs keep it, together:
    - **The head** (everything before the first `##`): the `#` title, the hook as the first paragraph, any README-only lines
      (dropped from the page), the early-development warning as one blockquote, one image (the hero's).
    - **Sections `##`:** each has a key, the slug of its English title (`getting-started`); the template knows every key, and an
      unknown one is an error, so a new section is not lost silently. Renaming an English title changes its key: change the
      template with it.
    - **Both languages:** the same sections in the same order (Ukrainian ones are matched to English ones by position), the same
      `###` in "What's inside".
    - **"What's inside":** a list of exactly six items, one thing each, in the order of the icons (album, want list, ledger,
      bill of materials, intake list, pick list), then a paragraph, then `###` subsections (each a nested accordion).
    - **No HTML** in README; images and links by relative paths; screenshots with the language suffix (`*.en.png`, `*.uk.png`).
    - **After any change to a README** (a new, renamed or removed `##` section above all), run the real generator before the
      commit: `dotnet run --project CakeBuild/CakeBuild.csproj -- --target=ModDbPage --ref=HEAD`. `PageGen.Tests` use sample
      READMEs, so only this step (and the `ModDB page` step of CI) sees the real ones. A new section also goes into
      `Contract.cs` (`AccordionKeys` or `OtherKeys`, in README order) and into the sample of `ModDbPageTests.cs`.
- **Changelog:** a version's heading is `## X.Y.Z (unreleased)` while it is being made and `## X.Y.Z (YYYY-MM-DD)` from the
  release day; the release job fails on a missing, undated or empty section. Before a release: the version in `modinfo.json`,
  the date in `CHANGELOG.md` and the version in both READMEs' early-development warning. A fresh Fendragon invite in both READMEs'
  thanks (an invite expires in 30 days); the author asks for it on the server.
- **Before a release**, with the release zip:
  - a minor release (0.x.0), or a patch that changes stored data, network messages or access checks: all of
    `docs/testing/functional.md` and the release's `docs/testing/upgrade-<from>-to-<to>.md`, in single player and on the
    dedicated server;
  - any other patch release (0.x.y): the smoke test in `functional.md`; its new cases were checked in game in their steps.
  The results go into the release PR's description (`functional.md`, Results).
- Release zip by hand: `build.ps1` (CakeBuild) → `Releases\` (it replaces only its own folder and zip there).
