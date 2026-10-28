# Contributing to Curio Catalogue

Thanks for wanting to help. Bug reports, ideas, code and translations are all welcome.

## Before you start

- Set up your machine with `docs/onboarding.md`.
- `CLAUDE.md` is the map of the documentation: for each document, when to read it and when to update it. It serves people and
  AI agents alike.
- `docs/critical.md` lists the traps that break the game, the network or the player's items without any error. Read it before
  your first change.

## Pull requests

- Fork the repository, make a branch `feature/<name>` and open a pull request into `main`. No issue is needed first.
- What the mod does for the player is described in `DESIGN.md`, and it changes only after agreeing with the author. If your change
  does something new for the player, say what and why in the pull request; `ROADMAP.md` lists the ideas we already want.
- One logical change per pull request. It is squash-merged, so its title becomes the commit message: English, imperative, short.
- Build and test as in `docs/development.md`; `dotnet test curiocatalogue.sln` must pass. CI runs the same on your pull request.
  For a first-time contributor GitHub waits until a maintainer approves the run.
- Keep the docs true: update what `CLAUDE.md` says to update when you change something.
- Player-facing texts go into every file in `curiocatalogue/assets/curiocatalogue/lang/`.

## Translations

- Copy `lang/en.json` to `lang/<code>.json`, where `<code>` is the game's language code (the file names in the game's
  `assets/game/lang/`).
- Translate every text: the tests require the same keys as `en.json`.
- Keep the placeholders `{0}`, `{1}`… and write a literal brace as `{{` or `}}`.
- Check the result in game: the texts are long in places (the handbook guide) and the windows are small.

## Using AI

Welcome; the project is built with it. The same rules hold as for code written by hand: the tests pass, every mocked engine
behaviour cites its source (`docs/development.md`, Writing tests), and decisions go into the docs, not only into a chat with an agent.

## Bugs and ideas

Open an issue with the matching template. For a bug, the game and mod versions, other mods and the logs
(`VintagestoryData/Logs/`: `server-main.log`, `client-main.log`) save a lot of guessing. Issues in English are preferred;
Ukrainian is fine too.

## Licence

By contributing you agree that your contribution is licensed under the MIT License of this repository (`LICENSE`).
