# Дослідження (етап 1)

Версія гри: **1.22.7**. Джерела: відкритий код `anegostudios/vsapi`, `vssurvivalmod`, `vsessentialsmod` (гілка на версії 1.22.7) і JSON-ресурси з `VintageStory-Dev\assets\survival`. Шляхи до файлів нижче — у цих репозиторіях/теках.

Позначки: ✅ підтверджено кодом/ресурсами · 🧪 потребує перевірки в грі (зонд) · ❓ не з'ясовано.

## 1. Взаємодія предмета з контейнером

- ✅ `CollectibleObject.HeldPriorityInteract` (`vsapi/Common/Collectible/Collectible.cs:89`): якщо `true`, то при **Shift + ПКМ** по блоку спершу викликається `OnHeldInteractStart` предмета, а не блок (скриня не відкривається). Без Shift порядок звичайний: блок першим. У ванілі так робить смолоскип (`BlockTorch.cs:39`). Поле не документоване для JSON — виставляємо в `OnLoaded` у C#.
- ✅ Модифікатори для кліків: `EntityControls.ShiftKey` і `EntityControls.CtrlKey` (`vsapi/Common/Entity/EntityControls.cs`). Документація прямо радить їх, а не `Sneak`/`Sprint`, для "клік + модифікатор".
- ✅ Друга рука: `EntityAgent.LeftHandItemSlot` (або `InventoryManager.OffhandHotbarSlot`).
- 🧪 Чи доходить `CtrlKey` до предмета разом із Shift, і що буде при Ctrl+ПКМ без Shift (очікування: відкриється скриня).

**Пропозиція для дизайну:** Shift+ПКМ — прибуткова, Shift+Ctrl+ПКМ — видаткова. Звичайний ПКМ з книгою в руці просто відкриває скриню — це навіть зручно.

## 2. Контейнери першої черги

| Контейнер | Блок (code) | Клас блок-сутності |
|---|---|---|
| Скриня | `chest-*` | `BlockEntityGenericTypedContainer` |
| Сундук | `trunk-*` | `BlockEntityGenericTypedContainer` |
| Підписана скриня | `labeledchest-*` | `BlockEntityLabeledChest` (нащадок попереднього) |
| Корзина (стаціонарна) | `stationarybasket-*` | `BlockEntityGenericTypedContainer` |
| Посудина для зберігання | `storagevessel-*` | `BlockEntityGenericTypedContainer` |
| Ящик | `crate-*` | `BlockEntityCrate` (один тип предмета) |
| Рюкзак/кошик на землі | `groundstorage` | `BlockEntityGroundStorage`; рюкзак лежить у слоті як предмет |

- ✅ Усі, крім рюкзака на землі, мають спільну основу `BlockEntityContainer` / інтерфейс `IBlockEntityContainer.Inventory` — одна гілка коду.
- ✅ Рюкзак на землі: стек рюкзака в слоті `groundstorage`; його вміст — через `IHeldBag` (`stack.Collectible.GetCollectibleInterface<IHeldBag>()`, `GetContents` / запис через той самий інтерфейс). У ванілі цим займається `BEBehaviorContainedBagInventory`.
- 🧪 Як саме виглядають ці інвентарі зсередини (зонд виводить усі слоти).

## 3. Доступ (land claims, замки, укріплення)

- ✅ **Важливо:** з `HeldPriorityInteract` блок і його поведінки не викликаються, тож ванільна перевірка замка (`BlockBehaviorLockable`) теж не спрацює. Перевіряємо самі, на сервері:
  - приватні зони: `world.Claims.TestAccess(player, pos, EnumBlockAccessFlags.Use)`; для контейнерів з `IClaimTraverseable` (скрині, корзини, посудини) ваніль дозволяє й рівень `Traverse` — повторюємо цю логіку;
  - замки/укріплення: `ModSystemBlockReinforcement.IsLockedForInteract(pos, player)`.
- ✅ Інші моди захисту чіпляються до `IEventAPI.OnTestBlockAccess`, який викликається з `TestAccess`, — тож вони покриються автоматично.
- 🧪 Чи не блокує рушій предмет у чужій зоні ще до нашого коду (перевірка на сервері з двома гравцями — етап 4).

## 4. Інвентар гравця і транзакція

- ✅ `IPlayerInventoryManager`: `GetOwnInventory("hotbar"/"backpack")`, `TryGiveItemstack`, `GetBestSuitedSlot`.
- ✅ Для "все або нічого" `TryGiveItemstack` не годиться (кладе частково). План: спершу "сухий прогін" — порахувати місце через `CollectibleObject.GetMergableQuantity` для неповних слотів і `ItemSlot.CanHold` для порожніх (сумки з `storageFlags` приймають не все), і лише потім переносити.

## 5. Ідентичність предметів

- ✅ Ваніль при порівнянні стеків ігнорує `GlobalConstants.IgnoredStackAttributes` = `temperature`, `toolMode`, `renderVariant`, `transitionstate` (псування). Знос — атрибут `durability`, додаємо до списку самі.
- ✅ Порівняння: `ItemStack.Equals(world, other, ignoredAttributes)`.

## 6. Гобелени

- ✅ Блок `tapestry-{north|east|south|west}`, малюнок — атрибут стека `type` (`blocktypes/cloth/tapestry.json`, `BlockTapestry.cs`).
- ✅ Знятий зі стіни гобелен завжди падає як `tapestry-north` з `type` — орієнтація не важить (`BehaviorHorizontalAttachable`, `dropBlockFace = "north"`).
- ✅ **Гнилий гобелен нічого не дає при знятті** (`BlockTapestry.GetDrops`).
- ✅ **Великі гобелени складаються зі шматків.** `ambush` (3×1) = `ambush1..3`, `salvation` (2×2) = `salvation11, 12, 21, 22`. Усього **31 малюнок = 88 шматків**.
- ✅ Повний список можна згенерувати з атрибута блоку `sizes` (назва → розмір); результат точно збігається з 88 шматками з ресурсів. У творчому інвентарі гобеленів немає (видаються командою `/giveblock tapestry-north 1 {type:"ambush1"}`), тож автовиявлення для каталогу — саме через `sizes`. Модові гобелени підхопляться, якщо мод доповнює `sizes`.
- ❓ Чи всі 88 шматків трапляються у виживанні (частина не входить до `tapestryGroups`: `aid-*`, `glider`, `symptoms`). Можливо, вони є лише в сюжетних локаціях — винести в конфіг виключень, якщо з'ясуємо.

**Наслідок для дизайну:** одиниця колекції — шматок. Каталог може показувати два прогреси: шматків (X з 88) і повністю зібраних малюнків (Y з 31).

## 7. Крафт

- ✅ Порожня книга: `book-normal-<колір>` (2 шкіри + 8 `paper-parchment`). Є також `book-aged-*` і `book-rotten-*`.
- ✅ Пергамент: `paper-parchment`, стек 32.
- ✅ Перо з чорнилом: `inkandquill` (миска чорного барвника + пір'їна). В інгредієнті ставимо `isTool: true` — не витрачається.
- ✅ **Знайдена велика книга** — клатер: блок `clutter` з атрибутом `type: "bookshelves/large-book-closed"` (також `-open`; `-pile*`, `-standing*` — купки; `-evaporating` — сюжетні, виключити).
- ✅ **Роль клею:** клатер має поведінку `Reparable` (`reparability: 6`). Не відремонтований клеєм — при ламанні розбивається й нічого не дає. Відремонтований — падає предметом. Тобто клей потрібен саме для того, щоб добути книгу.
- ✅ Інгредієнт з точним кодом (без `*`) порівнюється через `Satisfies`: атрибути з рецепта мають бути підмножиною атрибутів стека. Тож `{ type: "block", code: "clutter", attributes: { type: "bookshelves/large-book-closed" } }` працює без коду.
- ✅ **Вапняна вода в рецепті сітки — чистим JSON**, як у ванільному тісті (`recipes/grid/dough.json`): `recipeAttributes: { requiresContent: { type: "item", code: "limewaterportion" }, requiresLitres: N }`. Рідина витрачається, посудина лишається. Глечик: `jug-*-fired` (`BlockLiquidContainerTopOpened`).
- ✅ Залізні деталі: `metalnailsandstrips-iron` (можна дозволити `meteoriciron`, `steel`).
- 🧪 Чи з'являються великі книги в руїнах у виживанні (зонд покаже атрибути, якщо знайдемо).

## 8. Полиця

- ✅ Ванільна книга ставиться на полицю завдяки атрибутам `shelvable: true` + `displayable.shelf { size, category: "book" }`, також `displaycaseable`, `bookshelveable` (`itemtypes/lore/book.json`). Копіюємо їх у свій предмет.

## 9. Довідник

- ✅ Власні предмети можуть доповнювати свою сторінку через `ICustomHandbookPageContent.OnHandbookPageComposed`.
- ✅ Кнопка "додати в книгу" на сторінці *будь-якого* предмета — лише Harmony-патчем `CollectibleBehaviorHandbookTextAndExtraInfo.GetHandbookInfo`. Можливо, але це пізній етап.

### Клавіші-модифікатори в підказках (перевірено в грі 27.10.2026)

Гарячі клавіші `shift` / `ctrl` — модифікатори миші; `Controls.ShiftKey` / `CtrlKey` читаються з них. Тег `<hk>`, `WorldInteraction.HotKeyCode`
і наша підказка книги (`capi.Input.HotKeys[code].CurrentMapping`, vsapi HotkeyComponent.cs:19) показують одне й те саме.
- Галочка "Прив'язати модифікатори миші до дій Присідання\Бігу" (`separateCtrlKeyForMouse: false`, за замовчуванням): у налаштуваннях
  окремих рядків немає. Після зміни "Красться" на Щ до кінця сесії підказки (і ванільні) пишуть Щ, а дія лишається на Shift;
  після перезаходу і підказки, і дія на Shift. Вада гри, не наша.
- Галочку знято (`true`): модифікатор миші перепризначається окремо; і підказки, і дія йдуть за ним, зокрема після перезаходу.

## 10. Схожі моди (ModDB, 1.22.7)

- **Bookkeeper** — стаціонарний пюпітр, індекс контейнерів довкола (лише читання), пошук, підсвічування.
- **Quartermaster** (той самий автор) — стаціонарний стіл, забирати й класти предмети в навколишні контейнери.
- **Infinitum Collections** — журнал відкритих предметів (риба, гриби, метелики); гобелени лише в планах.
- **Seraph's Ledger** — набір QoL-дрібниць, облік торговців; не про колекції.

Ніхто не робить портативну книгу з накладними як операціями над множинами й каталогів з прогресом. Варто згадати Bookkeeper/Quartermaster в описі мода як сумісні/інші підходи.

## Зонд (етап 1)

Тимчасовий предмет `warehouseledger:probe` ("Дослідницький зонд", у творчому інвентарі). Клік ним пише в `VintageStory-DevData\Logs\server-main.log` (і `client-main.log`) рядок `[WLProbe]` з усім, що бачить: сторона, клавіші, блок, клас блок-сутності, доступ, вміст контейнера (і рюкзаків у ньому), предмет у другій руці з атрибутами. Видаляється на етапі 2.
