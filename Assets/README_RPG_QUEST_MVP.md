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

Сейчас Firebase слой работает как dev/offline adapter, чтобы проект компилировался без SDK. Когда Firebase Unity SDK будет добавлен, реальные вызовы нужно подключить внутри:

- `FirebaseInitializer`
- `AuthService`
- `PlayerProfileService`
- `GameStateService`
- `QuestService`
- `CloudFunctionsService`
- `AnalyticsService`

Firestore-схема и статусы в моделях совпадают с гайдом.
