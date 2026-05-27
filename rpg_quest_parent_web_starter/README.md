# RPG Quest Parent Dashboard — стартовый каркас

Это первый рабочий каркас родительского сайта для проекта: детская RPG clicker adventure + реальные квесты от родителя.

## Что уже есть

- Next.js + TypeScript приложение.
- Firebase Web SDK.
- Авторизация родителя через Firebase Auth.
- Первичная настройка семьи и ребёнка.
- Список детей.
- Создание real-life квестов.
- Список квестов ребёнка.
- Экран проверки отправленных квестов.
- Заготовка аналитики.
- Firestore Rules.
- Cloud Functions для production-логики одобрения, отклонения и получения награды.
- Большая инструкция для Codex по Unity-клиенту: `CODEX_UNITY_IMPLEMENTATION_GUIDE.md`.

## 1. Создание Firebase-проекта

1. Создай проект в Firebase Console.
2. Включи Authentication → Email/Password.
3. Создай Firestore Database.
4. Добавь Web App в настройках проекта.
5. Скопируй Firebase config в `.env.local` по примеру `.env.example`.

## 2. Установка сайта

```bash
npm install
cp .env.example .env.local
npm run dev
```

Открой:

```text
http://localhost:3000
```

## 3. Переменные окружения

Файл `.env.local`:

```bash
NEXT_PUBLIC_FIREBASE_API_KEY=...
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=...
NEXT_PUBLIC_FIREBASE_PROJECT_ID=...
NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=...
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=...
NEXT_PUBLIC_FIREBASE_APP_ID=...
NEXT_PUBLIC_FIREBASE_FUNCTIONS_REGION=europe-west1
```

## 4. Деплой Firestore Rules

Установи Firebase CLI и войди в аккаунт:

```bash
npm install -g firebase-tools
firebase login
firebase use --add
firebase deploy --only firestore:rules,firestore:indexes
```

## 5. Cloud Functions

Функции уже лежат в папке `functions`:

- `approveQuest`
- `rejectQuest`
- `claimQuestReward`

Установка и деплой:

```bash
cd functions
npm install
npm run build
cd ..
firebase deploy --only functions
```

В текущем MVP сайт пока использует прямое обновление статуса квеста из клиента. Это сделано, чтобы проще проверить UI. Для production нужно заменить `approveQuestDirect` и `rejectQuestDirect` в `questService.ts` на вызов callable Cloud Functions.

## 6. Firestore-модель

```text
users/{uid}
  uid
  role: "parent" | "child"
  familyId
  childId?
  displayName
  email

families/{familyId}
  name
  ownerParentId
  createdAt

children/{childId}
  familyId
  displayName
  level
  xp
  gold

children/{childId}/gameState/main
  level
  xp
  gold
  maxHp
  damage
  critChance
  rageGain
  unlockedLocations
  completedDungeons
  currentMapNodeId
  dailyStreak

children/{childId}/quests/{questId}
  title
  description
  category
  status
  rewardXp
  rewardGold
  rewardItems
  createdByParentId
  childComment
  parentComment

children/{childId}/rewardLedger/{rewardId}
  sourceType
  sourceId
  xp
  gold
  items
  claimed

children/{childId}/analyticsDaily/{yyyy-MM-dd}
  questsAssigned
  questsSubmitted
  questsApproved
  questsRejected
  battlesPlayed
  playSessions
  totalPlayTimeSeconds
  xpEarned
  goldEarned
```

## 7. MVP-проверка

1. Зарегистрируй родителя.
2. Перейди на `/setup`.
3. Создай семью и ребёнка.
4. Перейди в ребёнка → «Создать квест».
5. Создай квест.
6. Проверь, что квест появился в списке.

Чтобы увидеть экран «Проверка», вручную поменяй у квеста статус в Firestore с `assigned` на `submitted`. Когда Unity-клиент будет готов, он будет делать это сам.

## 8. Следующий шаг

После проверки каркаса нужно сделать:

1. Подключить вызовы `approveQuest`, `rejectQuest`, `claimQuestReward` из сайта и Unity.
2. Сделать Unity-авторизацию ребёнка.
3. В Unity реализовать загрузку `children/{childId}/quests`.
4. В Unity реализовать отправку задания на проверку.
5. В Unity реализовать получение награды через `claimQuestReward`.
