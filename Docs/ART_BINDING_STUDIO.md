# Art Binding Studio

Окно открывается через `RPG Quest > Art > Art Binding Studio`.

## Нарезка нового листа

1. Выберите PNG в `Assets/Art/SourceSheets`.
2. Нажмите `Prepare selected sheet`.
3. В Sprite Editor задайте границы и имена спрайтов.
4. Нажмите `Apply`.
5. Нажмите `Refresh` в Art Binding Studio.

Импортер сохраняет для `SourceSheets` режим `Multiple`, поэтому ручная
нарезка больше не сбрасывается при повторном импорте.

## Замена в префабе, сцене или конфиге

1. Выберите объект или prefab в Project/Hierarchy.
2. Выберите спрайт в левой части Art Binding Studio.
3. В центральной части найдите нужный `Image`, `SpriteRenderer` или
   поле типа `Sprite`.
4. Нажмите `Assign selected sprite`.

Изменение записывается с поддержкой Undo. Prefab-asset сохраняется сразу.

## Замена графики интерфейса, созданного кодом

Часть интерфейса получает изображения через `ArtLibrary`, поэтому у нее
нет постоянного prefab-поля для ручного назначения.

1. Выберите новый спрайт слева.
2. Справа найдите ключ старого изображения.
3. Нажмите `Save runtime override`.
4. Перезапустите Play Mode, если экран уже был открыт.

Подмены хранятся в
`Assets/Resources/Art/ArtBindings.asset`. Исходные изображения можно не
удалять: при отсутствии подмены `ArtLibrary` использует их как раньше.

## Рекомендуемый порядок переделки

1. Фоны и карта.
2. Постоянные панели интерфейса.
3. Иконки локаций и заданий.
4. Герой, враги и NPC.
5. Предметы, награды и эффекты.
6. Проверка каждого экрана в телефонном и широком разрешении.

## Текущий набор первого этапа

- `Assets/Prefabs/UI/Generated/Icons` — иконки UI.
- `Assets/Prefabs/UI/Generated/Panels` — панели, кнопки, рамки и статусы.
- `Assets/Prefabs/UI/Generated/Rewards` — только `reward_gold` и `reward_xp`.
- `Assets/Prefabs/Art/Characters/Enemies` — слоты состояний врагов.
- `Assets/Prefabs/Art/Characters/Hero` — слоты героя.
- `Assets/Prefabs/Art/Map` — сохраненные элементы карты.
- `Assets/Resources/Art/Backgrounds` — сохраненные задники.

Prefab-каталоги `Art/FX` и `Art/Items` временно удалены. Выпадение
предметов из боевой награды отключено, а кнопка инвентаря скрыта.

Повторно создать чистые слоты можно через
`RPG Quest > Art > Rebuild Manual Art Prefabs` или сочетанием
`Ctrl+Shift+M`.
