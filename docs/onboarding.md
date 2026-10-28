# Onboarding: setting up a machine

From a clean machine to the mod running in game from the IDE, tests green, and the game's source at hand. Windows is the tested
platform; the build and the core tests should work elsewhere, but nobody has tried yet. Day-to-day work is in `docs/development.md`.

**Update when** a setup step changes, e.g. a new game version or a new tool.

## 1. Tools

- **.NET 10 SDK** (the game needs it since 1.22.0). Check: `dotnet --list-sdks` shows `10.0.x`.
  Windows: `winget install --id Microsoft.DotNet.SDK.10 --exact`.
- **An IDE:** Visual Studio Community 2026 with the ".NET desktop development" workload (the launch profiles below are for it),
  or Rider (closest to IntelliJ), or VS Code with C# Dev Kit.
- **Git.** On Windows, Git for Windows also brings Bash and Perl, which some of our tools use.
- Optional, for reading the closed engine: `dotnet tool install -g ilspycmd` (see `docs/modding.md`).

## 2. The game: a separate copy for development

The target version is `dependencies.game` in `curiocatalogue/modinfo.json` (now **1.22.7**).

Never develop against the copy you play: builds lock files, test worlds pile up, and other mods get in the way. Keep three folders
of your own, for example next to the repo:

```
<root>\CurioCatalogue\           this repo
<root>\VintageStory-Dev\         a copy of the game install (copy the whole install folder, e.g. %AppData%\Vintagestory)
<root>\VintageStory-DevData\     the dev copy's data folder: worlds, mods, logs, config (starts empty)
<root>\VintageStory-ServerData\  a dedicated server's data folder (starts empty; only for server tests)
<root>\game-src\                 the game's open source (step 4)
```

Close the game before copying it. The copy's version must match `modinfo.json`.

## 3. Environment variables

The projects and launch profiles find everything through user environment variables. In PowerShell (replace `<root>` with
your folder):

```powershell
[Environment]::SetEnvironmentVariable("VINTAGE_STORY",             "<root>\VintageStory-Dev",        "User")
[Environment]::SetEnvironmentVariable("VINTAGE_STORY_DATA",        "<root>\VintageStory-DevData",    "User")
[Environment]::SetEnvironmentVariable("VINTAGE_STORY_SERVER_DATA", "<root>\VintageStory-ServerData", "User")
[Environment]::SetEnvironmentVariable("VINTAGE_STORY_SRC",         "<root>\game-src",                "User")
```

| Variable | Used by |
|---|---|
| `VINTAGE_STORY` | the projects (game DLLs) and every launch profile |
| `VINTAGE_STORY_DATA` | launch profiles **Client**, **Client (no mod)**, **Server** |
| `VINTAGE_STORY_SERVER_DATA` | launch profile **Server (dedicated)** |
| `VINTAGE_STORY_SRC` | `tools/check-refs.ps1` |

Restart the IDE and terminals afterwards; they read the environment only at start.

## 4. The game's source

The API and the vanilla mods are open; the engine is not. Clone them once (a partial clone fetches file contents on demand, so it
is small, and it keeps the history, so any game version can be read):

```powershell
cd <root>\game-src
foreach ($r in "vsapi", "vssurvivalmod", "vsessentialsmod") { git clone --filter=blob:none "https://github.com/anegostudios/$r.git" }
```

Each repo has one commit per game version, "Updated to Version X". `tools/check-refs.ps1` reads them at the version in
`modinfo.json` without checking anything out; to browse, `git -C vsapi log --oneline --grep "Updated to Version"`.
The vanilla JSON assets are in the game copy: `<VintageStory-Dev>\assets\survival\`.

## 5. Build and test

```powershell
git clone <this repo> CurioCatalogue
cd CurioCatalogue
dotnet build curiocatalogue.sln
dotnet test curiocatalogue.sln
```

Both test projects must pass. The `NU190x` warnings from CakeBuild are harmless.

## 6. First run

1. Open `curiocatalogue.sln`, choose the launch profile **Client** (`curiocatalogue/Properties/launchSettings.json`), press F5.
2. The game starts with its data in `VINTAGE_STORY_DATA`. Recommended in `<data>\clientsettings.json`: `"gameWindowMode": 0`
   (windowed), so a crash or a breakpoint does not trap the mouse in fullscreen.
3. In the Mods menu, "Curio Catalogue" is listed; `client-main.log` has a line about loading it.
4. Create a creative test world. Our items are in the creative inventory (search "ledger"); the handbook has the guide "Curio Catalogue".
5. A breakpoint in `CurioCatalogueModSystem.Start` is hit.
6. Optional: `.\build.ps1` makes the release zip in `Releases\`.

For a dedicated server test, run the profile **Server (dedicated)** once to create its data folder, then set `"ServerLanguage": "en"`
in its `serverconfig.json` (see `docs/development.md`).

## 7. Working with an AI assistant

`CLAUDE.md` is the index for Claude Code (and a fine starting point for people). Copy `CLAUDE.local.example.md` to
`CLAUDE.local.md` (git-ignored) and describe your machine and how you like to work.

## C# for a Java or Scala developer

`var` as in Java; properties instead of getters and setters; LINQ as Scala collections; `record` as a case class; `?` marks
nullable types; `switch` expressions with patterns as `match`. NSubstitute in tests reads like Mockito.
Modding wiki: "Setting up your Development Environment", "Preparing For Code Mods", "Creating A Code Mod".
