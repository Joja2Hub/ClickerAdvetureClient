using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ClickerAdventure.EditorTools
{
    /// <summary>Local sandbox; does not write quests, rewards or location history to Firebase.</summary>
    public sealed class GeoQuestTestWindow : EditorWindow
    {
        private double targetLatitude = 55.75;
        private double targetLongitude = 37.62;
        private float radiusMeters = 100;
        private int requiredVisits = 5;
        private int utcOffsetMinutes = 180;
        private double observedLatitude = 55.75;
        private double observedLongitude = 37.62;
        private float accuracyMeters = 5;
        private int dayOffset;
        private GeoQuestTracker tracker;
        [SerializeField] private GeoQuestDefinition savedDefinition;
        private GeoQuestLocalStore Store => new GeoQuestLocalStore(
            Path.Combine(Application.persistentDataPath, "GeoQuestEditorPreview"));
        private const string PreviewChildId = "editor-demo-child";
        private DateTimeOffset periodStart;
        private string result = "Создайте тестовый квест.";

        [MenuItem("RPG Quest/Geo Quests/Test Visit Tracking")]
        public static void Open()
        {
            GetWindow<GeoQuestTestWindow>("Геоквесты").minSize = new Vector2(420, 480);
        }

        private void OnEnable()
        {
            if (savedDefinition != null) RestoreProgress();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Локальная проверка. Координаты вводятся вручную; " +
                "данные и награды в Firebase не меняются. Посещения сохраняются локально; сброс создаёт новый прогресс.",
                MessageType.Info);

            EditorGUILayout.LabelField("Условия нового квеста", EditorStyles.boldLabel);
            targetLatitude = EditorGUILayout.DoubleField("Широта точки", targetLatitude);
            targetLongitude = EditorGUILayout.DoubleField("Долгота точки", targetLongitude);
            radiusMeters = EditorGUILayout.FloatField("Радиус, м", radiusMeters);
            requiredVisits = EditorGUILayout.IntField("Посещений за 7 дней", requiredVisits);
            utcOffsetMinutes = EditorGUILayout.IntField("UTC: смещение, минут", utcOffsetMinutes);
            if (GUILayout.Button("Создать / сбросить тестовый квест"))
                CreateQuest();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Проверка посещения", EditorStyles.boldLabel);
            observedLatitude = EditorGUILayout.DoubleField("Широта ребёнка", observedLatitude);
            observedLongitude = EditorGUILayout.DoubleField("Долгота ребёнка", observedLongitude);
            accuracyMeters = EditorGUILayout.FloatField("Погрешность, м", accuracyMeters);
            dayOffset = EditorGUILayout.IntField("День от начала (0–6)", dayOffset);

            using (new EditorGUI.DisabledScope(tracker == null))
            {
                if (tracker != null)
                {
                    EditorGUILayout.LabelField("Начало периода", periodStart.ToString("yyyy-MM-dd HH:mm zzz"));
                    EditorGUILayout.LabelField("Прогресс", $"{tracker.VisitCount} посещений" +
                        (tracker.IsCompleted ? " — цель достигнута" : ""));
                }
                if (GUILayout.Button("Отметить посещение"))
                {
                    if (dayOffset < -1 || dayOffset > 7)
                        result = "Для проверки выберите день от -1 до 7.";
                    else
                    {
                        result = Describe(tracker.RegisterVisit(observedLatitude, observedLongitude,
                            accuracyMeters, periodStart.AddDays(dayOffset).AddHours(12)));
                        SaveProgress();
                    }
                }
                if (GUILayout.Button("Повторить сохранение")) SaveProgress();
            }
            using (new EditorGUI.DisabledScope(savedDefinition == null))
                if (GUILayout.Button("Загрузить прогресс с диска")) RestoreProgress();
            EditorGUILayout.HelpBox(result, MessageType.None);
            EditorGUILayout.LabelField("Изменённые условия применяются после сброса квеста.", EditorStyles.wordWrappedLabel);
        }

        private void CreateQuest()
        {
            try
            {
                if (utcOffsetMinutes < -840 || utcOffsetMinutes > 840)
                    throw new ArgumentException("Смещение UTC должно быть от -840 до 840 минут.");
                TimeSpan offset = TimeSpan.FromMinutes(utcOffsetMinutes);
                DateTime today = DateTimeOffset.UtcNow.ToOffset(offset).Date;
                var start = new DateTimeOffset(today, offset);
                var definition = new GeoQuestDefinition
                {
                    Id = "editor-school-week",
                    Title = "Тест посещения школы",
                    Latitude = targetLatitude,
                    Longitude = targetLongitude,
                    RadiusMeters = radiusMeters,
                    RequiredVisits = requiredVisits,
                    StartsAtUnixSeconds = start.ToUnixTimeSeconds(),
                    EndsAtUnixSeconds = start.AddDays(7).ToUnixTimeSeconds(),
                    UtcOffsetMinutes = utcOffsetMinutes
                };
                var nextTracker = new GeoQuestTracker(definition);
                Store.Save(PreviewChildId, nextTracker);
                tracker = nextTracker;
                savedDefinition = definition;
                periodStart = start;
                dayOffset = 0;
                result = "Квест создан. Отметьте посещение, затем повторите в тот же день или выберите следующий.";
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is IOException || exception is UnauthorizedAccessException)
            {
                result = "Не удалось создать квест: " + exception.Message;
            }
        }

        private void SaveProgress()
        {
            try { Store.Save(PreviewChildId, tracker); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                result = "Прогресс в памяти не сохранён: " + exception.Message;
            }
        }

        private void RestoreProgress()
        {
            try
            {
                tracker = Store.Load(PreviewChildId, savedDefinition);
                periodStart = DateTimeOffset.FromUnixTimeSeconds(savedDefinition.StartsAtUnixSeconds)
                    .ToOffset(TimeSpan.FromMinutes(savedDefinition.UtcOffsetMinutes));
                targetLatitude = savedDefinition.Latitude;
                targetLongitude = savedDefinition.Longitude;
                radiusMeters = (float)savedDefinition.RadiusMeters;
                requiredVisits = savedDefinition.RequiredVisits;
                utcOffsetMinutes = savedDefinition.UtcOffsetMinutes;
                result = "Прогресс загружен с диска.";
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is IOException || exception is UnauthorizedAccessException)
            {
                result = "Не удалось восстановить прогресс: " + exception.Message;
            }
        }

        private static string Describe(GeoVisitResult result)
        {
            switch (result)
            {
                case GeoVisitResult.Counted: return "Посещение засчитано.";
                case GeoVisitResult.AlreadyVisitedToday: return "В этот день посещение уже засчитано.";
                case GeoVisitResult.OutsidePeriod: return "Дата вне срока задания.";
                case GeoVisitResult.InvalidLocation: return "Некорректные координаты.";
                case GeoVisitResult.InaccurateLocation: return "Недостаточная точность координат.";
                case GeoVisitResult.OutsideArea: return "Точка с учётом погрешности находится вне радиуса.";
                case GeoVisitResult.Completed: return "Цель уже достигнута.";
                default: return result.ToString();
            }
        }
    }
}
