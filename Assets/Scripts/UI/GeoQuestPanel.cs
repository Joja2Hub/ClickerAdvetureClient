using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    /// <summary>Foreground check-in UI. Local visits do not claim rewards or submit remote quests.</summary>
    public sealed class GeoQuestPanel : MonoBehaviour
    {
        private GeoQuestDefinition definition;
        private string childId;
        private GeoQuestLocalStore store;
        private GeoQuestCheckInService checkIns;
        private Text progressText;
        private Text statusText;
        private Text actionText;
        private Image progressBar;
        private Button action;
        private bool loaded;
        private bool checking;
        private bool completed;
        private float nextActionRefresh;
        public event Action Closed;

        public static GeoQuestPanel OpenDemo(Transform parent, string childId)
        {
            var offset = TimeSpan.FromHours(3);
            DateTime today = DateTimeOffset.UtcNow.ToOffset(offset).Date;
            int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            var start = new DateTimeOffset(today.AddDays(-daysSinceMonday), offset);
            return Open(parent, childId, new GeoQuestDefinition
            {
                Id = "demo-school-" + start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Title = "Пять посещений за неделю",
                Latitude = 55.75, Longitude = 37.62, RadiusMeters = 100, RequiredVisits = 5,
                StartsAtUnixSeconds = start.ToUnixTimeSeconds(), EndsAtUnixSeconds = start.AddDays(7).ToUnixTimeSeconds(),
                UtcOffsetMinutes = 180
            }, "Учебное задание: посещение тестовой точки. Прогресс сохраняется на этом устройстве. Задание без награды.");
        }

        public static GeoQuestPanel Open(Transform parent, string childId, GeoQuestDefinition definition, string instructions)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(childId)) throw new ArgumentException("Child ID is required.", nameof(childId));
            // Validate before adding UI or location components to the scene.
            new GeoQuestTracker(definition);
            var overlay = RuntimeUi.Panel(parent, "GeoQuestOverlay", new Color(0.03f, 0.08f, 0.09f, 0.96f));
            overlay.SetAsLastSibling();
            var panel = overlay.gameObject.AddComponent<GeoQuestPanel>();
            panel.childId = childId;
            panel.definition = definition;
            panel.store = new GeoQuestLocalStore();
            var provider = overlay.gameObject.AddComponent<GeoLocationProvider>();
            panel.checkIns = new GeoQuestCheckInService(provider, panel.store);
            panel.Build(instructions);
            panel.LoadProgress();
            return panel;
        }

        private void Build(string instructions)
        {
            var card = RuntimeUi.Card(transform, "GeoQuestCard", UiTheme.Paper);
            RuntimeUi.SetRect(card, new Vector2(0.5f, 0.51f), new Vector2(920, 1180), Vector2.zero);
            var title = RuntimeUi.Text(card, "Title", definition.Title, 38, UiTheme.Ink);
            title.fontStyle = FontStyle.Bold;
            title.supportRichText = false;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 24;
            title.resizeTextMaxSize = 38;
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.5f, 0.87f), new Vector2(800, 110), Vector2.zero);
            var hint = RuntimeUi.Text(card, "Instructions", instructions, 25, UiTheme.SecondaryInk);
            hint.supportRichText = false;
            RuntimeUi.SetRect(hint.rectTransform, new Vector2(0.5f, 0.73f), new Vector2(800, 170), Vector2.zero);
            var details = RuntimeUi.Card(card, "LocationDetails", new Color(0.91f, 0.93f, 0.90f));
            RuntimeUi.SetRect(details, new Vector2(0.5f, 0.56f), new Vector2(800, 170), Vector2.zero);
            var offset = TimeSpan.FromMinutes(definition.UtcOffsetMinutes);
            var start = DateTimeOffset.FromUnixTimeSeconds(definition.StartsAtUnixSeconds).ToOffset(offset);
            var end = DateTimeOffset.FromUnixTimeSeconds(definition.EndsAtUnixSeconds).ToOffset(offset);
            string location = string.Format(CultureInfo.InvariantCulture, "Точка: {0:F5}, {1:F5} · радиус {2:0} м",
                definition.Latitude, definition.Longitude, definition.RadiusMeters);
            var info = RuntimeUi.Text(details, "Details", location + $"\n{start:dd.MM HH:mm} — {end:dd.MM HH:mm} ({start:zzz})\nНе более одного посещения в день", 24, UiTheme.Ink);
            RuntimeUi.Stretch(info.rectTransform);
            info.rectTransform.offsetMin = new Vector2(20, 12);
            info.rectTransform.offsetMax = new Vector2(-20, -12);
            progressText = RuntimeUi.Text(card, "Progress", "Загрузка прогресса…", 36, UiTheme.Ink);
            progressText.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(progressText.rectTransform, new Vector2(0.5f, 0.41f), new Vector2(760, 70), Vector2.zero);
            progressBar = RuntimeUi.ProgressBar(card, "ProgressBar", new Color(0.84f, 0.88f, 0.84f), UiTheme.Primary);
            RuntimeUi.SetRect((RectTransform)progressBar.transform.parent, new Vector2(0.5f, 0.35f), new Vector2(760, 28), Vector2.zero);
            progressBar.fillAmount = 0;
            statusText = RuntimeUi.Text(card, "Status", "", 27, UiTheme.SecondaryInk);
            RuntimeUi.SetRect(statusText.rectTransform, new Vector2(0.5f, 0.23f), new Vector2(760, 170), Vector2.zero);
            action = RuntimeUi.Button(card, "CheckIn", "Отметить посещение", UiTheme.Primary, CheckIn);
            RuntimeUi.SetRect(action.GetComponent<RectTransform>(), new Vector2(0.5f, 0.09f), new Vector2(760, 86), Vector2.zero);
            actionText = action.GetComponentInChildren<Text>();
            var close = RuntimeUi.Button(transform, "Close", "Вернуться к карте", UiTheme.Secondary, Close);
            RuntimeUi.SetRect(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0.10f), new Vector2(430, 78), Vector2.zero);
        }

        private void LoadProgress()
        {
            try
            {
                var tracker = store.Load(childId, definition);
                loaded = true;
                SetProgress(tracker.VisitCount);
                statusText.text = completed ? "Цель достигнута. Все посещения сохранены." : "Подойди к нужной точке и отметь посещение.";
            }
            catch (Exception exception) when (IsStorageError(exception))
            {
                loaded = false;
                progressText.text = "Прогресс недоступен";
                statusText.text = "Не удалось загрузить прогресс. Повтори попытку. Сохранённые посещения не сбрасываются.";
            }
            UpdateAction();
        }

        private async void CheckIn()
        {
            if (checking) return;
            if (!loaded) { LoadProgress(); return; }
            checking = true;
            statusText.text = "Определяем местоположение…";
            UpdateAction();
            try
            {
                var result = await checkIns.CheckInAsync(childId, definition);
                if (this == null || !isActiveAndEnabled) return;
                if (result.Status != GeoCheckInStatus.Busy) SetProgress(result.VisitCount);
                statusText.text = Describe(result);
            }
            catch (Exception exception) when (IsStorageError(exception))
            {
                if (this != null && isActiveAndEnabled)
                    statusText.text = "Не удалось сохранить или загрузить посещение. Проверь свободное место и повтори попытку.";
            }
            finally
            {
                if (this != null) { checking = false; UpdateAction(); }
            }
        }

        private void SetProgress(int count)
        {
            completed = count >= definition.RequiredVisits;
            progressText.text = $"{count} из {definition.RequiredVisits} посещений";
            progressBar.fillAmount = (float)count / definition.RequiredVisits;
        }

        private void UpdateAction()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool active = now >= definition.StartsAtUnixSeconds && now < definition.EndsAtUnixSeconds;
            action.interactable = !checking && (!loaded || (!completed && active));
            actionText.text = checking ? "Ищем точку…" : !loaded ? "Повторить загрузку"
                : completed ? "Цель достигнута" : !active ? "Задание вне срока" : "Отметить посещение";
        }

        public void Close() => Destroy(gameObject);

        private void Update()
        {
            if (action == null || Time.unscaledTime < nextActionRefresh) return;
            nextActionRefresh = Time.unscaledTime + 1;
            UpdateAction();
        }

        private void OnDestroy() => Closed?.Invoke();

        private static bool IsStorageError(Exception exception) => exception is IOException
            || exception is UnauthorizedAccessException || exception is ArgumentException;

        private static string Describe(GeoCheckInResult result)
        {
            if (result.Status == GeoCheckInStatus.Busy) return "Проверка уже выполняется. Подожди немного.";
            if (result.Status == GeoCheckInStatus.InvalidReading) return "Нет свежих точных координат. Попробуй ещё раз.";
            if (result.Status == GeoCheckInStatus.LocationFailed)
            {
                switch (result.LocationError)
                {
                    case GeoLocationError.DisabledOrDenied: return "Разреши доступ к местоположению и включи геолокацию, затем повтори попытку.";
                    case GeoLocationError.Timeout: return "Не удалось определить точку вовремя. Попробуй на открытом месте.";
                    case GeoLocationError.Cancelled: return "Проверка прервана. Можно попробовать ещё раз.";
                    case GeoLocationError.Busy: return "Геолокация уже используется. Повтори попытку немного позже.";
                    default: return "Местоположение недоступно. Проверь геолокацию и повтори попытку.";
                }
            }
            switch (result.VisitResult)
            {
                case GeoVisitResult.Counted: return "Посещение засчитано и сохранено!";
                case GeoVisitResult.Completed: return "Цель уже достигнута. Все посещения сохранены.";
                case GeoVisitResult.AlreadyVisitedToday: return "Сегодня посещение уже засчитано. Приходи в другой день.";
                case GeoVisitResult.OutsideArea: return "Ты пока вне нужного радиуса. Подойди ближе к точке.";
                case GeoVisitResult.OutsidePeriod: return "Срок задания ещё не начался или уже закончился.";
                default: return "Координаты недостаточно точные. Подожди немного и повтори попытку.";
            }
        }
    }
}
