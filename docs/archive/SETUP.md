# Налаштування середовища (один раз на комп'ютер)

Мета: на цьому комп'ютері можна зібрати мод, запустити його в грі з IDE й налагоджувати.

Цільова версія гри: остання стабільна. На момент написання це **1.22.7** (16.08.2026). Перед початком перевірте на сторінці Version history, чи не вийшла новіша.

## Чекліст

### 1. Гра
- [ ] Встановлено Vintage Story останньої стабільної версії.
- [ ] Відома тека встановлення (за замовчуванням `%AppData%\Vintagestory`).
- [ ] Відома тека даних (`%AppData%\VintagestoryData`, там лежать `Mods`, `Saves`, `Logs`).

### 2. .NET
- [ ] Встановлено **.NET 10 SDK** (потрібен з версії гри 1.22.0).
- [ ] `dotnet --list-sdks` показує рядок `10.0.xxx`.

### 3. IDE (одна з)
- [ ] **Rider**: найзвичніший для того, хто прийшов з IntelliJ (Java/Scala).
- [ ] або **Visual Studio Community** з навантаженням ".NET desktop development".
- [ ] або **VS Code** з розширеннями C# і C# Dev Kit.

### 4. Змінна середовища
- [ ] `VINTAGE_STORY` вказує на теку встановлення гри (PowerShell, рівень користувача):
  ```powershell
  [Environment]::SetEnvironmentVariable("VINTAGE_STORY", "$Env:AppData\Vintagestory", "User")
  ```
- [ ] IDE і термінал перезапущено після встановлення змінної.

### 5. Шаблон мода
- [ ] Встановлено шаблони:
  ```powershell
  dotnet new install VintageStory.Mod.Templates
  ```
- [ ] У `C:\Projects\VintageStory\CurioCatalogue` створено проєкт (назва лише малими літерами):
  ```powershell
  dotnet new vsmod --AddSolutionFile -o warehouseledger
  ```
  (`--IncludeVSCode` для VS Code; `--IncludeVSSurvivalMod`, якщо потрібні посилання на ванільний код для навігації.)

### 6. Перший запуск
- [ ] Проєкт збирається без помилок.
- [ ] Запуск з IDE (конфігурація з `Properties/launchSettings.json`) відкриває гру з модом.
- [ ] Мод видно в меню модів; у `client-main.log` є рядок про його завантаження.
- [ ] Працює точка зупину в `StartServerSide` / `StartClientSide`.
- [ ] Створено окремий тестовий світ (творчий режим) і додано параметр запуску, що відкриває його одразу.
- [ ] Збірка CakeBuild створює архів у `Releases/` (наприклад, `warehouseledger_0.1.0.zip`).

### 7. Git (за бажанням)
- [ ] `git init`, `.gitignore` для .NET (bin/, obj/, Releases/), перший коміт.

## Готово, коли
Порожній мод запускається з IDE в тестовому світі, точка зупину спрацьовує, архів релізу збирається.

## Довідка
- Модинг-вікі: Setting up your Development Environment, Preparing For Code Mods, Creating A Code Mod.
- C# для Java/Scala розробника: `var` як у Java, властивості замість геттерів/сеттерів, LINQ як колекції Scala, `record` як case class, `?` для nullable.
