# Curio Catalogue

A Vintage Story mod about collecting and keeping account of things, in the spirit of an old counting house.

> **Early development.** This is 0.1.1, still a minimal version of the mod (an MVP). **Back up your world** before installing
> it or a new version of it.

![The album window: 6 of 88 pieces collected, the missing ones in a grid](docs/images/album-window.en.png)

## For players

- Download: [Vintage Story ModDB](https://mods.vintagestory.at/curiocatalogue).
- What the mod does, how to start, gestures, configuration and compatibility: the description,
  [in English](docs/moddb/description.md) and [українською](docs/moddb/description.uk.md). The ModDB page is generated from it.

## Building and tests

- Set up a machine with [docs/onboarding.md](docs/onboarding.md): .NET 10 SDK, a separate copy of the game, environment variables.
- Build: `dotnet build curiocatalogue.sln`; tests: `dotnet test curiocatalogue.sln`. Day-to-day work, launch profiles and tools:
  [docs/development.md](docs/development.md).
- [CLAUDE.md](CLAUDE.md) is the map of the documentation: for each document, when to read it and when to update it.

## Contributing

Bug reports, ideas, code and translations are welcome: see [CONTRIBUTING.md](CONTRIBUTING.md). What may come next:
[ROADMAP.md](ROADMAP.md). How the mod is made, by a human and by AI: "AI and human work" in the
[description](docs/moddb/description.md#ai-and-human-work).

## License

MIT, except a few files derived from Vintage Story: see [LICENSE](LICENSE).
