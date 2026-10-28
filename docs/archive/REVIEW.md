# Огляд взаємодій з гравцем (етап 9)

Мета: пройти все, що гравець бачить і робить, і для кожного пункту вирішити, чи зрозуміло новачкові, **що сталося** і **що
робити далі**. Нотатки пишемо просто тут, у колонці "Нотатки" або під пунктом. Рішення потім переносимо в `DESIGN.md`.

Як іти: розділ за розділом, у грі поруч (дев-копія, `/curio` дає гобелени, аркуші й глечики). Тексти в таблицях унизу
згенеровані з `lang/uk.json` і `lang/en.json` (27.10.2026) скриптом `tools/lang-table.pl`; після правок
перегенерувати: `perl tools/lang-table.pl curiocatalogue/assets/curiocatalogue/lang/en.json curiocatalogue/assets/curiocatalogue/lang/uk.json`.

## 1. Жести

Модифікатори: Shift / Ctrl / ПКМ. "Контейнер" — скриня, сундук, ящик, кошик, посудина для зберігання, рюкзак на землі.
Інші блоки з інвентарем (піч, жорна, бочка, полиця) — "не підтримується".

### 1.1 Книга в головній руці

| Що | Облікова книга | Список матеріалів | Альбом |
|---|---|---|---|
| ПКМ у повітря | вікно книги | вікно книги | вікно альбому |
| ПКМ по контейнеру | контейнер відкривається (ваніль) | так само | так само |
| Shift + ПКМ по контейнеру | прибуткова накладна: типи, яких нема в книзі | прибуткова: надлишок понад кількості | одразу забирає по одному кожного нового для колекції, викреслює |
| Shift + Ctrl + ПКМ по контейнеру | видаткова: типи з книги (усі екземпляри) | видаткова: у межах кількостей | нічого (клік іде грі) |
| Shift + ПКМ по землі / блоку без інвентаря | книга кладеться на землю (ваніль) | так само | так само |

### 1.2 Книга в другій руці, головна порожня

Ті самі дії, що й 1.1, тільки накладна з'являється відразу в головній руці. Утримана кнопка дає одну дію (повтори гри
кожні 250 мс поглинаються).

### 1.3 Накладна або список бажаного в головній руці

| Що | Прибуткова / видаткова накладна | Список бажаного |
|---|---|---|
| ПКМ у повітря | вікно накладної | вікно списку |
| Shift + ПКМ по контейнеру | провести: все або нічого; книга в другій руці оновлюється й забирає аркуш, інакше накладна стає пергаментом | взяти зі списку те, що є й що влазить; виконаний список стає пергаментом |
| Shift + ПКМ по землі | кладеться на землю, як сувій | так само |

Особливості проведення: альбом у другій руці при проведенні не враховується; при анулюванні будь-якої накладної аркуш іде
в будь-яку книгу в другій руці.

### 1.4 Питання до огляду

- [x] Чи очевидно, що прибуткова — Shift, а видаткова — Shift + Ctrl? Чи потрібні підказки внизу екрана, а не лише над скринею? Рішення: внизу екрана лише "ПКМ — відкрити" для книг і накладних (`GetHeldInteractionHelp`); жести з Shift — над контейнером.
- [x] Альбом: Shift + Ctrl + ПКМ нічого не робить — чи варто тоді казати про це гравцю? Рішення: ні, клік іде грі, підказка над контейнером не наштовхує на Ctrl.
- [x] Вікно відкривається звичайним ПКМ у повітря — чи не заважає це іншим діям з книгою в руці? Рішення: лишити, ПКМ у повітря з книгою у ванілі нічого не робить, ванільна книга відкривається так само.
- [x] Чи зрозуміло, куди дівається аркуш і чому накладна стала пергаментом? Рішення: повідомлення після проведення кажуть "Аркуш повернувся в книгу." або "Накладна стала пергаментом: книги в другій руці немає."; правила — у гайді й розділі "Аркуші".

## 2. Вікна

### 2.1 Вікно книги (облікова книга, список матеріалів, альбом)

- Назва (поле + "Зберегти"), рядок режиму й прогресу (для альбому "зібрано X з Y · малюнків A з B"), аркуші й записи.
- Пошук, сортування (за назвою, модом; для списку матеріалів — за кількістю).
- Сітка записів (лише перегляд). Для альбому з підписом "Бракує:".
- Кнопки: "Очистити" (з підтвердженням); для альбому — "Почати заново" і "Список бажаного".

### 2.2 Вікно накладної / списку бажаного

- Звідки й коли складена, рядки, кнопка "Анулювати" (з підтвердженням).

### 2.3 Питання до огляду

- [x] Чи потрібна в альбомі вся колекція (зібране + порожні клітинки) — записано як ідея після MVP. Рішення: після MVP.
- [x] Чи зрозумілі підписи кнопок і підтверджень? Рішення: "Скласти список бажаного"; "Анулювати" всюди, підтвердження коротке й каже, що саме ("Анулювати накладну?" / "Анулювати список бажаного?"); правила аркуша — у гайді. "Аркуші лишаться" в "Очистити" не додаємо.

## 3. Рецепти (сітка крафту)

| Група | Рецепт | Що дає |
|---|---|---|
| 1 | Запасний: ванільна книга посередині, цвяхи над і під, перо праворуч | облікова книга, 8 аркушів, обкладинка від книги |
| 1 | Відновлення: велика книга-мотлох + глечик вапняної води (1 л) + перо | облікова книга, 32 аркуші, темно-зелена |
| 2 | Порожня книга сама | перемикання облікова книга ↔ список матеріалів |
| 3 | Книга + пергамент | дозаряджання до 32 |
| 4 | Порожня облікова книга чи список + шматок гобелена (лишається) | альбом гобеленів |
| 5 | Книга + фарбована шкіра | інший колір обкладинки |
| 6 | Книга + глечик сірчаної кислоти (1 л) | стара обкладинка |

- [x] Чи знайде новачок ці рецепти без довідника? Чи достатньо розділів "Як користуватися", "Аркуші", "Обкладинка" на сторінці книги? Рішення: ні, і не мусить — нестандартні рецепти шукають у довіднику; перший рецепт описано словами в гайді.

## 4. Тексти

Рішення огляду 27.10.2026:
- Назви й тултіпи: в альбому немає рядка "Записи" (ні в тултіпі, ні у вікні), лише аркуші й прогрес; тултіп альбому — "по одному кожного шматка, якого бракує"; скрізь "примірники"; сторінка списку бажаного каже, що його можна анулювати.
- Чат і відмови: після проведення — куди дівся аркуш і чому пергамент; "немає різниці" замінено двома відмовами: "немає нічого, чого бракує книзі" (прибуткова) і "немає нічого з книги" (видаткова). Підказки над контейнером — без змін.

### Назви предметів

| Ключ | Українська | English | Нотатки |
|---|---|---|---|
| `modname` | Каталог дивовиж | Curio Catalogue | |
| `item-book-ledger-*` | Облікова книга | Ledger | |
| `item-book-materials-*` | Список матеріалів | Bill of materials | |
| `item-book-album-*` | Альбом | Album | |
| `item-invoice-incoming` | Прибуткова накладна | Incoming invoice | |
| `item-invoice-outgoing` | Видаткова накладна | Outgoing invoice | |
| `item-invoice-wantlist` | Список бажаного | Want list | |
| `item-handbooktext-curiocatalogue:invoice-incoming` | Записано те, що було в контейнері й чого бракувало книзі. Візьміть накладну в руку й натисніть <hk>shift</hk> + <hk>rightmouse</hk> по будь-якому контейнеру, щоб провести її: предмети перейдуть в інвентар, а книга в другій руці додасть їх і отримає аркуш назад. Без книги там накладна стане пергаментом. <hk>rightmouse</hk> у повітря показує накладну; там її можна анулювати. Докладніше — у гайді <a href=\"handbook://curiocatalogue-guide\">Каталог дивовиж</a>. | Lists what a container had and the book lacked. Take it in hand and press <hk>shift</hk> + <hk>rightmouse</hk> on any container to post it: the items move into your inventory, and a book in your off-hand adds them and gets its sheet back. Without a book there the invoice becomes parchment. <hk>rightmouse</hk> into the air shows the invoice; it can be cancelled there. More in the <a href=\"handbook://curiocatalogue-guide\">Curio Catalogue</a> guide. | |
| `item-handbooktext-curiocatalogue:invoice-outgoing` | Записано те, що з книги знайшлося в контейнері. Візьміть накладну в руку й натисніть <hk>shift</hk> + <hk>rightmouse</hk> по будь-якому контейнеру, щоб провести її: предмети перейдуть в інвентар, а книга в другій руці викреслить їх і отримає аркуш назад. Без книги там накладна стане пергаментом. <hk>rightmouse</hk> у повітря показує накладну; там її можна анулювати. Докладніше — у гайді <a href=\"handbook://curiocatalogue-guide\">Каталог дивовиж</a>. | Lists what a container had of the book's entries. Take it in hand and press <hk>shift</hk> + <hk>rightmouse</hk> on any container to post it: the items move into your inventory, and a book in your off-hand crosses them off and gets its sheet back. Without a book there the invoice becomes parchment. <hk>rightmouse</hk> into the air shows the invoice; it can be cancelled there. More in the <a href=\"handbook://curiocatalogue-guide\">Curio Catalogue</a> guide. | |
| `item-handbooktext-curiocatalogue:invoice-wantlist` | Те, чого бракує альбому; складається у вікні альбому, щоб віддати другові. Хто тримає список, натискає <hk>shift</hk> + <hk>rightmouse</hk> по скрині: список забирає в його інвентар те, що в ньому записано, і викреслює взяте. Виконаний список стає пергаментом. <hk>rightmouse</hk> у повітря показує список; там його можна анулювати. Докладніше — у гайді <a href=\"handbook://curiocatalogue-guide\">Каталог дивовиж</a>. | What an album still misses, made in the album window to give to a friend. Whoever holds it presses <hk>shift</hk> + <hk>rightmouse</hk> on a chest: the list takes what it names into their inventory and crosses it off. When the list is done, it turns into parchment. <hk>rightmouse</hk> into the air shows the list; it can be cancelled there. More in the <a href=\"handbook://curiocatalogue-guide\">Curio Catalogue</a> guide. | |
| `item-handbooktitle-curiocatalogue:invoice-*` | Як користуватися | How to use | |
| `album-name-tapestries` | Альбом: гобелени | Album: tapestries | |

### Підказки предметів (тултіп)

| Ключ | Українська | English | Нотатки |
|---|---|---|---|
| `ledger-info-kind` | {0} | {0} | |
| `ledger-info-sheets` | Аркуші: {0} з {1} | Sheets: {0} of {1} | |
| `game:tapestry-` | будь-який шматок | any piece | |
| `ledger-info-cover` | Обкладинка: {0} | Cover: {0} | |
| `ledger-info-cover-aged` | Обкладинка: {0}, стара | Cover: {0}, aged | |
| `cover-brickred` | цегляна | brick red | |
| `cover-cherryred` | вишнева | cherry red | |
| `cover-darkbeige` | темно-бежева | dark beige | |
| `cover-darkgray` | темно-сіра | dark gray | |
| `cover-darkgreen` | темно-зелена | dark green | |
| `cover-darkolive` | темно-оливкова | dark olive | |
| `cover-gray` | сіра | gray | |
| `cover-olive` | оливкова | olive | |
| `cover-orange` | помаранчева | orange | |
| `cover-orangebrown` | руда | orange-brown | |
| `cover-purple` | фіолетова | purple | |
| `cover-purpleorange` | фіолетово-помаранчева | purple-orange | |
| `cover-teal` | бірюзова | teal | |
| `ledger-info-entries` | Записи: {0} (ліміт {1}) | Entries: {0} (limit {1}) | |
| `ledger-info-album` | Зібрано {0} з {1} · малюнків {2} з {3} | Collected {0} of {1} · pictures {2} of {3} | |
| `ledger-info-howto-types` | {0} + ПКМ по контейнеру: прибуткова накладна на предмети, яких немає в книзі. ⏎ {0} + {1} + ПКМ: видаткова накладна на предмети з книги. Під час проведення вона забере з контейнера всі їхні примірники. | {0} + right-click a container: incoming invoice for items not in the ledger. ⏎ {0} + {1} + right-click: outgoing invoice for items in the ledger. When posted, it takes all of them from the container. | |
| `ledger-info-howto-counts` | {0} + ПКМ по контейнеру: прибуткова накладна на надлишок понад кількість у списку. ⏎ {0} + {1} + ПКМ: видаткова накладна на те, що просить список, не більше вказаної кількості. | {0} + right-click a container: incoming invoice for what is over the listed counts. ⏎ {0} + {1} + right-click: outgoing invoice for what the list asks for, up to its counts. | |
| `ledger-info-howto-album` | Записано те, чого колекції ще бракує. ⏎ {0} + ПКМ по контейнеру: одразу забирає по одному кожного шматка, якого бракує, і викреслює його. Дублікати лишаються. | Lists what the collection is still missing. ⏎ {0} + right-click a container: takes one of each missing item right away and crosses it off. Duplicates stay. | |
| `ledger-info-open` | ПКМ у повітря відкриває вікно: у руці або в другій руці, коли головна порожня. | Right-click into the air opens it, in your hand or in your off-hand when the main hand is empty. | |
| `invoice-info-blank` | Порожньо | Blank | |
| `invoice-info-origin` | Складено в день {0} біля {1}, {2}, {3} | Made on day {0} at {1}, {2}, {3} | |
| `invoice-info-line` | {0} × {1} | {0} × {1} | |
| `invoice-info-more` | …і ще {0} | …and {0} more | |
| `unknown-item` | Невідомий предмет #{0} | Unknown item #{0} | |

### Довідник і гайд

| Ключ | Українська | English | Нотатки |
|---|---|---|---|
| `guide-title` | Каталог дивовиж | Curio Catalogue | |
| `guide-text` | <strong>Каталог дивовиж</strong> — мод про колекціонування. Його серце — <strong>альбом</strong>: книга, що знає всю вашу колекцію, показує, чого бракує, і забирає нові шматки просто зі скрині. Перша колекція — гобелени. Поруч з альбомом — дві звичайні книги для обліку: <strong>облікова книга</strong> і <strong>список матеріалів</strong>.<br><br><strong>Перша книга</strong><br>Покладіть ванільну книгу <itemstack type=\"item\">book-normal-teal\|book-normal-cherryred\|book-aged-orange</itemstack> посередині сітки крафту, цвяхи й смуги <itemstack type=\"item\">metalnailsandstrips-iron</itemstack> над і під нею, а перо з чорнилом <itemstack type=\"item\">inkandquill</itemstack> праворуч. Вийде <a href=\"handbook://item-curiocatalogue:book-ledger-normal-darkgreen\">облікова книга</a> з 8 аркушами й обкладинкою тієї книги. Щасливчики, що знайдуть у руїнах <a href=\"handbook://block-clutter-{{ \\\"type\\\": \\\"bookshelves/large-book-closed\\\" }}\">велику книгу</a>, можуть відновити її глечиком вапняної води й пером з чорнилом: така книга має 32 аркуші.<br><br><strong>Альбом</strong><br>Порожня облікова книга чи список матеріалів і будь-який шматок гобелена <itemstack type=\"block\">tapestry-north</itemstack> у сітці крафту дають <a href=\"handbook://item-curiocatalogue:book-album-normal-brickred\">альбом</a> <itemstack type=\"item\">curiocatalogue:book-album-normal-brickred</itemstack>; шматок лишається. В альбомі всі гобелени записані як «бракує». Тримайте його в руці (або в другій руці, коли основна порожня) й натисніть <hk>shift</hk> + <hk>rightmouse</hk> по скрині: він забере в інвентар по одному примірнику кожного шматка, якого бракує, і викреслить їх. Дублікати лишаться в скрині. Якщо в інвентарі не вистачить місця, альбом візьме те, що вміститься, малюнок за малюнком.<br><hk>rightmouse</hk> у повітря відкриває альбом: там видно, чого бракує, скільки зібрано шматків і цілих малюнків, і є пошук. Кнопка <i>Почати заново</i> знову позначає все як «бракує».<br><br><strong>Список бажаного</strong><br>Кнопка <i>Список бажаного</i> у вікні альбому переписує все, чого бракує, на окремий <a href=\"handbook://item-curiocatalogue:invoice-wantlist\">список</a>; це коштує один аркуш. Віддайте його другові: коли друг натисне <hk>shift</hk> + <hk>rightmouse</hk> по скрині, список забере з неї в інвентар друга все, що є в списку, і викреслить узяте. Передати знайдене вам — уже справа друга: список лише збирає. Виконаний список стає пергаментом.<br><br><strong>Облікова книга і список матеріалів</strong><br>Облікова книга записує, які предмети є, <a href=\"handbook://item-curiocatalogue:book-materials-normal-teal\">список матеріалів</a> — скільки кожного; порожня книга, покладена в сітку крафту без інших предметів, перемикається між ними. З книгою в руці <hk>shift</hk> + <hk>rightmouse</hk> по контейнеру складає <a href=\"handbook://item-curiocatalogue:invoice-incoming\">прибуткову накладну</a> на те, чого в книзі немає, а <hk>shift</hk> + <hk>ctrl</hk> + <hk>rightmouse</hk> — <a href=\"handbook://item-curiocatalogue:invoice-outgoing\">видаткову</a> на те, що в ній є. Потім візьміть накладну в руку й натисніть <hk>shift</hk> + <hk>rightmouse</hk> по будь-якому контейнеру, щоб провести її: предмети перейдуть в інвентар. Якщо книга в другій руці, вона оновиться й отримає аркуш назад; без неї накладна стане пергаментом.<br><br><strong>Аркуші й обкладинки</strong><br>Кожна накладна й кожен список бажаного коштує аркуш. Накладну чи список бажаного можна анулювати в їхньому вікні: предмети лишаться на місці, а аркуш повернеться в книгу в другій руці; без книги накладна чи список стане пергаментом. Додавайте аркуші <a href=\"handbook://item-paper-parchment\">пергаментом</a> у сітці крафту, до 32. Фарбована шкіра перефарбовує книгу, а глечик сірчаної кислоти старить обкладинку. | <strong>Curio Catalogue</strong> is about collecting. Its heart is the <strong>album</strong>: a book that knows your whole collection, shows what you are still missing and takes new pieces straight from a chest. The first collection is tapestries. Next to the album live two plain books for keeping stock, the <strong>ledger</strong> and the <strong>bill of materials</strong>.<br><br><strong>Your first book</strong><br>Put a vanilla book <itemstack type=\"item\">book-normal-teal\|book-normal-cherryred\|book-aged-orange</itemstack> in the middle of the crafting grid, nails and strips <itemstack type=\"item\">metalnailsandstrips-iron</itemstack> above and below it, and ink and quill <itemstack type=\"item\">inkandquill</itemstack> to its right. You get a <a href=\"handbook://item-curiocatalogue:book-ledger-normal-darkgreen\">ledger</a> with 8 sheets and the cover of the book you used. The lucky ones who find a <a href=\"handbook://block-clutter-{{ \\\"type\\\": \\\"bookshelves/large-book-closed\\\" }}\">large book</a> in ruins can restore it with a jug of lime water and ink and quill: that ledger has 32 sheets.<br><br><strong>The album</strong><br>An empty ledger or bill of materials and any tapestry piece <itemstack type=\"block\">tapestry-north</itemstack> in the crafting grid make an <a href=\"handbook://item-curiocatalogue:book-album-normal-brickred\">album</a> <itemstack type=\"item\">curiocatalogue:book-album-normal-brickred</itemstack>; the piece stays. The album lists every tapestry as missing. Hold it, or keep it in your off-hand with the main hand empty, and press <hk>shift</hk> + <hk>rightmouse</hk> on a chest: it takes one of each missing piece into your inventory and crosses them off. Duplicates stay in the chest. If your inventory runs out of room, it takes what fits, picture by picture.<br><hk>rightmouse</hk> into the air opens the album: it shows what is missing and how many pieces and whole pictures you have, and has a search. The <i>Start over</i> button makes everything missing again.<br><br><strong>Want lists</strong><br>The <i>Want list</i> button in the album window writes everything still missing onto a separate <a href=\"handbook://item-curiocatalogue:invoice-wantlist\">list</a>; this costs one sheet. Give it to a friend: when they press <hk>shift</hk> + <hk>rightmouse</hk> on a chest, the list moves what it names from the chest into your friend's inventory and crosses it off. Handing the finds over to you is up to your friend; the list only gathers. When the list is done, it turns into parchment.<br><br><strong>Ledger and bill of materials</strong><br>The ledger notes which kinds of items there are, the <a href=\"handbook://item-curiocatalogue:book-materials-normal-teal\">bill of materials</a> how many of each; an empty book alone in the crafting grid switches between them. With a book in hand, <hk>shift</hk> + <hk>rightmouse</hk> on a container makes an <a href=\"handbook://item-curiocatalogue:invoice-incoming\">incoming invoice</a> for what the book lacks, <hk>shift</hk> + <hk>ctrl</hk> + <hk>rightmouse</hk> an <a href=\"handbook://item-curiocatalogue:invoice-outgoing\">outgoing invoice</a> for what it lists. Then take the invoice in hand and press <hk>shift</hk> + <hk>rightmouse</hk> on any container to post it: the items move into your inventory. With the book in your off-hand it is updated and gets its sheet back; without it the invoice becomes parchment.<br><br><strong>Sheets and covers</strong><br>Each invoice and each want list costs a sheet. An invoice or a want list can be cancelled in its window: no items move, and its sheet goes back into the book in your off-hand; without one it becomes parchment. Add sheets with <a href=\"handbook://item-paper-parchment\">parchment</a> in the crafting grid, up to 32. Dyed leather recolours a book, and a jug with sulfuric acid ages its cover. | |
| `handbook-use-title` | Як користуватися | How to use | |
| `handbook-use-ledger-text` | Записує, які предмети є, без кількості. З обліковою книгою в руці, або в другій руці, коли основна порожня, <hk>shift</hk> + <hk>rightmouse</hk> по контейнеру складає <a href=\"handbook://item-curiocatalogue:invoice-incoming\">прибуткову накладну</a> на предмети, яких у книзі ще немає, а <hk>shift</hk> + <hk>ctrl</hk> + <hk>rightmouse</hk> — <a href=\"handbook://item-curiocatalogue:invoice-outgoing\">видаткову</a> на ті, що в ній є; проведена, вона забирає з контейнера всі їхні примірники. Проводьте накладні з книгою в другій руці, щоб книга оновлювалася. <hk>rightmouse</hk> у повітря відкриває книгу: записи, пошук, сортування, назва й <i>Очистити</i>. Порожня облікова книга, покладена в сітку крафту без інших предметів, стає <a href=\"handbook://item-curiocatalogue:book-materials-normal-teal\">списком матеріалів</a>, а з будь-яким шматком гобелена — <a href=\"handbook://item-curiocatalogue:book-album-normal-brickred\">альбомом</a>. Більше — у гайді <a href=\"handbook://curiocatalogue-guide\">Каталог дивовиж</a>. | Notes which kinds of items there are, without counts. With the ledger in hand, or in your off-hand with the main hand empty, <hk>shift</hk> + <hk>rightmouse</hk> on a container makes an <a href=\"handbook://item-curiocatalogue:invoice-incoming\">incoming invoice</a> for the items the ledger does not list yet, <hk>shift</hk> + <hk>ctrl</hk> + <hk>rightmouse</hk> an <a href=\"handbook://item-curiocatalogue:invoice-outgoing\">outgoing invoice</a> for the items it lists; posted, it takes all of them from the container. Post invoices with the ledger in your off-hand to keep it up to date. <hk>rightmouse</hk> into the air opens the ledger: its entries, search, sorting, a name and <i>Clear</i>. An empty ledger alone in the crafting grid becomes a <a href=\"handbook://item-curiocatalogue:book-materials-normal-teal\">bill of materials</a>, with any tapestry piece an <a href=\"handbook://item-curiocatalogue:book-album-normal-brickred\">album</a>. More in the <a href=\"handbook://curiocatalogue-guide\">Curio Catalogue</a> guide. | |
| `handbook-use-materials-text` | Записує, скільки якого предмета є: список покупок для проєкту, що сам зменшується. <a href=\"handbook://item-curiocatalogue:invoice-incoming\">Прибуткова накладна</a>, проведена зі списком у другій руці, додає свої предмети з кількостями. Потім зі списком у руці чи в другій руці <hk>shift</hk> + <hk>ctrl</hk> + <hk>rightmouse</hk> по контейнеру складає <a href=\"handbook://item-curiocatalogue:invoice-outgoing\">видаткову накладну</a> на те, чого список ще просить, у межах його кількостей. Проводьте її зі списком у другій руці, скриня за скринею: що лишилося в списку — того ще бракує. Без списку в другій руці накладна його не змінює, тож список працює як шаблон. <hk>shift</hk> + <hk>rightmouse</hk> складає прибуткову накладну на те, що понад записані кількості. Порожній список матеріалів, покладений у сітку крафту без інших предметів, стає <a href=\"handbook://item-curiocatalogue:book-ledger-normal-darkgreen\">обліковою книгою</a>, а з будь-яким шматком гобелена — <a href=\"handbook://item-curiocatalogue:book-album-normal-brickred\">альбомом</a>. Більше — у гайді <a href=\"handbook://curiocatalogue-guide\">Каталог дивовиж</a>. | Notes how many of each item there are: a shopping list for a project that shrinks by itself. An <a href=\"handbook://item-curiocatalogue:invoice-incoming\">incoming invoice</a> posted with the list in your off-hand adds its items with their counts. Then, with the list in hand or in your off-hand, <hk>shift</hk> + <hk>ctrl</hk> + <hk>rightmouse</hk> on a container makes an <a href=\"handbook://item-curiocatalogue:invoice-outgoing\">outgoing invoice</a> for what the list still asks for, up to its counts. Post it with the list in your off-hand, chest after chest: what stays in the list is what you still lack. Without the list in your off-hand an invoice leaves it unchanged, so the list works as a template. <hk>shift</hk> + <hk>rightmouse</hk> makes an incoming invoice for what is over the counts. An empty bill of materials alone in the crafting grid becomes a <a href=\"handbook://item-curiocatalogue:book-ledger-normal-darkgreen\">ledger</a>, with any tapestry piece an <a href=\"handbook://item-curiocatalogue:book-album-normal-brickred\">album</a>. More in the <a href=\"handbook://curiocatalogue-guide\">Curio Catalogue</a> guide. | |
| `handbook-use-album-text` | Знає всю колекцію й записує, чого ще бракує; перша колекція — гобелени. З альбомом у руці, або в другій руці, коли основна порожня, <hk>shift</hk> + <hk>rightmouse</hk> по скрині забирає в інвентар по одному кожного шматка, якого бракує, і викреслює його; дублікати лишаються в скрині. <hk>rightmouse</hk> у повітря відкриває альбом: чого бракує, скільки зібрано шматків і цілих малюнків, пошук. <i>Почати заново </i>знову позначає все як «бракує»; <i>Список бажаного </i>переписує все, чого бракує, на <a href=\"handbook://item-curiocatalogue:invoice-wantlist\">список бажаного</a> для друга. Більше — у гайді <a href=\"handbook://curiocatalogue-guide\">Каталог дивовиж</a>. | Knows the whole collection and lists what is still missing; the first collection is tapestries. With the album in hand, or in your off-hand with the main hand empty, <hk>shift</hk> + <hk>rightmouse</hk> on a chest takes one of each missing piece into your inventory and crosses it off; duplicates stay in the chest. <hk>rightmouse</hk> into the air opens the album: what is missing, how many pieces and whole pictures you have, and a search. <i>Start over </i>makes everything missing again; <i>Want list </i>writes what is missing onto a <a href=\"handbook://item-curiocatalogue:invoice-wantlist\">want list</a> for a friend. More in the <a href=\"handbook://curiocatalogue-guide\">Curio Catalogue</a> guide. | |
| `handbook-sheets-title` | Аркуші | Sheets | |
| `handbook-sheets-text` | Кожна накладна (<a href=\"handbook://item-curiocatalogue:invoice-incoming\">прибуткова</a> чи <a href=\"handbook://item-curiocatalogue:invoice-outgoing\">видаткова</a>) й кожен <a href=\"handbook://item-curiocatalogue:invoice-wantlist\">список бажаного</a> коштує аркуш. Якщо провести чи анулювати накладну з книгою в другій руці, аркуш повертається; без книги накладна стає пергаментом, як і виконаний список бажаного. Додавайте аркуші пергаментом у сітці крафту, по аркушу за штуку, до 32; що не влізе, лишається. | Each invoice (<a href=\"handbook://item-curiocatalogue:invoice-incoming\">incoming</a> or <a href=\"handbook://item-curiocatalogue:invoice-outgoing\">outgoing</a>) and each <a href=\"handbook://item-curiocatalogue:invoice-wantlist\">want list</a> costs a sheet. Posting or cancelling an invoice with the book in your off-hand gives the sheet back; without the book the invoice turns into parchment, and so does a finished want list. Add sheets with parchment in the crafting grid, one per piece, up to 32; what does not fit stays. | |
| `handbook-cover-title` | Обкладинка | Cover | |
| `handbook-cover-text` | Обкладинка — від ванільної книги; відновлена велика книга — темно-зелена. Фарбована шкіра перефарбовує книгу, глечик з 1 л сірчаної кислоти старить обкладинку (бірюзова й фіолетова не старіють). | The cover comes from the vanilla book; a restored large book is dark green. Dyed leather recolours a book, a jug with 1 litre of sulfuric acid ages its cover (teal and purple do not age). | |
| `item-handbooktext-curiocatalogue:invoice-incoming` | Записано те, що було в контейнері й чого бракувало книзі. Візьміть накладну в руку й натисніть <hk>shift</hk> + <hk>rightmouse</hk> по будь-якому контейнеру, щоб провести її: предмети перейдуть в інвентар, а книга в другій руці додасть їх і отримає аркуш назад. Без книги там накладна стане пергаментом. <hk>rightmouse</hk> у повітря показує накладну; там її можна анулювати. Докладніше — у гайді <a href=\"handbook://curiocatalogue-guide\">Каталог дивовиж</a>. | Lists what a container had and the book lacked. Take it in hand and press <hk>shift</hk> + <hk>rightmouse</hk> on any container to post it: the items move into your inventory, and a book in your off-hand adds them and gets its sheet back. Without a book there the invoice becomes parchment. <hk>rightmouse</hk> into the air shows the invoice; it can be cancelled there. More in the <a href=\"handbook://curiocatalogue-guide\">Curio Catalogue</a> guide. | |
| `item-handbooktext-curiocatalogue:invoice-outgoing` | Записано те, що з книги знайшлося в контейнері. Візьміть накладну в руку й натисніть <hk>shift</hk> + <hk>rightmouse</hk> по будь-якому контейнеру, щоб провести її: предмети перейдуть в інвентар, а книга в другій руці викреслить їх і отримає аркуш назад. Без книги там накладна стане пергаментом. <hk>rightmouse</hk> у повітря показує накладну; там її можна анулювати. Докладніше — у гайді <a href=\"handbook://curiocatalogue-guide\">Каталог дивовиж</a>. | Lists what a container had of the book's entries. Take it in hand and press <hk>shift</hk> + <hk>rightmouse</hk> on any container to post it: the items move into your inventory, and a book in your off-hand crosses them off and gets its sheet back. Without a book there the invoice becomes parchment. <hk>rightmouse</hk> into the air shows the invoice; it can be cancelled there. More in the <a href=\"handbook://curiocatalogue-guide\">Curio Catalogue</a> guide. | |
| `item-handbooktext-curiocatalogue:invoice-wantlist` | Те, чого бракує альбому; складається у вікні альбому, щоб віддати другові. Хто тримає список, натискає <hk>shift</hk> + <hk>rightmouse</hk> по скрині: список забирає в його інвентар те, що в ньому записано, і викреслює взяте. Виконаний список стає пергаментом. <hk>rightmouse</hk> у повітря показує список; там його можна анулювати. Докладніше — у гайді <a href=\"handbook://curiocatalogue-guide\">Каталог дивовиж</a>. | What an album still misses, made in the album window to give to a friend. Whoever holds it presses <hk>shift</hk> + <hk>rightmouse</hk> on a chest: the list takes what it names into their inventory and crosses it off. When the list is done, it turns into parchment. <hk>rightmouse</hk> into the air shows the list; it can be cancelled there. More in the <a href=\"handbook://curiocatalogue-guide\">Curio Catalogue</a> guide. | |
| `item-handbooktitle-curiocatalogue:invoice-*` | Як користуватися | How to use | |

### Підказки над контейнером

| Ключ | Українська | English | Нотатки |
|---|---|---|---|
| `heldhelp-incoming` | Прибуткова накладна | Incoming invoice | |
| `heldhelp-outgoing` | Видаткова накладна | Outgoing invoice | |
| `heldhelp-post` | Провести накладну | Post the invoice | |
| `heldhelp-collect` | Забрати нове для колекції | Take new items for the collection | |
| `heldhelp-take` | Взяти зі списку | Take from the list | |
| `heldhelp-open-ledger` | Відкрити облікову книгу | Open the ledger | |
| `heldhelp-open-materials` | Відкрити список матеріалів | Open the bill of materials | |
| `heldhelp-open-album` | Відкрити альбом | Open the album | |
| `heldhelp-open-incoming` | Переглянути накладну | View the invoice | |
| `heldhelp-open-outgoing` | Переглянути накладну | View the invoice | |
| `heldhelp-open-wantlist` | Переглянути список бажаного | View the want list | |

### Повідомлення в чаті (результат)

| Ключ | Українська | English | Нотатки |
|---|---|---|---|
| `invoice-made` | {0}: рядків — {1}. Аркушів лишилося: {2} з {3}. | {0}. Lines: {1}. Sheets left: {2} of {3}. | |
| `invoice-posted` | Проведено: рядків — {0}, предметів — {1}. | Posted. Lines: {0}, items: {1}. | |
| `invoice-posted-ledger` | Записів у книзі: {0} → {1}. | Book entries: {0} → {1}. | |
| `invoice-posted-sheet` | Аркуш повернувся в книгу. | Its sheet is back in the book. | |
| `invoice-posted-parchment` | Накладна стала пергаментом: книги в другій руці немає. | The invoice is now parchment: no book in your off-hand. | |
| `invoice-posted-parchment-full` | Аркушів у книзі вже {0}, тож накладна стала пергаментом. | The book already has {0} sheets, so the invoice is now parchment. | |
| `ledger-overlimit` | Записів у книзі: {0}, понад ліміт {1}. Прибуткових накладних не буде, доки не приберете частину записів. | The book has {0} entries, over its limit of {1}. It cannot make incoming invoices until some entries are removed. | |
| `ledger-cleared` | Книгу очищено: прибрано записів — {0}. | The book is cleared. Entries removed: {0}. | |
| `album-restarted` | Альбом розпочато заново. Лишилося зібрати: {0}. | The album starts over. Still to collect: {0}. | |
| `invoice-cancelled` | Накладну анульовано. | The invoice is cancelled. | |
| `invoice-cancelled-sheet` | Накладну анульовано, аркуш повернувся в книгу. | The invoice is cancelled, its sheet is back in the book. | |
| `invoice-cancelled-sheet-album` | Накладну анульовано, аркуш повернувся в альбом. | The invoice is cancelled, its sheet is back in the album. | |
| `invoice-cancelled-parchment` | Накладну анульовано, вона стала пергаментом. | The invoice is cancelled and is now parchment. | |
| `wantlist-made` | Список бажаного: треба знайти предметів — {0}. Аркушів лишилося: {1} з {2}. | Want list. Items to find: {0}. Sheets left: {1} of {2}. | |
| `wantlist-posted` | Взято зі списку: {0}. Лишилося в списку: {1}. | Taken from the list: {0}. Still on the list: {1}. | |
| `wantlist-posted-done` | Взято зі списку: {0}. Список виконано, він став пергаментом. | Taken from the list: {0}. The list is done and is now parchment. | |
| `album-collected` | Взято для колекції: {0}. Лишилося зібрати: {1}. | Taken for the collection: {0}. Still to collect: {1}. | |
| `album-collected-done` | Взято для колекції: {0}. Колекцію зібрано! | Taken for the collection: {0}. The collection is complete! | |
| `notfitted` | Не вмістилося: {0} — звільніть місце. | Did not fit: {0}; make room. | |

### Червоні відмови

| Ключ | Українська | English | Нотатки |
|---|---|---|---|
| `ingameerror-unsupported` | Цей контейнер поки не підтримується | This container is not supported yet | |
| `ingameerror-noaccess` | Немає доступу до цього контейнера | You have no access to this container | |
| `ingameerror-ledgerfull` | Книга заповнена (записів: {0}). Складіть видаткову накладну, щоб звільнити місце | The book is full (entries: {0}). Make an outgoing invoice to make room | |
| `ingameerror-nodifference-incoming` | У цьому контейнері немає нічого, чого бракує книзі | This container has nothing the book lacks | |
| `ingameerror-nodifference-outgoing` | У цьому контейнері немає нічого з книги | This container has nothing from the book | |
| `ingameerror-nosheets` | У книзі не лишилося аркушів | The book has no sheets left | |
| `ingameerror-missing` | Бракує: {0} | Missing: {0} | |
| `ingameerror-noroom` | Не вистачає місця в інвентарі (бракує слотів: {0}) | Not enough room in your inventory. Slots still needed: {0} | |
| `ingameerror-nothingfromlist` | У цьому контейнері нічого зі списку | This container has nothing from the list | |
| `ingameerror-allcollected` | Колекцію зібрано, немає чого вносити в список | The collection is complete, nothing to put on a list | |
| `ingameerror-nothingnew` | У цьому контейнері нічого нового для колекції | Nothing new for the collection in this container | |

### Вікна

| Ключ | Українська | English | Нотатки |
|---|---|---|---|
| `ledgerwindow-name` | Назва: | Name: | |
| `ledgerwindow-save` | Зберегти | Save | |
| `ledgerwindow-mode-types` | Облікова книга | Ledger | |
| `ledgerwindow-mode-counts` | Список матеріалів | Bill of materials | |
| `ledgerwindow-mode-album` | Альбом | Album | |
| `ledgerwindow-info` | Аркуші: {0} з {1} · Записи: {2} (ліміт {3}) | Sheets: {0} of {1} · Entries: {2} (limit {3}) | |
| `ledgerwindow-collected` | Зібрано {0} з {1} · малюнків {2} з {3} | Collected {0} of {1} · pictures {2} of {3} | |
| `ledgerwindow-missing` | Бракує: | Missing: | |
| `ledgerwindow-search` | Пошук: | Search: | |
| `ledgerwindow-sort-name` | За назвою | By name | |
| `ledgerwindow-sort-mod` | За модом | By mod | |
| `ledgerwindow-sort-count` | За кількістю | By count | |
| `ledgerwindow-clear` | Очистити | Clear | |
| `ledgerwindow-restart` | Почати заново | Start over | |
| `ledgerwindow-wantlist` | Скласти список бажаного | Make a want list | |
| `confirm-clear` | Очистити книгу? Буде прибрано записів: {0}. | Clear the book? Entries to remove: {0}. | |
| `confirm-restart` | Почати колекцію заново? Усе знову стане «бракує». | Start the collection over? Everything becomes missing again. | |
| `invoicewindow-summary` | Рядків: {0} · предметів: {1} | Lines: {0} · items: {1} | |
| `invoicewindow-cancel` | Анулювати | Cancel | |
| `confirm-cancel` | Анулювати накладну? | Cancel the invoice? | |
| `confirm-cancel-wantlist` | Анулювати список бажаного? | Cancel the want list? | |

