# RPG Quest Adventure MVP

Этот Unity-проект собран по `CODEX_UNITY_IMPLEMENTATION_GUIDE.md` как первый MVP-скелет 2D RPG clicker adventure.

## Что уже добавлено

- Структура папок из гайда.
- Доменные модели: `UserProfile`, `ChildProfile`, `GameState`, `RealLifeQuest`, `InventoryItemInstance`, enum-статусы.
- ScriptableObject-конфиги: биомы, враги, данжи, карта, предметы, навыки, лут, города.
- Сервисы без привязки к UI: auth, profile, gameState, quests, rewards, inventory, equipment, map, battle, analytics, cloud functions facade, level, network.
- Runtime UI для MVP: Login, Map, Battle, Town/Adventure Guild.
- Единый визуальный стиль MVP: игровые фоны, карточки, тени, обводки, карта-пергамент, маркеры локаций, HUD боя, HP/rage bars, reward-панель.
- Offline/dev режим, чтобы проект запускался без Firebase Unity SDK.
- Editor-генератор демо-сцен и демо-контента.

## Геоквесты: первый этап

Добавлены логика посещений и источник координат GPS с ручными координатами
для Unity Editor. Проверка без запуска игры:
`RPG Quest → Geo Quests → Test Visit Tracking`.
Можно проверить радиус, погрешность, одно посещение в день и цель за неделю.
Подключение к игровым карточкам, родительскому сайту и Firebase — следующие этапы.
Подробности: `Docs/GeoQuests.md` в корне проекта.

## Как сгенерировать сцены

В Unity открой меню:

`RPG Quest -> Build MVP Project`

Генератор создаст:

- `Assets/Scenes/00_Boot.unity`
- `Assets/Scenes/01_Login.unity`
- `Assets/Scenes/02_Map.unity`
- `Assets/Scenes/03_Battle.unity`
- `Assets/Scenes/04_Town.unity`
- `Assets/Scenes/05_Loading.unity`

И демо-контент:

- 2 данжа: `sunny_forest`, `crystal_cave`
- 1 город: `home_village`
- несколько врагов
- стартовую loot table

## Как запустить

1. Запусти `RPG Quest -> Build MVP Project`.
2. Открой `Assets/Scenes/00_Boot.unity`.
3. Нажми Play.
4. На экране входа нажми `Dev вход`.
5. Проверь карту, бой и городскую Adventure Guild.

Основная сцена для старта: `Assets/Scenes/00_Boot.unity`.

## PC test controls

Эти клавиши нужны только для Editor/Standalone тестов:

- Login: `Enter` обычный вход, `F1` dev вход, `Tab` переключение полей.
- Map: `1-9` открыть локацию по порядку, `G` открыть город/гильдию, `F5` добавить тестовые XP/gold, `Esc` вернуться на Login.
- Battle: `Space`/`Enter` удар, `Q` Power Attack, `E` Heal, `Esc` карта.
- Town: `1-5` фильтры гильдии, `Space`/`Enter` действие с первым доступным квестом, `R` обновить список, `Esc` карта.

Для тестовой Windows-сборки: `RPG Quest -> Build Windows Test Player`.

## Firebase

Для PC/Editor тестов Unity умеет подключаться к той же Firebase базе, что и родительский Next.js сайт, через REST bridge без Firebase Unity SDK.

Unity ищет web-конфиг здесь:

- `Assets/StreamingAssets/firebase-web-config.env`
- `C:/Users/erush/Desktop/rpg_quest_parent_web_starter/.env.local`

Рабочий поток:

1. В родительском сайте открыть ребёнка.
2. Перейти во вкладку `Unity вход`.
3. Создать email/password для ребёнка.
4. В Unity на Login ввести этот email/password и нажать `Войти`.

После входа Unity загружает `users/{uid}`, проверяет `role == "child"`, берёт `childId`, загружает `children/{childId}`, `gameState/main` и квесты из `children/{childId}/quests`.

Сейчас Firebase слой работает через REST adapter. Когда Firebase Unity SDK будет добавлен, реальные вызовы можно перенести внутрь тех же сервисов:

- `FirebaseInitializer`
- `AuthService`
- `PlayerProfileService`
- `GameStateService`
- `QuestService`
- `CloudFunctionsService`
- `AnalyticsService`

Firestore-схема и статусы в моделях совпадают с гайдом.
