using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public class LoginScreen : MonoBehaviour
    {
        private TMP_InputField emailInput;
        private TMP_InputField passwordInput;
        private Text statusText;
        private bool isLoggingIn;

        private async void Start()
        {
            await ServiceRegistry.InitializeAsync();
            Build();
        }

        private void Update()
        {
            if (!DesktopTestMode.IsEnabled || isLoggingIn)
            {
                return;
            }

            if (DesktopTestMode.Pressed(KeyCode.Tab))
            {
                if (emailInput.isFocused)
                {
                    passwordInput.ActivateInputField();
                }
                else
                {
                    emailInput.ActivateInputField();
                }
            }

            if (DesktopTestMode.Pressed(KeyCode.Return) || DesktopTestMode.Pressed(KeyCode.KeypadEnter))
            {
                _ = LoginAsync(false);
            }

            if (DesktopTestMode.Pressed(KeyCode.F1))
            {
                _ = LoginAsync(true);
            }
        }

        private void Build()
        {
            var canvas = RuntimeUi.CreateCanvas();
            RuntimeUi.AdventureBackground(canvas.transform, new Color(0.18f, 0.37f, 0.54f), new Color(0.14f, 0.42f, 0.31f), new Color(0.08f, 0.12f, 0.16f));

            var crest = RuntimeUi.Circle(canvas.transform, "HeroCrest", new Color(0.95f, 0.72f, 0.28f));
            RuntimeUi.SetRect(crest.rectTransform, new Vector2(0.5f, 0.79f), new Vector2(190, 190), Vector2.zero);

            var title = RuntimeUi.Text(canvas.transform, "Title", "RPG Quest Adventure", 58, Color.white);
            title.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(900, 90), Vector2.zero);

            var subtitle = RuntimeUi.Text(canvas.transform, "Subtitle", "Реальные дела усиливают героя", 30, new Color(1f, 0.88f, 0.58f));
            RuntimeUi.SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(860, 70), Vector2.zero);

            emailInput = CreateInput(canvas.transform, "Email", "child@example.com", new Vector2(0.5f, 0.50f));
            passwordInput = CreateInput(canvas.transform, "Password", "password", new Vector2(0.5f, 0.41f));
            passwordInput.contentType = TMP_InputField.ContentType.Password;

            var login = RuntimeUi.Button(canvas.transform, "LoginButton", "Войти", new Color(0.21f, 0.51f, 0.76f), () => _ = LoginAsync(false));
            RuntimeUi.SetRect(login.GetComponent<RectTransform>(), new Vector2(0.5f, 0.29f), new Vector2(650, 86), Vector2.zero);

            var dev = RuntimeUi.Button(canvas.transform, "DevLoginButton", "Dev вход", new Color(0.24f, 0.58f, 0.35f), () => _ = LoginAsync(true));
            RuntimeUi.SetRect(dev.GetComponent<RectTransform>(), new Vector2(0.5f, 0.20f), new Vector2(650, 86), Vector2.zero);

            statusText = RuntimeUi.Text(canvas.transform, "Status", "", 28, new Color(1f, 0.85f, 0.45f));
            RuntimeUi.SetRect(statusText.rectTransform, new Vector2(0.5f, 0.11f), new Vector2(850, 100), Vector2.zero);
        }

        private static TMP_InputField CreateInput(Transform parent, string name, string placeholder, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(Image), typeof(TMP_InputField), typeof(Shadow), typeof(Outline));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.95f, 0.91f, 0.82f);
            image.raycastTarget = true;
            go.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.25f);
            go.GetComponent<Shadow>().effectDistance = new Vector2(0, -4);
            go.GetComponent<Outline>().effectColor = new Color(0.28f, 0.20f, 0.13f, 0.55f);
            var input = go.GetComponent<TMP_InputField>();
            input.targetGraphic = image;
            input.caretColor = RuntimeUi.Ink;
            input.selectionColor = new Color(0.21f, 0.51f, 0.76f, 0.35f);

            var text = CreateInputText(go.transform, "Text", "", RuntimeUi.Ink);
            var placeholderText = CreateInputText(go.transform, "Placeholder", placeholder, new Color(0.42f, 0.35f, 0.27f));
            input.textComponent = text;
            input.placeholder = placeholderText;
            RuntimeUi.SetRect(go.GetComponent<RectTransform>(), anchor, new Vector2(760, 78), Vector2.zero);
            return input;
        }

        private static TextMeshProUGUI CreateInputText(Transform parent, string name, string value, Color color)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = 30;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            RuntimeUi.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(24, 0);
            text.rectTransform.offsetMax = new Vector2(-24, 0);
            return text;
        }

        private async Task LoginAsync(bool dev)
        {
            if (isLoggingIn)
            {
                return;
            }

            isLoggingIn = true;
            statusText.text = "Загрузка профиля...";
            try
            {
                var user = dev
                    ? await ServiceRegistry.Auth.DevLoginAsync()
                    : await ServiceRegistry.Auth.SignInWithEmailAsync(emailInput.text, passwordInput.text);

                if (user.Role != UserRole.Child)
                {
                    statusText.text = "Этот клиент доступен только ребёнку.";
                    return;
                }

                await ServiceRegistry.PlayerProfiles.LoadChildProfileAsync(user.ChildId);
                await ServiceRegistry.GameState.LoadAsync(user.ChildId);
                SceneLoader.LoadMap();
            }
            catch (System.Exception ex)
            {
                statusText.text = ex.Message;
            }
            finally
            {
                isLoggingIn = false;
            }
        }
    }
}
