using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public class LoginScreen : MonoBehaviour
    {
        private InputField emailInput;
        private InputField passwordInput;
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
            passwordInput.contentType = InputField.ContentType.Password;

            var login = RuntimeUi.Button(canvas.transform, "LoginButton", "Войти", new Color(0.21f, 0.51f, 0.76f), () => _ = LoginAsync(false));
            RuntimeUi.SetRect(login.GetComponent<RectTransform>(), new Vector2(0.5f, 0.29f), new Vector2(650, 86), Vector2.zero);

            var dev = RuntimeUi.Button(canvas.transform, "DevLoginButton", "Dev вход", new Color(0.24f, 0.58f, 0.35f), () => _ = LoginAsync(true));
            RuntimeUi.SetRect(dev.GetComponent<RectTransform>(), new Vector2(0.5f, 0.20f), new Vector2(650, 86), Vector2.zero);

            statusText = RuntimeUi.Text(canvas.transform, "Status", "", 28, new Color(1f, 0.85f, 0.45f));
            RuntimeUi.SetRect(statusText.rectTransform, new Vector2(0.5f, 0.11f), new Vector2(850, 100), Vector2.zero);
        }

        private static InputField CreateInput(Transform parent, string name, string placeholder, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(Image), typeof(InputField), typeof(Shadow), typeof(Outline));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.95f, 0.91f, 0.82f);
            go.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.25f);
            go.GetComponent<Shadow>().effectDistance = new Vector2(0, -4);
            go.GetComponent<Outline>().effectColor = new Color(0.28f, 0.20f, 0.13f, 0.55f);
            var input = go.GetComponent<InputField>();
            var text = RuntimeUi.Text(go.transform, "Text", "", 30, RuntimeUi.Ink, TextAnchor.MiddleLeft);
            text.rectTransform.offsetMin = new Vector2(24, 0);
            text.rectTransform.offsetMax = new Vector2(-24, 0);
            var placeholderText = RuntimeUi.Text(go.transform, "Placeholder", placeholder, 30, new Color(0.42f, 0.35f, 0.27f), TextAnchor.MiddleLeft);
            placeholderText.rectTransform.offsetMin = new Vector2(24, 0);
            placeholderText.rectTransform.offsetMax = new Vector2(-24, 0);
            input.textComponent = text;
            input.placeholder = placeholderText;
            RuntimeUi.SetRect(go.GetComponent<RectTransform>(), anchor, new Vector2(760, 78), Vector2.zero);
            return input;
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
