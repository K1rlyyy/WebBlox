# WebBlox
> **Брендинг:** логотип WebBlox розміщений у `Assets/WebBloxLogo.png`, а іконка вікна/застосунку — у `Assets/WebBloxLogo.ico`. Щоб отримати повну відповідність оригінальному макету, використовуйте сам PNG/SVG-ресурс логотипа.

WebBlox is a WPF visual HTML builder with an HTML element library and a beginner-friendly CSS designer.

## HTML mode
- Switch to **HTML** in the left library.
- Search for an HTML tag, choose a category, then drag it onto the canvas (or double-click it).
- Select an element on the canvas to edit its text, attributes, and quick styles.

## CSS mode
- Switch to **CSS** in the left library.
- Choose a colored category in the vertical Scratch-style rail on the left; the matching CSS properties appear beside it.
- Select a property. Its plain-language explanation and a starter value appear below.
- Change the value if needed and click **Add property**.
- The property is added to the CSS rule for the selected element. If no element is selected, it targets `body`.
- The CSS tab on the right contains the full CSS editor, expanded ready-made presets (gradients, cards, shadows, sticky navigation, responsive images, auto-fit Grid, pill buttons, and CSS variables), responsive media-query helper, and CSS import/export.

The generated HTML preview includes the global CSS. Advanced CSS syntax (including pseudo-classes, `@media`, `@keyframes`, CSS variables, and newer CSS features) can be entered directly in the full CSS editor.



## JavaScript
- У правій панелі відкрий вкладку **JavaScript · Логіка**, щоб редагувати код, імпортувати `.js` або експортувати його окремим файлом.
- Кнопки **Клік по кнопці** та **Лічильник** вставляють приклади. У першому прикладі признач HTML-кнопці ID `myButton`; у лічильнику кнопці потрібен ID `counterButton`, а текстовому елементу — `counterValue`.
- Натисни **Перегляд**, щоб відкрити сторінку у браузері й перевірити поведінку. Під час експорту HTML JavaScript вбудовується в документ; окремої JS-бібліотеки або NuGet-пакета не потрібно.
- JavaScript зберігається у файлі `.webblox`. Проєкти старого формату без поля JavaScript відкриваються з порожнім редактором JS.
- Виконуй у попередньому перегляді лише код, якому довіряєш; помилки JavaScript можна переглянути в консолі браузера через F12.

## Build
Open `WebBlox.csproj` in Visual Studio 2022 with the .NET 8 SDK and the WPF workload installed. Restore NuGet packages, then build and run.

## Updated workspace layout
- The CSS category rail and property list are side-by-side, similar to Scratch categories and blocks.
- The central canvas uses a more compact layout, leaving more room for the CSS tools and inspector.

## HTML-бібліотека у стилі Scratch
- Категорії HTML розміщені вертикально зліва, у кольорових картках, за аналогією з CSS-бібліотекою.
- Поруч відображаються картки HTML-тегів із коротким описом.
- Після вибору елемента внизу показується його детальніше призначення.
- Пошук працює за назвою тегу та його описом; подвійний клік або перетягування додає елемент на полотно.

## Панелі, теми та пояснення CSS
- Категорії й відповідні HTML/CSS-елементи розтягуються на всю доступну ширину своїх колонок; висота списків підлаштовується під розмір області бібліотеки.
- Перетягуй вертикальні розділювачі між бібліотекою, полотном і налаштуваннями, щоб окремо змінювати ширину кожної панелі.
- Кнопка із сонцем/місяцем у верхній панелі перемикає світлу та темну теми. Іконка обертається під час перемикання.
- Кожна CSS-властивість у бібліотеці має пояснення прямо в картці; після вибору також показується підказка та приклад початкового значення.

## Останнє оновлення інтерфейсу

- Виправлено оновлення теми після перемикання HTML/CSS: видима панель перемальовується після появи її елементів.
- Об'єднано повторні оновлення теми, щоб уникати накладання відкладених перефарбувань.
- При перемиканні HTML/CSS зберігаються вибрані категорії; зовнішня прокрутка бібліотеки повертається нагору.
- Пошук HTML-елементів зберігає поточний вибір і позицію прокрутки, якщо елемент досі відповідає фільтру.
- У внутрішніх HTML/CSS-списках колесо миші прокручує сам список, а на його межі керування переходить до зовнішньої бібліотеки.
- Світлу тему оновлено до м'якої блакитно-сірої палітри з приглушеними поверхнями замість чисто білого фону.


## Розширена бібліотека HTML і CSS
- HTML-бібліотека містить 113 тегів, зокрема семантичні `hgroup`, `search`, `menu`, `q`, `dfn`, `s` і `slot`; для нових елементів додано описи та підтримку вкладення там, де це доречно.
- CSS-дизайнер містить понад 320 властивостей у 14 групах: додано логічні відступи й рамки, сучасні типографічні налаштування, окремі transform-властивості, позиціонування відносно якорів, scroll/view timelines та інші актуальні можливості CSS.
- Нові властивості можна вибирати в бібліотеці CSS або вводити вручну в редакторі коду. Наявний експорт HTML і CSS не змінено.

## CSS and external stylesheet links

- The CSS designer reads and updates a selector rule by matching the full selector, not a substring. It understands nested braces, comments, quoted text, functions, and rules inside `@media` / `@supports` blocks when reading declarations.
- The `link` HTML palette item creates a stylesheet link by default. In the properties panel, set **Посилання (href)** to a local CSS filename such as `styles.css` or to an `https://...` URL. In **Alt / placeholder / type / rel (для link)** set `stylesheet`; when it is left blank, WebBlox defaults to `stylesheet`.
- During browser preview, relative image, script, hyperlink, CSS-file, `url(...)`, and quoted `@import` paths are resolved from the current project folder (the folder where the project was saved/opened). If there is no project folder yet, the preview falls back to the application's current working directory.
- Exported HTML keeps portable relative paths. Keep `index.html` beside `styles.css` and other local assets, or put the assets in matching subfolders.
- HTML `<style>` palette elements are emitted in `<head>`, as are `<link>` elements. The WebBlox global CSS editor is emitted after linked stylesheets so its rules can override them when selectors have equal specificity.

## Validation note

The source was statically checked for well-formed XAML, event-handler references, duplicate named controls (control-template names are scoped separately), and balanced C# delimiters. Build/run validation still needs to be performed in Visual Studio with `dotnet restore` and `dotnet build`.


## Виправлення полотна та прокручування (листопад 2026)

- У вкладках HTML-властивостей і CSS Designer ввімкнено фізичне піксельне прокручування (`CanContentScroll=False`) та вимкнено відкладене прокручування, щоб перетягування повзунка оновлювало вміст одразу.
- Полотно HTML-структури отримало масштабування карток (70–140%), а також кнопки згортання та розгортання вкладених блоків.
- На полотні вибраний елемент тепер має чітку рамку; зміна вибору не перебудовує все дерево, тому фокус і позиція перегляду не скидаються без потреби.
- Ширина бібліотеки та панелі властивостей трохи зменшена за замовчуванням, щоб залишити більше місця центральному полотну; панелі можна й надалі змінювати перетягуванням роздільників.

## Візуальні JavaScript-блоки
- У правій панелі відкрий **JavaScript · Логіка → Візуальні блоки**. Бібліотека містить понад 140 шаблонів блоків у категоріях: Події, HTML та DOM, CSS та анімації, Змінні й математика, Умови та цикли, Функції й таймери, Повідомлення й діалоги, Сховище й дані, Мережа й API, Форми й браузер.
- Знайди блок через пошук або категорію. Двічі клацни, натисни **Додати блок** або перетягни його до послідовності праворуч.
- Вибери блок у послідовності та налаштуй його параметри. Для умов, циклів, подій і функцій доступне багаторядкове поле з кодом тіла.
- **Згенерувати JS** замінює код у вкладці редактора JavaScript згенерованою послідовністю; **Додати до коду** дописує її до наявного коду. Після цього натисни **Перегляд**, щоб виконати сценарій у браузері.
- Блоки можна переставляти, дублювати й видаляти, а також імпортувати/експортувати у JSON. Послідовність блоків зберігається всередині `.webblox` разом із HTML, CSS та JavaScript.
- Доступні блоки DOM для зміни тексту, HTML, атрибутів, класів, форм, елементів, посилань і зображень; CSS-ефекти й анімації; змінні, числа, рядки, масиви та JSON; if/else, цикли, функції, таймери; діалоги, localStorage/sessionStorage, fetch GET/POST, URL, fullscreen та інші браузерні дії.
- Блоки утворюють візуальну послідовність; вкладену логіку для умов, циклів і функцій можна редагувати в полі **Код блока**. Це не повний парсер довільного JavaScript у Scratch-блоки.
- Виконуй лише код, якому довіряєш. Запити до зовнішніх API можуть обмежуватися CORS, а fullscreen і буфер обміну залежать від правил браузера.

## Третя вкладка бібліотеки: JavaScript та готові функції
- У лівій панелі **Бібліотека** тепер є три режими: **HTML**, **CSS** і **JS**. Перемикання JS відкриває справа редактор візуальних JS-блоків.
- У JS-бібліотеці є пошук і фільтр категорій, картки з описом і вимогами до HTML-ID/селекторів.
- Доступні готові рішення: події кнопок, лічильник натискань, зміна тексту, показ/приховування, якорі, темна тема, анімації, валідація email і форм, лічильник символів, пароль, live-пошук, калькулятор, випадкові значення, вікторина, таймер, поточний рік, збереження в localStorage, список завдань, копіювання та завантаження тексту, попередній перегляд зображень, API/JSON, діалоги, карусель, акордеон і перемикання вкладок.
- Окремо є утиліти `wbDebounce`, `wbClamp`, `wbRandomInt`, `wbShuffle`, `wbFormatDate`, `wbEscapeHtml`, `wbSleep`, `wbSlugify` та `wbFormatNumber`.
- Вибраний шаблон можна додати в кінець редактора JavaScript, замінити ним код (із підтвердженням) або додати як візуальний блок. Подвійний клік по картці додає шаблон до коду.
- Більшість готових дій очікують елементи з вказаними ID або класами. Їхні вимоги показуються під описом функції; приклади API потребують підключення до мережі й дозволу CORS.
- Після додавання коду натисни **Перегляд**. Виконуй лише сценарії, яким довіряєш.


## Ліва JS-бібліотека та каталог готових рішень
- Поруч із HTML і CSS додано третій режим **JS**. У ньому можна шукати готові JavaScript-рішення за категоріями.
- Каталог містить 41 шаблон: дії кнопок, DOM, форми, ефекти, збереження даних, калькулятор/таймер/вікторина, API, карусель, акордеон, вкладки сторінки й багаторазові утиліти.
- Для кожної картки є опис і список потрібних HTML-ID/класів. Подвійне клацання або **Додати до JS-коду** додає шаблон у редактор; **Замінити JS** робить це після підтвердження; **Додати як блок** зберігає шаблон як блок у візуальній послідовності.
- На зміні розміру вікна висота списку функцій підлаштовується до доступної області бібліотеки.


## Категорії JavaScript та однакова швидкість прокручування
- Ліва JS-бібліотека використовує кольорову вертикальну панель категорій, як HTML і CSS. Картки готових функцій показуються поруч; пошук працює разом з обраною категорією.
- Категорії JS: усі функції, кнопки та події, текст і DOM, ефекти та анімації, форми, ігри й утиліти, дані та сховище, утиліти JavaScript.
- Усі прокручувані області вікна використовують однаковий крок коліщатка — близько 32 фізичних пікселів на стандартний поділ. Якщо внутрішній список досяг краю, залишок прокручування передається зовнішній панелі.
- Для `ListBox` і `ScrollViewer` вимкнено логічне та відкладене прокручування, щоб рух працював у пікселях, а перетягування повзунка одразу оновлювало вміст.
